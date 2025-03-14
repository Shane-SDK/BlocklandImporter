using Blockland.Editor.Windows;
using Blockland.Group;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Blockland
{
    [CustomEditor(typeof(SaveGrouping))]
    public class SaveGroupingInspector : UnityEditor.Editor
    {
        UnityEditor.IMGUI.Controls.BoxBoundsHandle handle;
        private void OnEnable()
        {
            SceneView.duringSceneGui += SceneGUI;
            handle = new UnityEditor.IMGUI.Controls.BoxBoundsHandle();
            handle.handleColor = Color.red;
        }
        private void OnDisable()
        {
            SceneView.duringSceneGui -= SceneGUI;
        }
        public override void OnInspectorGUI()
        {
            SaveGrouping saveGroup = (SaveGrouping)target;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("save"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("groups"));

            GUI.enabled = true;
            if (GUILayout.Button("Open Editor"))
            {
                SaveEditor editor = SaveEditor.CreateInstance<SaveEditor>();
                StageUtility.GoToStage(editor, true);
                editor.SetGroup(target as SaveGrouping);
            }

            serializedObject.ApplyModifiedProperties();
        }
        private void SceneGUI(SceneView view)
        {
            SerializedProperty property = serializedObject.FindProperty("groups");
            if (!property.isExpanded) return;

            SaveGrouping saveGroup = (SaveGrouping)target;

            foreach (Group.Group group in  saveGroup.groups)
            {
                for (int i = 0; i < group.volumes.Count; i++)
                {
                    VolumeSelection selection = group.volumes[i];

                    handle.handleColor = Color.red;
                    handle.size = selection.bounds.size;
                    handle.center = selection.bounds.center;

                    EditorGUI.BeginChangeCheck();
                    handle.DrawHandle();
                    Handles.color = Color.white;
                    UnityEditor.Handles.Label(handle.center, group.name);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(saveGroup, "Change Bounds");
                        // user changed the bounds
                        selection.bounds.center = handle.center;
                        selection.bounds.size = handle.size;

                        group.volumes[i] = selection;
                        EditorUtility.SetDirty(this);
                    }

                    Vector3 center = selection.bounds.center;
                    Vector3 scale = selection.bounds.size;
                    Undo.RecordObject(saveGroup, "Change Transform");
                    Handles.TransformHandle(ref center, Quaternion.identity, ref scale);
                    selection.bounds.center = center;
                    selection.bounds.size = scale;

                    group.volumes[i] = selection;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
