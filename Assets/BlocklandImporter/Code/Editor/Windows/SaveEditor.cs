using UnityEngine;
using UnityEditor;
using Blockland.Meshing;
using UnityEditor.SceneManagement;
using Blockland.Group;

namespace Blockland.Editor.Windows
{
    public class SaveEditor : PreviewSceneStage
    {
        SaveGrouping group;
        public void SetGroup(SaveGrouping group)
        {
            this.group = group;

            GameObject go = MeshBuilder.CreateGameObject(group.save, true, false, true);
            EditorSceneManager.MoveGameObjectToScene(go, this.scene);
        }
        protected override bool OnOpenStage()
        {
            return base.OnOpenStage();
        }
        protected override GUIContent CreateHeaderContent()
        {
            return new GUIContent($"Save Editor");
        }
    }
}
