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
        public Color[] colorMap = new Color[64];
        public List<BrickInstance> bricks = new List<BrickInstance>();
        public static SaveData CreateFromReader(Reader reader)
        {
            SaveData saveData = ScriptableObject.CreateInstance<SaveData>();
            reader.ReadLine();  // skip first line

            // Read description
            if (!int.TryParse(reader.ReadLine(), out int descLineCount)) return saveData;

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

                    saveData.bricks.Add(new BrickInstance { data = brickResource, position = position, angle = angle, color = saveData.colorMap[colorIndex] });
                }
                else
                {
                    Debug.LogWarning($"Could not find asset {new Resources.ResourcePath(dataPath).AssetDatabasePath}");
                }

            }

            return saveData;
        }
    }
    public struct BrickInstance
    {
        public float Angle => angle * 90.0f;
        public Objects.BrickData data;
        public Vector3 position;
        public byte angle;
        public Color color;

        public void GetTransformedBounds(out Bounds bounds)
        {
            Vector3 size = Quaternion.AngleAxis(Angle, Vector3.up) * data.size;
            for (int i = 0; i < 3; i++)
                size[i] = Mathf.Abs(size[i]);

            bounds = new Bounds(position, size);
        }
        public void GetTransformedBounds(out BoundsInt intBounds)
        {
            GetTransformedBounds(out Bounds bounds);

            intBounds = new BoundsInt(Vector3Int.FloorToInt(bounds.center - bounds.size / 2.0f), Vector3Int.RoundToInt(bounds.size));
        }
        /// <summary>
        /// World to local in STUDS
        /// </summary>
        /// <param name="studWorld"></param>
        /// <returns></returns>
        public Vector3 InverseTransformPoint(Vector3 studWorld)
        {
            Vector3 local = studWorld - position;
            local = Quaternion.AngleAxis(-Angle, Vector3.up) * local;
            return local;
        }
    }
}
