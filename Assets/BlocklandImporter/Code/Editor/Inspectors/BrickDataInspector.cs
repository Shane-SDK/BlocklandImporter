using Blockland.Objects;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Blockland
{
    [CustomEditor(typeof(BrickData))]
    public class BrickDataInspector : UnityEditor.Editor
    {
        MeshPreview meshPreview;
        private void OnEnable()
        {
            BrickData brick = target as BrickData;
            meshPreview = new(brick.CreateMesh());
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (meshPreview != null )
            {
                Rect rect = GUILayoutUtility.GetAspectRect(1.0f);
                meshPreview.OnPreviewGUI(rect, GUIStyle.none);
                meshPreview.OnPreviewSettings();
            }
            
        }
        private void OnDisable()
        {
            meshPreview?.Dispose();
        }
    }
}
