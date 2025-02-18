using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Blockland
{
    public class SaveReader
    {
        public List<BrickInstance> bricks = new List<BrickInstance>();
        public Color[] colors = new Color[64];
        public SaveReader(StreamReader reader)
        {
            reader.ReadLine();  // skip first line

            // Read description
            if (!int.TryParse(reader.ReadLine(), out int descLineCount)) return;

            for (int i = 0; i < descLineCount; i++)
                reader.ReadLine();

            // Read color table
            for (int i = 0; i < 64; i++)
            {
                colors[i] = ReadColor(reader);
            }

            // Read linecount
            if (!int.TryParse(reader.ReadLine().Split(' ', options: System.StringSplitOptions.RemoveEmptyEntries)[^1], out int lineCount)) return;

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
                int quoteIndex = line.IndexOf('"');
                string brickUIName = line.Substring(0, quoteIndex);
                // Load resource
                if (Blockland.resources.GetResource(new Resources.ResourcePath(brickUIName + ".blb"), out Resources.Brick brickResource))
                {
                    string[] brickStringData = line.Substring(quoteIndex + 1, line.Length - (quoteIndex + 1)).Split(' ', options: System.StringSplitOptions.RemoveEmptyEntries);
                    float ReadValue(int index)
                    {
                        if (float.TryParse(brickStringData[index], out float result))
                            return result;
                        else return 0;
                    }
                    int ReadInt(int index)
                    {
                        if (int.TryParse(brickStringData[index], out int result))
                            return result;
                        else return 0;
                    }

                    if (brickStringData.Length < 3) return;
                    Vector3 position = Blockland.BlocklandUnitsToStuds(new Vector3(ReadValue(0), ReadValue(2), ReadValue(1)));
                    int colorIndex = ReadInt(5);

                    byte angle = (byte)Mathf.RoundToInt(ReadValue(3));

                    bricks.Add(new BrickInstance { brickResource = brickResource, position = position, angle = angle, color = colors[colorIndex] });

                }

            }
        }
        public Color ReadColor(StreamReader reader)
        {
            string[] lineValues = reader.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

            Color color = new Color();

            for (int i = 0; i < Mathf.Min(4, lineValues.Length); i++)
            {
                if (float.TryParse(lineValues[i], out float value))
                {
                    color[i] = value;
                }
            }

            return color;
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
