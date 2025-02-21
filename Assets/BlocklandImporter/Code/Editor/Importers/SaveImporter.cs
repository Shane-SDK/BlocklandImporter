using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using UnityEngine.Rendering;
using Blockland.Objects;
using Blockland.Meshing;

namespace Blockland.Editor
{
    [ScriptedImporter(0, "bls")]
    public class SaveImporter : ScriptedImporter
    {
        
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
            MeshBuilder meshBuilder = new(ctx);
            foreach (BrickInstance brick in save.bricks)
                meshBuilder.AddBrick(brick);

            Mesh mesh = meshBuilder.CreateMesh();

            root.AddComponent<MeshFilter>().sharedMesh = mesh;

            Material[] materials = new Material[meshBuilder.textureFaces.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                TextureFace face = meshBuilder.textureFaces[i];
                if (face == TextureFace.Top || face == TextureFace.BottomEdge)
                    materials[i] = UnityEngine.Resources.Load<Material>("Bricks/BrickTop");
                else
                    materials[i] = UnityEngine.Resources.Load<Material>("Bricks/BrickSide");
            }

            root.AddComponent<MeshRenderer>().sharedMaterials = materials;

            ctx.AddObjectToAsset("mesh", mesh);
            ctx.AddObjectToAsset("saveData", save);

            UnityEngine.Profiling.Profiler.EndSample();
        }
    }

}
