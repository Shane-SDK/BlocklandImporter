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
        static public readonly int[] faceTransformations = new int[]
        {
            0, 1, 2, 3, 4, 5,
            5, 4, 2, 3, 0, 1,
            1, 0, 2, 3, 5, 4,
            4, 5, 2, 3, 1, 0,
        };
        static public readonly int[] axisToFaceSetDirection = new[]
        {
            3, 5,   // +X, -X
            0, 1,   // +Y, -Y
            2, 4    // +Z, -Z
        };
        public static void GetFaces(IList<BrickInstance> bricks, IList<Face> faces)
        {
            BoundsInt[] brickBounds = new BoundsInt[bricks.Count];

            UnityEngine.Profiling.Profiler.BeginSample("BrickBounds");
            for (int i = 0; i < bricks.Count; i++)
            {
                BrickInstance instance = bricks[i];
                instance.GetTransformedBounds(out BoundsInt bounds);
                brickBounds[i] = bounds;
            }
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Create Occlusion Data");
            new Occlusion.Occlusion().GetOcclusion(brickBounds, out byte[] occlusionFlags);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Insert Faces");
            for (int i = 0; i < bricks.Count; i++)
            {
                BrickInstance instance = bricks[i];
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

                byte occlusionFlag = occlusionFlags[i];

                for (int n = 0; n < 6; n++)
                {
                    int worldDirection = faceTransformations[(instance.angle % 4) * 6 + n];
                    byte mask = (byte)(1 << worldDirection);
                    bool occlude = (mask & occlusionFlag) == mask;
                    if (!occlude)
                        InsertFaces(data.faceSets[axisToFaceSetDirection[n]], ref faces);
                }

                if (occlusionFlag != (0x3F))
                    InsertFaces(data.faceSets[6], ref faces);  // Omni
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        public static Mesh CreateMesh(ICollection<Face> faces, out TextureFace[] textureFaces, bool createLightMapUVs = false)
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

//#if UNITY_EDITOR
//            if (createLightMapUVs)
//            {
//                UnityEditor.Unwrapping.GenerateSecondaryUVSet(mesh);
//            }
//#endif

            mesh.UploadMeshData(true);

            return mesh;
        }
        public static System.Numerics.Vector3 ToNumeric(Vector3 c)
        {
            return new System.Numerics.Vector3(c.x, c.y, c.z);
        }
        public static bool Overlaps(ref BoundsInt a, ref BoundsInt b)
        {
            for (int c = 0; c < 3; c++)
            {
               if (a.min[c] >= b.max[c]) return false;
               if (a.max[c] <= b.min[c]) return false;
            }

            return true;
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
