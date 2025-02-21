using Blockland.Resources;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blockland
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad()]
#endif
    public static class Blockland
    {
        public static float studToUnityScaleFactor = 8.00f / 19.20f;
        public const float plateStudRatio = 3.2f / 8.0f;
        public const float metricToStudFactor = 2.0f;  // 0.5 => 1 stud apart
        public const float metricToPlateFactor = 5.0f;   // 0.2 => 1 plate apart
        public static Resources.Resources resources;
        public static Settings settings;
        public static Data brickUINameTable;
        static Blockland()
        {
            LoadSettings();
            resources = new();
        }
        public static Vector3 BlocklandUnitsToStuds(Vector3 pos)
        {
            return new Vector3(
                        pos.x * Blockland.metricToStudFactor,
                        pos.y * Blockland.metricToPlateFactor,
                        pos.z * Blockland.metricToStudFactor);
        }
        public static Vector3 StudsToUnity(Vector3 studs)
        {
            studs *= studToUnityScaleFactor;
            studs.y *= plateStudRatio;
            return studs;
        }
        static Settings LoadSettings()
        {
            settings = UnityEngine.Resources.Load<Settings>("Settings");

#if UNITY_EDITOR    // load from asset database first
            if (settings == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t: Blockland.Settings");
                foreach (string stringGuid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(stringGuid);
                    settings = UnityEditor.AssetDatabase.LoadAssetAtPath<Settings>(path);
                    if (settings != null)
                        break;
                }
            }
#endif
            if (settings == null)
            {
                // create new one
                settings = ScriptableObject.CreateInstance<Settings>();
                brickUINameTable = ScriptableObject.CreateInstance<Data>();
                brickUINameTable.name = "BrickUINameTable";
#if UNITY_EDITOR
                string path = "Assets/BlocklandImporter/Resources/Settings.asset";
                UnityEditor.AssetDatabase.CreateAsset(settings, path);
                Debug.Log($"Created Blockland Settings asset at {path}", settings);
                UnityEditor.AssetDatabase.AddObjectToAsset(brickUINameTable, settings);
#endif
            }

            return settings;
        }
    }
}
