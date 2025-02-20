using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Blockland
{
    public class SaveReader
    {
        public List<BrickInstance> bricks = new List<BrickInstance>();
        public Color[] colors = new Color[64];
        public SaveReader(Reader reader)
        {
            reader.ReadLine();  // skip first line

            // Read description
            if (!int.TryParse(reader.ReadLine(), out int descLineCount)) return;

            for (int i = 0; i < descLineCount; i++)
                reader.ReadLine();

            // Read color table
            for (int i = 0; i < 64; i++)
            {
                reader.ReadLine();
                colors[i] = new Color(reader.ReadLineFloat(0), reader.ReadLineFloat(1), reader.ReadLineFloat(2), reader.ReadLineFloat(3));
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
                    Vector3 position = Blockland.BlocklandUnitsToStuds(new Vector3(reader.ReadLineFloat(1), reader.ReadLineFloat(3), reader.ReadLineFloat(2)));
                    int colorIndex = reader.ReadLineInt(6);

                    byte angle = (byte)Mathf.RoundToInt(reader.ReadLineFloat(4));

                    bricks.Add(new BrickInstance { brickResource = brickResource, position = position, angle = angle, color = colors[colorIndex] });

                }

            }
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
