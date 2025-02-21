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
        public const float plateStudRatio = 3.2f / 8.0f;
        public const float metricToStudFactor = 2.0f;  // 0.5 => 1 stud apart
        public const float metricToPlateFactor = 5.0f;   // 0.2 => 1 plate apart
        public static Resources.Resources resources;
        public static Settings settings;
        public static Data brickUINameTable;
        static Blockland()
        {
            settings = LoadAsset<Settings>("Settings", "t: Blockland.Settings");
            brickUINameTable = LoadAsset<Data>("BrickUINameTable", "t: Blockland.Data");
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
            studs *= settings.studToUnityScaleFactor;
            studs.y *= plateStudRatio;
            return studs;
        }
        public static Direction GetOppositeDirection(Direction dir)
        {
            if (dir == Direction.Left) return Direction.Right;
            if (dir == Direction.Right) return Direction.Left;
            if (dir == Direction.Up) return Direction.Down;
            if (dir == Direction.Down) return Direction.Up;
            if (dir == Direction.Forward) return Direction.Backward;
            if (dir == Direction.Backward) return Direction.Forward;

            return Direction.Forward;
        }
        public static T LoadAsset<T>(string name, string typeFilter) where T : UnityEngine.ScriptableObject
        {
            T instance = UnityEngine.Resources.Load<T>(name);

#if UNITY_EDITOR    // load from asset database first
            if (instance == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets(typeFilter);
                foreach (string stringGuid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(stringGuid);
                    instance = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                    if (instance != null)
                        break;
                }
            }
#endif
            if (instance == null)
            {
                // create new one
                instance = ScriptableObject.CreateInstance<T>();
                instance.name = name;
#if UNITY_EDITOR
                string path = $"Assets/BlocklandImporter/Resources/{name}.asset";
                UnityEditor.AssetDatabase.CreateAsset(instance, path);
                Debug.Log($"Created {typeof(T)}, {name} at {path}", instance);
#endif
            }

            return instance;
        }
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Blockland/Load Assets")]
        public static void LoadAssets()
        {
            Blockland.settings = Blockland.LoadAsset<Settings>("Settings", "t: Blockland.Settings");
            Blockland.brickUINameTable = Blockland.LoadAsset<Data>("BrickUINameTable", "t: Blockland.Data");

            brickUINameTable.RefreshMap();
        }
#endif
    }
    public enum Direction
    {
        Forward,
        Backward,
        Up,
        Down,
        Left,
        Right
    }
}
