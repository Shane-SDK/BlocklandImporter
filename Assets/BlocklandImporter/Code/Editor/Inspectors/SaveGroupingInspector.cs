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
                    // selection bounds are in stud space, transform to World/Unity
                    VolumeSelection selection = group.volumes[i];

                    handle.handleColor = Color.red;
                    handle.size = Blockland.StudsToUnity(selection.bounds.size);
                    handle.center = Blockland.StudsToUnity(selection.bounds.center);

                    EditorGUI.BeginChangeCheck();
                    handle.DrawHandle();
                    Handles.color = Color.white;
                    UnityEditor.Handles.Label(handle.center, group.name);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(saveGroup, "Change Bounds");
                        // user changed the bounds
                        selection.bounds.center = Blockland.UnityToStuds(handle.center);
                        selection.bounds.size = Blockland.UnityToStuds(handle.size);

                        group.volumes[i] = selection;
                        EditorUtility.SetDirty(this);
                    }

                    Vector3 center = Blockland.StudsToUnity(selection.bounds.center);
                    Vector3 scale = Blockland.StudsToUnity(selection.bounds.size);
                    Undo.RecordObject(saveGroup, "Change Transform");
                    Handles.TransformHandle(ref center, Quaternion.identity, ref scale);
                    selection.bounds.center = Blockland.UnityToStuds(center);
                    selection.bounds.size = Blockland.UnityToStuds(scale);

                    group.volumes[i] = selection;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        public static Bounds TransformUnityToStuds(Bounds worldBounds)
        {
            Vector3 center = Blockland.UnityToStuds(worldBounds.center);
            Vector3 size = Blockland.UnityToStuds(worldBounds.size);

            return new Bounds(center, size);
        }
        public static Bounds TransformStudsToUnity(Bounds studBounds)
        {
            Vector3 center = Blockland.StudsToUnity(studBounds.center);
            Vector3 size = Blockland.StudsToUnity(studBounds.size);

            return new Bounds(center, size);
        }
    }
}
