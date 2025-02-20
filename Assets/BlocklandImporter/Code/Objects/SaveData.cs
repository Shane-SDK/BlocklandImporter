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
                    reader.ReadLine();
                    continue;
                }

                string line = reader.ReadLine();
                UnityEngine.Profiling.Profiler.BeginSample("ParseBlockLine");
                ParseBlockLine(line);
                UnityEngine.Profiling.Profiler.EndSample();
            }

            void ParseBlockLine(string line)
            {
                if (!reader.TryParseLineElement(0, out string brickDesignator) || !brickDesignator.Contains('"')) return;

                int quoteIndex = line.IndexOf('"');
                string brickUIName = line.Substring(0, quoteIndex);
                // Load resource
                if (Blockland.resources.GetResource(new Resources.ResourcePath(brickUIName + ".blb"), out Resources.Brick brickResource))
                {
                    Vector3 position = Blockland.BlocklandUnitsToStuds(new Vector3(reader.ParseLineFloat(1), reader.ParseLineFloat(3), reader.ParseLineFloat(2)));
                    int colorIndex = reader.ParseLineInt(6);

                    byte angle = (byte)Mathf.RoundToInt(reader.ParseLineFloat(4));

                    saveData.bricks.Add(new BrickInstance { brickResource = brickResource, position = position, angle = angle, color = saveData.colorMap[colorIndex] });

                }

            }

            return saveData;
        }
        public struct BrickInstance
        {
            public float Angle => angle * 90.0f;
            public Resources.Brick brickResource;
            public Vector3 position;
            public byte angle;
            public Color color;

            public void GetTransformedBounds(out Bounds bounds)
            {
                Vector3 size = Quaternion.AngleAxis(Angle, Vector3.up) * brickResource.colliderSize;
                for (int i = 0; i < 3; i++)
                    size[i] = Mathf.Abs(size[i]);

                bounds = new Bounds(position, size);
            }
            public void GetTransformedBounds(out BoundsInt intBounds)
            {
                GetTransformedBounds(out Bounds bounds);

                intBounds = new BoundsInt(Vector3Int.FloorToInt(bounds.center - bounds.size / 2.0f), Vector3Int.RoundToInt(bounds.size));
            }
        }
    }
}
