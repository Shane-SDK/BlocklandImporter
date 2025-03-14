using Blockland.Group;
using Blockland.Objects;
using UnityEditor;
using UnityEngine;

namespace Blockland
{
    [CustomEditor(typeof(SaveData))]
    public class SaveDataInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            SaveData save = target as SaveData;

            GUI.enabled = false;
            EditorGUILayout.LabelField($"{save.bricks.Count} bricks");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bounds"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("colorMap"));

            GUI.enabled = true;
            if (GUILayout.Button("Create Grouping Data"))
            {
                SaveGrouping group = SaveGrouping.CreateFromSave(save);
                string path = AssetDatabase.GetAssetPath(save);

                if (string.IsNullOrEmpty(path)) return;

                path = $"{System.IO.Path.GetDirectoryName(path)}/{group.name} Group.asset";
                if (System.IO.File.Exists(path))
                {
                    return;   
                }

                UnityEditor.AssetDatabase.CreateAsset(group, path);
            }
        }
    }
}
