using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using UnityEngine.Rendering;
using Blockland.Objects;

namespace Blockland.Editor
{
    [ScriptedImporter(0, "blb")]
    public class BrickImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            using FileStream file = File.OpenRead(ctx.assetPath);
            using Reader reader = new Reader(file);

            BrickData data = BrickData.CreateFromReader(reader);

            ctx.AddObjectToAsset("data", data);
            ctx.SetMainObject(data);
        }
    }
}
