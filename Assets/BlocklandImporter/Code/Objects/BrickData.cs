using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Blockland.Objects
{
    public class BrickData : ScriptableObject
    {
        public Vector3Int size;
        public BrickType type;
        public FaceSet[] faceSets;
        public IEnumerable<Face> GetFaces(FaceDirection direction)
        {
            if (faceSets == null)
                yield break;

            Face[] faces = faceSets[(int)direction].faces;
            if (faces == null)
                yield break;

            foreach (Face face in faces)
            {
                yield return face;
            }
        }
        public static BrickData CreateFromReader(Reader reader)
        {    
            BrickData data = ScriptableObject.CreateInstance<BrickData>();
            reader.ReadLine();
            data.size = new Vector3Int(reader.ParseLineInt(0), reader.ParseLineInt(2), reader.ParseLineInt(1));
            switch (reader.ReadLine().ToLower())
            {
                case "brick":
                    data.type = BrickType.Brick; break;
                case "special":
                    data.type = BrickType.Special; break;
                case "specialbrick":
                    data.type = BrickType.SpecialBrick; break;
                default:
                    data.type = BrickType.Brick; break;
            }

            data.faceSets = new FaceSet[7];
            for (int i = 0; i < data.faceSets.Length; i++)
            {
                data.faceSets[i] = new();
            }

            if (data.type == BrickType.Brick)
            {
                void AddFace(FaceDirection face, float uScale, float vScale, TextureFace sideMat)
                {
                    Color color = Color.white;
                    FaceVertex a = default, b = default, c = default, d = default;

                    a.color = color;
                    b.color = color;
                    c.color = color;
                    d.color = color;

                    switch (face)
                    {
                        case FaceDirection.South:     // Front
                            a.position = new Vector3(0, 0, 0);                   // 0
                            b.position = new Vector3(0, data.size.y, 0);              // 2
                            c.position = new Vector3(data.size.x, 0, 0);              // 1
                            d.position = new Vector3(data.size.x, data.size.y, 0);         // 3
                            break;
                        case FaceDirection.North:     // Back
                            a.position = new Vector3(0, 0, data.size.z);              // 4
                            b.position = new Vector3(data.size.x, 0, data.size.z);         // 5
                            c.position = new Vector3(0, data.size.y, data.size.z);         // 6
                            d.position = new Vector3(data.size.x, data.size.y, data.size.z);    // 7
                            break;
                        case FaceDirection.West:     // Left
                            a.position = new Vector3(0, 0, 0);                   // 0
                            b.position = new Vector3(0, 0, data.size.z);              // 4
                            c.position = new Vector3(0, data.size.y, 0);              // 2
                            d.position = new Vector3(0, data.size.y, data.size.z);         // 6
                            break;
                        case FaceDirection.East:     // Right
                            a.position = new Vector3(data.size.x, 0, 0);              // 1
                            b.position = new Vector3(data.size.x, data.size.y, 0);         // 3
                            c.position = new Vector3(data.size.x, 0, data.size.z);         // 5
                            d.position = new Vector3(data.size.x, data.size.y, data.size.z);    // 7
                            break;
                        case FaceDirection.Top:     // Top
                            a.position = new Vector3(0, data.size.y, 0);              // 2
                            b.position = new Vector3(0, data.size.y, data.size.z);         // 6
                            c.position = new Vector3(data.size.x, data.size.y, 0);         // 3
                            d.position = new Vector3(data.size.x, data.size.y, data.size.z);    // 7
                            break;
                        case FaceDirection.Bottom:     // Bottom
                            a.position = new Vector3(0, 0, 0);                   // 0
                            b.position = new Vector3(data.size.x, 0, 0);              // 1
                            c.position = new Vector3(0, 0, data.size.z);              // 4
                            d.position = new Vector3(data.size.x, 0, data.size.z);         // 5
                            break;
                    }

                    Vector3 offset = (Vector3)data.size / 2.0f;
                    a.position -= offset;
                    b.position -= offset;
                    c.position -= offset;
                    d.position -= offset;

                    a.uv = new Vector2(1 * uScale, 0 * vScale);
                    b.uv = new Vector2(1 * uScale, 1 * vScale);
                    c.uv = new Vector2(0 * uScale, 0 * vScale);
                    d.uv = new Vector2(0 * uScale, 1 * vScale);

                    FaceSet set = data.faceSets[(int)face];
                    set.faces = new Face[1];
                    set.faces[0] = new Face { a = a, b = b, c = d, d = c, color = color, colorOverride = false, texture = sideMat };
                }

                AddFace(FaceDirection.North, 1, 1, TextureFace.Side);     // Front
                AddFace(FaceDirection.South, 1, 1, TextureFace.Side);     // Back
                AddFace(FaceDirection.West, 1, 1, TextureFace.Side);     // Left
                AddFace(FaceDirection.East, 1, 1, TextureFace.Side);     // Right
                AddFace(FaceDirection.Top, data.size.x, data.size.z, TextureFace.Top);     // Top
                AddFace(FaceDirection.Bottom, data.size.z, data.size.x, TextureFace.BottomLoop);     // Bottom

                return data;
            }

            if (data.type == BrickType.Special)
            {
                // brick volume data
                // size x == how many columns
                // size y == how many rows
                // size z == how many sets of columns and rows

                for (int i = 0; i < (data.size.y * data.size.z); i++)
                {
                    reader.ReadNextNonEmptyLine();
                }
            }

            // collision data ???
            reader.ReadNextNonEmptyLine();  // read collision count ???
            int collisionCount = reader.ParseLineInt(0);
            reader.SkipNonEmptyLine(collisionCount * 2);  // skip sets of offset + size boxes

            // skip coverage section
            reader.ReadLine();
            if (reader.Line.ToLower().Contains("coverage"))
                reader.SkipNonEmptyLine(6);

            // should be at the quad section now
            for (int i = 0; i < 7; i++)
            {
                ReadFaces(data.faceSets[i], reader);
            }

            return data;
        }
        static void ReadFaces(FaceSet set, Reader reader)
        {
            // first line that has an int will denote the face count
            int faceCount = 0;
            while (!reader.EndOfStream)
            {
                reader.ReadNextNonEmptyLine();
                if (reader.TryParseLineElement(0, out faceCount))
                {
                    break;
                }
            }

            set.faces = new Face[faceCount];
            if (faceCount == 0)
                return;
            Vector3[] positionBuffer = new Vector3[4];
            Color[] colorBuffer = new Color[4];
            Vector2[] uvBuffer = new Vector2[4];
            for (int i = 0; i < set.faces.Length; i++)
            {
                Face face = new Face();
                string textureLine = reader.ReadNextNonEmptyLine();
                int colonIndex = textureLine.IndexOf(':');
                if (colonIndex == -1)
                    return;

                face.texture = ParseTextureFace(textureLine.Substring(colonIndex + 1, textureLine.Length - colonIndex - 1));
                
                // read positions
                reader.ReadNextNonEmptyLine();  // position header
                for (int v = 0; v < 4; v++)
                {
                    reader.ReadNextNonEmptyLine();
                    Vector3 position = new Vector3(reader.ParseLineFloat(0), reader.ParseLineFloat(2), reader.ParseLineFloat(1));
                    positionBuffer[v] = position;
                }

                // read uvs
                reader.ReadNextNonEmptyLine();  // header
                for (int v = 0; v < 4; v++)
                {
                    reader.ReadNextNonEmptyLine();
                    Vector2 uv = new Vector2(reader.ParseLineFloat(0), reader.ParseLineFloat(1));
                    uvBuffer[v] = uv;
                }

                if ((char.ToLower((char)reader.Peek())) == 'c')  // colors
                {
                    reader.ReadLine();  // header
                    face.colorOverride = true;
                    for (int v = 0; v < 4; v++)
                    {
                        reader.ReadNextNonEmptyLine();
                        colorBuffer[v] = new Color(reader.ParseLineFloat(0), reader.ParseLineFloat(1), reader.ParseLineFloat(2), reader.ParseLineFloat(3));
                    }
                }
                else
                {
                    System.Array.Fill(colorBuffer, Color.white);
                }

                //void Swap(int a, int b)
                //{
                //    Vector2 temp = uvBuffer[a];
                //    uvBuffer[a] = uvBuffer[b];
                //    uvBuffer[b] = temp;
                //}

                //Swap(0, 1);
                //Swap(3, 2);

                for (int v = 0; v < 4; v++)
                {
                    face[v] = new FaceVertex { position = positionBuffer[v], uv = uvBuffer[v], color = colorBuffer[v] };
                }
                reader.SkipNonEmptyLine(5);  // normal data

                set.faces[i] = face;
            }
        }
        public static TextureFace ParseTextureFace(string text)
        {
            switch (text.ToLower())
            {
                case "bottomedge":
                    return TextureFace.BottomEdge;
                case "bottomloop":
                    return TextureFace.BottomLoop;
                case "side":
                    return TextureFace.Side;
                case "ramp":
                    return TextureFace.Ramp;
                case "top":
                    return TextureFace.Top;
                case "print":
                    return TextureFace.Print;
            }

            return TextureFace.Side;
        }
    }
}
