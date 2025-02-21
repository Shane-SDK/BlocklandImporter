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
        Dictionary<string, int> keyMap = new();
        public Data()
        {
            for (int i = 0; i < keys.Count; i++)
            {
                keyMap[keys[i]] = i;
            }
        }
        public void InsertData(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine().ToLower();
                int separatorIndex = line.IndexOf(',');

                if (separatorIndex == -1) continue;
                int keyStart = 0;
                int keyLength = separatorIndex;

                int valueStart = separatorIndex + 1;
                int valueLength = line.Length - valueStart;

                string key = line.Substring(keyStart, keyLength);
                string value = line.Substring(valueStart, valueLength);

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
    }
}
