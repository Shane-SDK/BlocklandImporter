using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using UnityEngine.Rendering;
using Blockland.Objects;

namespace Blockland.Editor
{
    [ScriptedImporter(0, "bls")]
    public class SaveImporter : ScriptedImporter
    {
        public static readonly VertexAttributeDescriptor[] vertexAttributes = new VertexAttributeDescriptor[] { 
            new VertexAttributeDescriptor( VertexAttribute.Position, VertexAttributeFormat.Float32, 3 ),
            new VertexAttributeDescriptor( VertexAttribute.Normal, VertexAttributeFormat.Float32, 3 ),
            new VertexAttributeDescriptor( VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4 ),
            new VertexAttributeDescriptor( VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2 ),
        };
        public override void OnImportAsset(AssetImportContext ctx)
        {
            using FileStream file = System.IO.File.OpenRead(ctx.assetPath);
            Reader reader = new Reader(file);
            UnityEngine.Profiling.Profiler.BeginSample("Read Save");
            SaveData save = SaveData.CreateFromReader(reader);
            save.name = System.IO.Path.GetFileNameWithoutExtension(ctx.assetPath);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Asset Creation");
            GameObject root = new();
            ctx.AddObjectToAsset("root", root);
            ctx.SetMainObject(root);

            List<Vertex> vertices = new List<Vertex>();
            Dictionary<SideMaterial, List<int>> sideIndices = new();

            sideIndices[SideMaterial.Top] = new();
            sideIndices[SideMaterial.Side] = new();

            HashSet<Vector3Int> occludingCoords = new HashSet<Vector3Int>();
            for (int i = 0; i < save.bricks.Count; i++)
            {
                SaveData.BrickInstance instance = save.bricks[i];
                instance.GetTransformedBounds(out BoundsInt bounds);

                foreach (Vector3Int pos in bounds.allPositionsWithin)
                {
                    if (!occludingCoords.Contains(pos))
                    {
                        occludingCoords.Add(pos);
                    }
                }
            }

            // todo - share coplanar verts

            for (int i = 0; i < save.bricks.Count; i++)
            {
                SaveData.BrickInstance instance = save.bricks[i];

                if (instance.brickResource.Type == Resources.ResourceType.Brick)
                {
                    instance.GetTransformedBounds(out Bounds bounds);
                    instance.GetTransformedBounds(out BoundsInt intBounds);

                    void AddFace(int side, float uScale, float vScale, SideMaterial sideMat)
                    {
                        // Convert size from studs to unity units
                        Vector3 size = Blockland.StudsToUnity(instance.brickResource.colliderSize);
                        Quaternion rotation = Quaternion.AngleAxis(instance.Angle, Vector3.up);

                        Vector3 origin = Blockland.StudsToUnity(instance.brickResource.colliderSize / 2.0f);
                        Vector3 position = Blockland.StudsToUnity(instance.position) - size / 2.0f;

                        Vector3 TransformVector(Vector3 local, Vector3 origin, Quaternion rotation)
                        {
                            Vector3 offset = local - origin;
                            offset = rotation * offset;

                            return origin + offset;
                        }

                        Color color = instance.color;

                        Vertex a = default, b = default, c = default, d = default;

                        a.color = color;
                        b.color = color;
                        c.color = color;
                        d.color = color;

                        int offset = vertices.Count;

                        List<int> indices = sideIndices[sideMat];

                        indices.Add(offset + 0);
                        indices.Add(offset + 1);
                        indices.Add(offset + 3);
                        indices.Add(offset + 0);
                        indices.Add(offset + 3);
                        indices.Add(offset + 2);

                        switch (side)
                        {
                            case 0:     // Front
                                a.position = position + TransformVector(new Vector3(0, 0, 0), origin, rotation);                   // 0
                                b.position = position + TransformVector(new Vector3(0, size.y, 0), origin, rotation);              // 2
                                c.position = position + TransformVector(new Vector3(size.x, 0, 0), origin, rotation);              // 1
                                d.position = position + TransformVector(new Vector3(size.x, size.y, 0), origin, rotation);         // 3
                                break;
                            case 1:     // Back
                                a.position = position + TransformVector(new Vector3(0, 0, size.z), origin, rotation);              // 4
                                b.position = position + TransformVector(new Vector3(size.x, 0, size.z), origin, rotation);         // 5
                                c.position = position + TransformVector(new Vector3(0, size.y, size.z), origin, rotation);         // 6
                                d.position = position + TransformVector(new Vector3(size.x, size.y, size.z), origin, rotation);    // 7
                                break;
                            case 2:     // Left
                                a.position = position + TransformVector(new Vector3(0, 0, 0), origin, rotation);                   // 0
                                b.position = position + TransformVector(new Vector3(0, 0, size.z), origin, rotation);              // 4
                                c.position = position + TransformVector(new Vector3(0, size.y, 0), origin, rotation);              // 2
                                d.position = position + TransformVector(new Vector3(0, size.y, size.z), origin, rotation);         // 6
                                break;
                            case 3:     // Right
                                a.position = position + TransformVector(new Vector3(size.x, 0, 0), origin, rotation);              // 1
                                b.position = position + TransformVector(new Vector3(size.x, size.y, 0), origin, rotation);         // 3
                                c.position = position + TransformVector(new Vector3(size.x, 0, size.z), origin, rotation);         // 5
                                d.position = position + TransformVector(new Vector3(size.x, size.y, size.z), origin, rotation);    // 7
                                break;
                            case 4:     // Top
                                a.position = position + TransformVector(new Vector3(0, size.y, 0), origin, rotation);              // 2
                                b.position = position + TransformVector(new Vector3(0, size.y, size.z), origin, rotation);         // 6
                                c.position = position + TransformVector(new Vector3(size.x, size.y, 0), origin, rotation);         // 3
                                d.position = position + TransformVector(new Vector3(size.x, size.y, size.z), origin, rotation);    // 7
                                break;
                            case 5:     // Bottom
                                a.position = position + TransformVector(new Vector3(0, 0, 0), origin, rotation);                   // 0
                                b.position = position + TransformVector(new Vector3(size.x, 0, 0), origin, rotation);              // 1
                                c.position = position + TransformVector(new Vector3(0, 0, size.z), origin, rotation);              // 4
                                d.position = position + TransformVector(new Vector3(size.x, 0, size.z), origin, rotation);         // 5
                                break;
                        }

                        a.uv = new Vector2(0 * uScale, 0 * vScale);
                        b.uv = new Vector2(1 * uScale, 0 * vScale);
                        c.uv = new Vector2(0 * uScale, 1 * vScale);
                        d.uv = new Vector2(1 * uScale, 1 * vScale);

                        vertices.Add(a);
                        vertices.Add(b);
                        vertices.Add(c);
                        vertices.Add(d);
                    }

                    bool IsOccluded(Vector3 direction)
                    {
                        bool occlude = true;
                        direction = Quaternion.AngleAxis(instance.Angle, Vector3.up) * direction;

                        BoundsInt overlapBounds = intBounds;
                        overlapBounds.position += Vector3Int.RoundToInt(direction);

                        foreach (Vector3Int c in overlapBounds.allPositionsWithin)
                        {
                            Vector3Int pos = c;

                            if (intBounds.Contains(pos)) continue;
                            if (!occludingCoords.Contains(pos))
                            {
                                occlude = false;
                            }
                        }

                        return occlude;
                    }

                    if (!IsOccluded(-Vector3.forward))
                        AddFace(0, 1, 1, SideMaterial.Side);     // Front

                    if (!IsOccluded(Vector3.forward))
                        AddFace(1, 1, 1, SideMaterial.Side);     // Back

                    if (!IsOccluded(-Vector3.right))
                        AddFace(2, 1, 1, SideMaterial.Side);     // Left

                    if (!IsOccluded(Vector3.right))
                        AddFace(3, 1, 1, SideMaterial.Side);     // Right

                    if (!IsOccluded(Vector3.up))
                        AddFace(4, instance.brickResource.colliderSize.z, instance.brickResource.colliderSize.x, SideMaterial.Top);     // Top

                    if (!IsOccluded(-Vector3.up))
                        AddFace(5, instance.brickResource.colliderSize.x, instance.brickResource.colliderSize.z, SideMaterial.Top);     // Bottom
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = System.IO.Path.GetFileName(ctx.assetPath);
            mesh.indexFormat = vertices.Count >= (65536) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

            mesh.SetVertexBufferParams(vertices.Count, vertexAttributes);
            mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count);

            mesh.subMeshCount = 2;
            mesh.SetTriangles(sideIndices[SideMaterial.Side], 0);
            mesh.SetTriangles(sideIndices[SideMaterial.Top], 1);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.UploadMeshData(true);

            root.AddComponent<MeshFilter>().sharedMesh = mesh;

            Material[] materials = new Material[2]
            {
                    UnityEngine.Resources.Load<Material>("Bricks/BrickSide"),
                    UnityEngine.Resources.Load<Material>("Bricks/BrickTop")
            };

            root.AddComponent<MeshRenderer>().sharedMaterials = materials;

            ctx.AddObjectToAsset("mesh", mesh);
            ctx.AddObjectToAsset("saveData", save);

            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
    public enum SideMaterial
    {
        Top,
        Side
    }
    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Color32 color;
        public Vector2 uv;
    }
}
