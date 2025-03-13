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
        }
    }
}
