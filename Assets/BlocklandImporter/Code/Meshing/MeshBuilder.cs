using Blockland.Meshing.Occlusion;
using Blockland.Objects;
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
        static public readonly int[] axisToComponentsTable = new[]
        {
            0, 1, 2,
            0, 1, 2,
            1, 0, 2,
            1, 0, 2,
            2, 1, 0,
            2, 1, 0
        };
        public static void GetFaces(IList<BrickInstance> bricks, IList<Face> faces, bool centerOrigin = false)
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

            if (centerOrigin)
            {
                Vector3 min = Vector3.one * float.MaxValue;
                Vector3 max = Vector3.one * float.MinValue;

                for (int i = 0; i < brickBounds.Length; i++)
                {
                    BoundsInt bounds = brickBounds[i];
                    min = Vector3.Min(bounds.min, min);
                    max = Vector3.Max(bounds.max, max);
                }

                Vector3 origin = Vector3Int.FloorToInt((max + min) / 2.0f);
                origin.y = 0;
                origin = Blockland.StudsToUnity(origin);

                // transform all faces to origin
                for (int i = 0; i < faces.Count; i++)
                {
                    Face face = faces[i];
                    for (int v = 0; v < 4; v++)
                    {
                        face.SetPosition(v, face[v].position - origin);
                    }
                    faces[i] = face;
                }
            }
        }
        public static void GetFacesOctree(IList<BrickInstance> bricks, IList<Face> faces, bool centerOrigin = false)
        {
            BoundsInt[] brickBounds = new BoundsInt[bricks.Count];
            BoundsInt bounds;
            Vector3Int min = Vector3Int.one * int.MaxValue;
            Vector3Int max = Vector3Int.one * int.MinValue;

            UnityEngine.Profiling.Profiler.BeginSample("Initialization");

            for (int i = 0; i < bricks.Count; i++)
            {
                BrickInstance instance = bricks[i];
                instance.GetTransformedBounds(out BoundsInt instanceBounds);
                brickBounds[i] = instanceBounds;
                min = Vector3Int.Min(min, instanceBounds.min);
                max = Vector3Int.Max(max, instanceBounds.max);
            }

            Vector3Int size = max - min;
            bounds = new BoundsInt(min.x, min.y, min.z, size.x, size.y, size.z);

            // create octree
            int treeSize = Mathf.Max(size.x, size.y, size.z);
            Octree.BoundsOctree<int> brickTree = new Octree.BoundsOctree<int>(treeSize, Extensions.Vec3(bounds.center), 1, 1.2f);
            for (int i = 0; i < brickBounds.Length; i++)
            {
                BoundsInt instanceBounds = brickBounds[i];
                Octree.BoundingBox box = new Octree.BoundingBox(Extensions.Vec3(instanceBounds.center), Extensions.Vec3(instanceBounds.size));
                brickTree.Add(i, box);
            }

            //Extensions.DrawOctree(brickTree, new Color(1, 0, 0, 0.25f), Color.cyan, 10);

            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Insert Faces");
            List<int> overlapIndices = new();
            for (int i = 0; i < bricks.Count; i++)
            {
                BrickInstance instance = bricks[i];
                BrickData data = instance.data;
                BoundsInt instanceBounds = brickBounds[i];

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

                overlapIndices.Clear();

                Vector3Int overlapMin = instanceBounds.min - Vector3Int.one;
                Vector3Int overlapMax = instanceBounds.max + Vector3Int.one;

                BoundsInt overlapBounds = new BoundsInt(overlapMin, overlapMax - overlapMin);
                UnityEngine.Profiling.Profiler.BeginSample("GetCollidingNonAlloc");
                brickTree.GetCollidingNonAlloc(overlapIndices, new Octree.BoundingBox(Extensions.Vec3(overlapBounds.center), Extensions.Vec3(overlapBounds.size)));
                UnityEngine.Profiling.Profiler.EndSample();

                bool DoOccludeFace(int worldDirectionIndex)
                {
                    /*
                     * Calculate bounding box to overlap
                     * Get bricks in bounds using octree
                     * 
                     * iterate over each space in bounds
                     */

                    int extrudedPosition;
                    int flattenComponent = axisToComponentsTable[worldDirectionIndex * 3];
                    int rightAxisComponent = axisToComponentsTable[worldDirectionIndex * 3 + 1];
                    int upAxisComponent = axisToComponentsTable[worldDirectionIndex * 3 + 2];

                    if ((worldDirectionIndex % 2) == 0)  // index corresponds to a positive direction
                    {
                        extrudedPosition = instanceBounds.max[flattenComponent];
                    }
                    else
                    {
                        extrudedPosition = instanceBounds.min[flattenComponent] - 1;
                    }

                    // get corresponding axis to move in
                    // flatten i and j components
                    uint rightLength = (uint)(instanceBounds.max[rightAxisComponent] - instanceBounds.min[rightAxisComponent]);
                    uint upLength = (uint)(instanceBounds.max[upAxisComponent] - instanceBounds.min[upAxisComponent]);
                    uint area = rightLength * upLength;

                    //overlapIndices.Clear();

                    //Vector3Int overlapMin = default;
                    //overlapMin[rightAxisComponent] = instanceBounds.min[rightAxisComponent];
                    //overlapMin[upAxisComponent] = instanceBounds.min[upAxisComponent];
                    //overlapMin[flattenComponent] = extrudedPosition;

                    //Vector3Int overlapSize = default;
                    //overlapSize[rightAxisComponent] = (int)rightLength;
                    //overlapSize[upAxisComponent] = (int)upLength;
                    //overlapSize[flattenComponent] = 1;

                    //BoundsInt overlapBounds = new BoundsInt(overlapMin, overlapSize);
                    //UnityEngine.Profiling.Profiler.BeginSample("GetCollidingNonAlloc");
                    //brickTree.GetCollidingNonAlloc(overlapIndices, new Octree.BoundingBox(Extensions.Vec3(overlapBounds.center), Extensions.Vec3(overlapBounds.size)));
                    //UnityEngine.Profiling.Profiler.EndSample();

                    //if (overlapIndices.Count == 0)
                    //{
                    //    UnityEngine.Profiling.Profiler.EndSample();
                    //    return false;
                    //}

                    int lastUsedNeighborIndex = -1;
                    UnityEngine.Profiling.Profiler.BeginSample("Bounding Box Iteration");
                    for (uint flattenedIndex = 0; flattenedIndex < area; flattenedIndex++)
                    {
                        // check if space in volume does not have another bounds in it

                        uint up = flattenedIndex / rightLength;  // local positive offsets from min
                        uint right = flattenedIndex % rightLength;

                        Vector3Int coordinate = default;
                        coordinate[rightAxisComponent] = instanceBounds.min[rightAxisComponent] + (int)right;
                        coordinate[upAxisComponent] = instanceBounds.min[upAxisComponent] + (int)up;
                        coordinate[flattenComponent] = extrudedPosition;

                        bool foundBrick = false;

                        if (lastUsedNeighborIndex != -1 && brickBounds[lastUsedNeighborIndex].Contains(coordinate))  // early escape
                        {
                            foundBrick = true;
                        }

                        if (!foundBrick)
                        {
                            UnityEngine.Profiling.Profiler.BeginSample("Second Loop Iteration");
                            for (int n = 0; n < overlapIndices.Count; n++)
                            {
                                int index = overlapIndices[n];
                                if (index == i) continue;

                                BoundsInt neighbor = brickBounds[index];
                                if (neighbor.Contains(coordinate))
                                {
                                    lastUsedNeighborIndex = index;
                                    foundBrick = true;
                                    break;
                                }
                            }
                            UnityEngine.Profiling.Profiler.EndSample();
                        }

                        if (!foundBrick)
                        {
                            UnityEngine.Profiling.Profiler.EndSample();
                            return false;
                        }
                    }
                    UnityEngine.Profiling.Profiler.EndSample();

                    return true;
                }

                byte occlusionFlags = 0;
                for (int n = 0; n < 6; n++)
                {
                    int worldDirection = faceTransformations[(instance.angle % 4) * 6 + n];
                    UnityEngine.Profiling.Profiler.BeginSample("DoOccludeFace");
                    bool occlude = DoOccludeFace(worldDirection);
                    UnityEngine.Profiling.Profiler.EndSample();
                    if (occlude)
                        occlusionFlags = (byte)(occlusionFlags | (1 << n));
                    if (!occlude)
                        InsertFaces(data.faceSets[axisToFaceSetDirection[n]], ref faces);
                }

                if (occlusionFlags != (0x3F))
                    InsertFaces(data.faceSets[6], ref faces);  // Omni
            }
            UnityEngine.Profiling.Profiler.EndSample();

            if (centerOrigin)
            {
                Vector3 origin = Vector3Int.FloorToInt(bounds.center);
                origin.y = 0;
                origin = Blockland.StudsToUnity(origin);

                // transform all faces to origin
                for (int i = 0; i < faces.Count; i++)
                {
                    Face face = faces[i];
                    for (int v = 0; v < 4; v++)
                    {
                        face.SetPosition(v, face[v].position - origin);
                    }
                    faces[i] = face;
                }
            }
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
        public static bool Overlaps(ref BoundsInt a, ref BoundsInt b)
        {
            for (int c = 0; c < 3; c++)
            {
               if (a.min[c] >= b.max[c]) return false;
               if (a.max[c] <= b.min[c]) return false;
            }

            return true;
        }
        public static GameObject CreateGameObject(SaveData save, bool mergeFaces, bool createLightmaps, bool centerOrigin)
        {
            GameObject goRoot = new(save.name);

            // mesh builder
            UnityEngine.Profiling.Profiler.BeginSample("GetFaces");
            List<Face> faces = new List<Face>();
            MeshBuilder.GetFacesOctree(save.bricks, faces, centerOrigin);
            UnityEngine.Profiling.Profiler.EndSample();

            if (mergeFaces)
            {
                UnityEngine.Profiling.Profiler.BeginSample("Optimize Faces");
                List<Face> mergedFaces = new();
                FaceOptimizer optimizer = new FaceOptimizer();
                optimizer.OptimizeFaces(faces, mergedFaces, (int)Mathf.Sqrt(save.bricks.Count));
                faces = mergedFaces;
                UnityEngine.Profiling.Profiler.EndSample();
            }

            UnityEngine.Profiling.Profiler.BeginSample("Create Mesh");
            Mesh mesh = MeshBuilder.CreateMesh(faces, out TextureFace[] textureFaces, createLightmaps);
            mesh.name = save.name;
            UnityEngine.Profiling.Profiler.EndSample();

            if (createLightmaps)
            {
                UnityEngine.Profiling.Profiler.BeginSample("Create Lightmap UVs");
                LightMapper.GenerateUVs(faces, out Vector2[] uvs);
                mesh.SetUVs(1, uvs);
                UnityEngine.Profiling.Profiler.EndSample();
            }

            goRoot.AddComponent<MeshFilter>().sharedMesh = mesh;

            Material[] materials = new Material[textureFaces.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                TextureFace face = textureFaces[i];
                switch (face)
                {
                    case TextureFace.Top:
                        materials[i] = UnityEngine.Resources.Load<Material>("Bricks/BrickTop");
                        break;
                    case TextureFace.Side:
                    default:
                        materials[i] = UnityEngine.Resources.Load<Material>("Bricks/BrickSide");
                        break;
                    case TextureFace.Print:
                        materials[i] = UnityEngine.Resources.Load<Material>("Bricks/Print");
                        break;
                    case TextureFace.Ramp:
                        materials[i] = UnityEngine.Resources.Load<Material>("Bricks/Ramp");
                        break;
                    case TextureFace.BottomEdge:
                        materials[i] = UnityEngine.Resources.Load<Material>("Bricks/BrickBottomEdge");
                        break;
                    case TextureFace.BottomLoop:
                        materials[i] = UnityEngine.Resources.Load<Material>("Bricks/BrickBottomLoop");
                        break;
                }
            }

            goRoot.AddComponent<MeshRenderer>().sharedMaterials = materials;

            return goRoot;
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
