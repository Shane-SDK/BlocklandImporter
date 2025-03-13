using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blockland.Objects
{
    public class SaveData : ScriptableObject
    {
        public BoundsInt bounds;
        public Color[] colorMap = new Color[64];
        public List<BrickInstance> bricks = new List<BrickInstance>();
        public static SaveData CreateFromReader(Reader reader)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Create Save From Reader");
            SaveData saveData = ScriptableObject.CreateInstance<SaveData>();
            Vector3Int min = Vector3Int.one * int.MaxValue;
            Vector3Int max = Vector3Int.one * int.MinValue;
            reader.ReadLine();  // skip first line

            // Read description
            if (!int.TryParse(reader.ReadLine(), out int descLineCount))
            {
                UnityEngine.Profiling.Profiler.EndSample();
                return saveData;
            }

            for (int i = 0; i < descLineCount; i++)
                reader.ReadLine();

            // Read color table
            for (int i = 0; i < 64; i++)
            {
                reader.ReadLine();
                saveData.colorMap[i] = new Color(reader.ParseLineFloat(0), reader.ParseLineFloat(1), reader.ParseLineFloat(2), reader.ParseLineFloat(3));
            }

            // Read linecount
            reader.ReadLine();

            while (!reader.EndOfStream)
            {
                char peek = (char)reader.Peek();
                if (peek == '+' || char.IsWhiteSpace(peek))
                {
                    reader.SkipLine();
                    continue;
                }

                string line = reader.ReadLine();
                UnityEngine.Profiling.Profiler.BeginSample("ParseBlockLine");
                ParseBlockLine(line);
                UnityEngine.Profiling.Profiler.EndSample();
            }

            void ParseBlockLine(string line)
            {
                if (!line.Contains('"')) return;

                int quoteIndex = line.IndexOf('"');
                string brickUIName = line.Substring(0, quoteIndex).ToLower();
                if (!Blockland.brickUINameTable.TryGetValue(brickUIName, out string dataPath))
                {
                    Debug.LogWarning($"Could not find data from UI name {brickUIName}");
                    return;
                }

                string brickProperties = line.Substring(quoteIndex + 1, line.Length - (quoteIndex + 1));
                reader.SetStringRuns(brickProperties);

                // Load resource
                if (Blockland.resources.LoadBrickData(new Resources.ResourcePath(dataPath), out Objects.BrickData brickResource))
                {
                    Vector3 position = Blockland.BlocklandUnitsToStuds(new Vector3(reader.ParseLineFloat(0), reader.ParseLineFloat(2), reader.ParseLineFloat(1)));
                    int colorIndex = reader.ParseLineInt(5);

                    byte angle = (byte)Mathf.RoundToInt(reader.ParseLineFloat(3));
                    BrickInstance instance = new BrickInstance { data = brickResource, position = position, angle = angle, color = saveData.colorMap[colorIndex] };
                    saveData.bricks.Add(instance);

                    instance.GetTransformedBounds(out BoundsInt bounds);
                    min = Vector3Int.Min(min, bounds.min);
                    max = Vector3Int.Max(max, bounds.max);
                }
                else
                {
                    Debug.LogWarning($"Could not find asset {new Resources.ResourcePath(dataPath).AssetDatabasePath}");
                }

            }

            Vector3Int boundsSize = max - min;
            saveData.bounds = new BoundsInt(min.x, min.y, min.z, boundsSize.x, boundsSize.y, boundsSize.z);

            UnityEngine.Profiling.Profiler.EndSample();
            return saveData;
        }
    }
}
