using Blockland.Objects;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Blockland.Meshing
{
    public class MeshBuilder
    {
        public static readonly VertexAttributeDescriptor[] vertexAttributes = new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor( VertexAttribute.Position, VertexAttributeFormat.Float32, 3 ),
            new VertexAttributeDescriptor( VertexAttribute.Normal, VertexAttributeFormat.Float32, 3 ),
            new VertexAttributeDescriptor( VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4 ),
            new VertexAttributeDescriptor( VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2 ),
        };
        public static void GetFaces(IList<BrickInstance> bricks, IList<Face> faces)
        {
            Dictionary<Vector3Int, int> partLookup = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < bricks.Count; i++)
            {
                BrickInstance instance = bricks[i];
                instance.GetTransformedBounds(out BoundsInt bounds);

                foreach (Vector3Int pos in bounds.allPositionsWithin)
                {
                    if (!partLookup.ContainsKey(pos))
                    {
                        partLookup.Add(pos, i);
                    }
                }
            }

            bool IsOccluding(Vector3Int occluderPoint, Vector3 occluderDirectionVector, Direction occluderDirection)
            {
                // get reference to occluder brick using lookup
                // transform??

                if (!partLookup.TryGetValue(occluderPoint, out int brickIndex))
                    return false;

                BrickInstance instance = bricks[brickIndex];

                if (instance.data.type == BrickType.Brick)  // bricks occlude from any point/direction
                    return true;

                return false;
                // get occluder point in local space
                //Vector3 local = instance.InverseTransformPoint(occluderPoint);
            }

            for (int i = 0; i < bricks.Count; i++)
            {
                BrickInstance instance = bricks[i];

                instance.GetTransformedBounds(out Bounds bounds);
                instance.GetTransformedBounds(out BoundsInt intBounds);

                bool IsFaceOccluded(Vector3 direction, int axisIndex)
                {
                    direction = Quaternion.AngleAxis(instance.Angle, Vector3.up) * direction;
                    int transformedAxisIndex = axisIndex;
                    bool turn90 = instance.angle % 2 == 1;
                    if (turn90 && axisIndex != 1)
                        transformedAxisIndex = axisIndex == 0 ? 2 : 0;

                    BoundsInt overlapBounds = intBounds;
                    overlapBounds.position += Vector3Int.RoundToInt(direction);

                    foreach (Vector3Int pos in overlapBounds.allPositionsWithin)
                    {
                        if (intBounds.Contains(pos)) continue;  // todo FIX THIS SHIT, USE SMARTER BOUNDS

                        if (!partLookup.TryGetValue(pos, out int partIndex))
                            return false;

                        BrickInstance otherInstance = bricks[partIndex];
                        if (otherInstance.data == instance.data)
                        {
                            Vector3 difference = otherInstance.position - instance.position;
                            difference[transformedAxisIndex] = 0;
                            if (difference.sqrMagnitude == 0.0f)  // only test for aligned bricks
                            {
                                if (Mathf.DeltaAngle(otherInstance.Angle, instance.Angle) == 0.0f && instance.data.HasSymmetry(axisIndex))  // same orientation
                                {
                                    return true;
                                }
                                //else if (Mathf.DeltaAngle(otherInstance.Angle, instance.Angle) == 180.0f)  // opposite directions
                                //{
                                //    return true;
                                //}
                            }
                        }

                        if (!IsOccluding(pos, -direction, Direction.Backward))
                        {
                            return false;
                        }

                    }

                    return true;
                }

                BrickData data = instance.data;
                void InsertFaces(FaceSet faceSet, ref IList<Face> faces)
                {
                    foreach (Face face in faceSet.faces)
                    {
                        Face newFace = face;

                        for (int i = 0; i < 4; i++)
                        {
                            FaceVertex newVertex = face[i];
                            newVertex.position = Blockland.StudsToUnity(Quaternion.AngleAxis(instance.Angle, Vector3.up) * face[i].position + instance.position);
                            if (!face.colorOverride)
                                newVertex.color = instance.color;
                            newFace[i] = newVertex;
                        }

                        faces.Add(newFace);
                    }
                }

                if (!IsFaceOccluded(Vector3.forward, 2))  // North / Front
                    InsertFaces(data.faceSets[2], ref faces);

                if (!IsFaceOccluded(-Vector3.forward, 2))   // South / Back
                    InsertFaces(data.faceSets[4], ref faces);

                if (!IsFaceOccluded(Vector3.right, 0))    // East / Right
                    InsertFaces(data.faceSets[3], ref faces);

                if (!IsFaceOccluded(-Vector3.right, 0))     // West / Left
                    InsertFaces(data.faceSets[5], ref faces);

                if (!IsFaceOccluded(Vector3.up, 1))    // Top
                    InsertFaces(data.faceSets[0], ref faces);

                if (!IsFaceOccluded(-Vector3.up, 1))   // Bottom
                    InsertFaces(data.faceSets[1], ref faces);

                InsertFaces(data.faceSets[6], ref faces);  // Omni
            }
        }
        public static Mesh CreateMesh(ICollection<Face> faces, out TextureFace[] textureFaces)
        {
            SubMeshDescriptor[] descriptors;
            List<Vertex> vertices = new List<Vertex>();
            Dictionary<TextureFace, List<uint>> sideIndices = new();
            Mesh mesh = new Mesh();

            List<uint> GetIndices(TextureFace face)
            {
                if (!sideIndices.TryGetValue(face, out List<uint> indices))
                {
                    indices = new List<uint>();
                    sideIndices[face] = indices;
                }

                return indices;
            }

            foreach (Face face in faces)
            {
                // get indices that correspond to texture
                List<uint> textureIndices = GetIndices(face.texture);

                int offset = vertices.Count;
                textureIndices.Add((uint)offset + 0);
                textureIndices.Add((uint)offset + 1);
                textureIndices.Add((uint)offset + 2);
                textureIndices.Add((uint)offset + 0);
                textureIndices.Add((uint)offset + 2);
                textureIndices.Add((uint)offset + 3);

                for (int v = 0; v < 4; v++)
                {
                    vertices.Add(new Vertex
                    {
                        position = face[v].position,
                        color = face[v].color,
                        uv = face[v].uv
                    });
                }
            }

            mesh.indexFormat = vertices.Count >= (65536) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

            mesh.SetVertexBufferParams(vertices.Count, vertexAttributes);
            mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count);

            descriptors = new SubMeshDescriptor[sideIndices.Count];
            textureFaces = new TextureFace[sideIndices.Count];

            System.Collections.IList indices = mesh.indexFormat == IndexFormat.UInt32 ? new List<uint>() : new List<ushort>();

            int indexStart = 0;
            int index = 0;
            foreach (KeyValuePair<TextureFace, List<uint>> pair in sideIndices)
            {
                if (mesh.indexFormat == IndexFormat.UInt32)
                    (indices as List<uint>).AddRange(pair.Value);
                else
                {
                    List<ushort> shortIndices = indices as List<ushort>;
                    for (int i = 0; i < pair.Value.Count; i++)
                    {
                        shortIndices.Add((ushort)pair.Value[i]);
                    }
                }
                descriptors[index] = new SubMeshDescriptor(indexStart, pair.Value.Count);
                textureFaces[index] = pair.Key;
                indexStart += pair.Value.Count;
                index++;
            }

            mesh.SetIndexBufferParams(indices.Count, mesh.indexFormat);
            if (mesh.indexFormat == IndexFormat.UInt32)
                mesh.SetIndexBufferData(indices as List<uint>, 0, 0, indices.Count);
            else
                mesh.SetIndexBufferData(indices as List<ushort>, 0, 0, indices.Count);
            mesh.subMeshCount = indices.Count;
            mesh.SetSubMeshes(descriptors);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            mesh.UploadMeshData(true);

            return mesh;
        }
    }
    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Color32 color;
        public Vector2 uv;
    }
}
