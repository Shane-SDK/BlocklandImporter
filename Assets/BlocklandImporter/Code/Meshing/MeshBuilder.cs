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
        public SubMeshDescriptor[] descriptors;
        public TextureFace[] textureFaces;
        public List<BrickInstance> bricks = new();
        public List<Vertex> vertices = new List<Vertex>();
        public Dictionary<TextureFace, List<uint>> sideIndices = new();
#if UNITY_EDITOR
        UnityEditor.AssetImporters.AssetImportContext importContext;
#endif
        public MeshBuilder()
        {

        }
#if UNITY_EDITOR
        public MeshBuilder(UnityEditor.AssetImporters.AssetImportContext importContext)
        {
            this.importContext = importContext;
        }
#endif
        public void AddBrick(BrickInstance brick)
        {
            bricks.Add(brick);
        }
        public Mesh CreateMesh(bool merge = true)
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

            // todo - share coplanar verts

            for (int i = 0; i < bricks.Count; i++)
            {
                BrickInstance instance = bricks[i];

                instance.GetTransformedBounds(out Bounds bounds);
                instance.GetTransformedBounds(out BoundsInt intBounds);

                bool IsFaceOccluded(Vector3 direction)
                {
                    direction = Quaternion.AngleAxis(instance.Angle, Vector3.up) * direction;

                    BoundsInt overlapBounds = intBounds;
                    overlapBounds.position += Vector3Int.RoundToInt(direction);

                    foreach (Vector3Int c in overlapBounds.allPositionsWithin)
                    {
                        Vector3Int pos = c;

                        if (intBounds.Contains(pos)) continue;
                        if (!IsOccluding(c, -direction, Direction.Backward))
                        {
                            return false;
                        }

                    }

                    return true;
                }

                BrickData data = instance.data;
                void InsertFaces(FaceSet faceSet)
                {
                    foreach (Face face in faceSet.faces)
                    {
                        // get indices that correspond to texture
                        List<uint> textureIndices = GetIndices(face.texture);

                        int offset = vertices.Count;
                        textureIndices.Add((uint)offset + 0);
                        textureIndices.Add((uint)offset + 1);
                        textureIndices.Add((uint)offset + 3);
                        textureIndices.Add((uint)offset + 1);
                        textureIndices.Add((uint)offset + 2);
                        textureIndices.Add((uint)offset + 3);

                        for (int v = 0; v < 4; v++)
                        {
                            Color color = face[v].color;
                            if (!face.colorOverride)
                                color = instance.color;

                            Vector3 transformedPosition = Blockland.StudsToUnity(Quaternion.AngleAxis(instance.Angle, Vector3.up) * face[v].position + instance.position);

                            vertices.Add(new Vertex
                            {
                                position = transformedPosition,
                                color = color,
                                uv = face[v].uv
                            });
                        }
                    }
                }

                if (!IsFaceOccluded(Vector3.forward))  // North / Front
                    InsertFaces(data.faceSets[2]);

                if (!IsFaceOccluded(-Vector3.forward))   // South / Back
                    InsertFaces(data.faceSets[4]);

                if (!IsFaceOccluded(Vector3.right))    // East / Right
                    InsertFaces(data.faceSets[3]);

                if (!IsFaceOccluded(-Vector3.right))     // West / Left
                    InsertFaces(data.faceSets[5]);

                if (!IsFaceOccluded(Vector3.up))    // Top
                    InsertFaces(data.faceSets[0]);

                if (!IsFaceOccluded(-Vector3.up))   // Bottom
                    InsertFaces(data.faceSets[1]);

                InsertFaces(data.faceSets[6]);  // Omni
            }

            Mesh mesh = new Mesh();
#if UNITY_EDITOR
            if (importContext != null)
                mesh.name = System.IO.Path.GetFileName(importContext.assetPath);
#endif
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
        List<uint> GetIndices(TextureFace face)
        {
            if (!sideIndices.TryGetValue(face, out List<uint> indices))
            {
                indices = new List<uint>();
                sideIndices[face] = indices;
            }

            return indices;
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
