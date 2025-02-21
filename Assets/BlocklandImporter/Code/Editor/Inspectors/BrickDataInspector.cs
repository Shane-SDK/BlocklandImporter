using Blockland.Meshing;
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
            MeshBuilder meshBuilder = new MeshBuilder();
            meshBuilder.AddBrick(new BrickInstance { angle = 0, color = Color.red, data =  brick, position = Vector3.zero });
            meshPreview = new(meshBuilder.CreateMesh());
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (meshPreview != null )
            {
                GUI.enabled = true;
                Rect rect = GUILayoutUtility.GetAspectRect(1.0f);
                meshPreview.OnPreviewGUI(rect, GUIStyle.none);
                meshPreview.OnPreviewSettings();
                GUI.enabled = false;
            }
            
        }
        private void OnDisable()
        {
            meshPreview?.Dispose();
        }
    }
}
