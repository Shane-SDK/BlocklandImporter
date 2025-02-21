using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Blockland
{
    public class Data : ScriptableObject
    {
        public string this[string key]
        {
            get
            {
                return values[keyMap[key]];
            }
        }
        [SerializeField]
        List<string> keys = new();
        [SerializeField] 
        List<string> values = new();
        [SerializeField]
        Dictionary<string, int> keyMap = new();
        private void OnEnable()
        {
            RefreshMap();
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload += AssemblyReloadEvents_afterAssemblyReload;
#endif
        }
        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload -= AssemblyReloadEvents_afterAssemblyReload;
#endif
        }

        private void AssemblyReloadEvents_afterAssemblyReload()
        {
            RefreshMap();
        }

        public bool TryGetValue(string key, out string value)
        {
            if (keyMap.TryGetValue(key, out int index))
            {
                value = values[index];
                return true;
            }

            value = string.Empty;
            return false;
        }
        public void InsertData(StreamReader reader)
        {
            RefreshMap();
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine().ToLower();
                int separatorIndex = line.IndexOf(',');

                if (separatorIndex == -1) continue;
                int keyStart = 0;
                int keyLength = separatorIndex;

                int valueStart = separatorIndex + 1;
                int valueLength = line.Length - valueStart;

                string value = line.Substring(keyStart, keyLength);
                string key = line.Substring(valueStart, valueLength);

                if (keyMap.TryGetValue(key, out int keyIndex))
                {
                    // overwrite
                    keys[keyIndex] = key;
                    values[keyIndex] = value;
                }
                else
                {
                    keyMap[key] = keys.Count;
                    keys.Add(key);
                    values.Add(value);
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        public void RefreshMap()
        {
            if (keyMap == null)
                keyMap = new Dictionary<string, int>();

            keyMap.Clear();

            for (int i = 0; i < keys.Count; i++)
            {
                keyMap[keys[i]] = i;
            }
        }
    }
}
