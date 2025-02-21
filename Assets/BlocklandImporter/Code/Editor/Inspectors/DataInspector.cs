using Blockland.Objects;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Blockland
{
    [CustomEditor(typeof(Data))]
    public class DataInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Import Data"))
            {
                string dir = UnityEditor.EditorUtility.OpenFilePanel("Open data", "Assets/BlocklandImporter/Assets/", "txt");
                if (System.IO.File.Exists(dir))
                {
                    using FileStream file = File.OpenRead(dir);
                    using StreamReader reader = new StreamReader(file);

                    (target as Data).InsertData(reader);
                }
            }
        }
    }
}
