using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using Blockland.Objects;
using Blockland.Meshing;

namespace Blockland.Editor
{
    [ScriptedImporter(0, "bls")]
    public class SaveImporter : ScriptedImporter
    {
        public bool createPrefab = false;
        public bool centerOrigin = false;
        public bool mergeFaces = true;
        public bool generateLightMapUVs = false;
        public override void OnImportAsset(AssetImportContext ctx)
        {
            using FileStream file = System.IO.File.OpenRead(ctx.assetPath);
            Reader reader = new Reader(file);
            SaveData save = SaveData.CreateFromReader(reader);
            save.name = System.IO.Path.GetFileNameWithoutExtension(ctx.assetPath);

            if (createPrefab)
            {
                GameObject go = MeshBuilder.CreateGameObject(save, mergeFaces, generateLightMapUVs, centerOrigin);
                ctx.AddObjectToAsset("prefab", go);
                ctx.AddObjectToAsset("mesh", go.GetComponent<MeshFilter>().sharedMesh);
            }
            
            ctx.AddObjectToAsset("saveData", save);
            ctx.SetMainObject(save);
        }
    }

}
