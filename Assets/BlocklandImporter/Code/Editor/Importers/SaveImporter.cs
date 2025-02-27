using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using UnityEngine.Rendering;
using Blockland.Objects;
using Blockland.Meshing;
using System.Collections;

namespace Blockland.Editor
{
    [ScriptedImporter(0, "bls")]
    public class SaveImporter : ScriptedImporter
    {
        public bool mergeFaces = true;
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

            // mesh builder
            MeshBuilder meshBuilder = new();

            List<Face> faces = new List<Face>();
            MeshBuilder.GetFaces(save.bricks, faces, mergeFaces);
            Mesh mesh = MeshBuilder.CreateMesh(faces, out TextureFace[] textureFaces);

            root.AddComponent<MeshFilter>().sharedMesh = mesh;

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

            root.AddComponent<MeshRenderer>().sharedMaterials = materials;

            ctx.AddObjectToAsset("mesh", mesh);
            ctx.AddObjectToAsset("saveData", save);

            UnityEngine.Profiling.Profiler.EndSample();
        }
    }

}
