using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blockland.Objects
{
    public class BrickData : ScriptableObject
    {
        public Vector3Int size;
        public BrickType type;
        public FaceSet[] faceSets;
        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new();
            List<Color> colors = new();
            List<int> indices = new();

            foreach (FaceSet set in faceSets)
            {
                if (set.faces == null) continue;

                foreach (Face face in set.faces)
                {
                    int offset = vertices.Count;
                    indices.Add(offset + 0);
                    indices.Add(offset + 1);
                    indices.Add(offset + 3);
                    indices.Add(offset + 1);
                    indices.Add(offset + 2);
                    indices.Add(offset + 3);

                    for (int i = 0; i < 4; i++)
                    {
                        vertices.Add(face[i].position);
                        colors.Add(face[i].color);
                    }
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.SetColors(colors);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.UploadMeshData(true);

            return mesh;
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

            if (data.type == BrickType.Brick)
                return data;

            data.faceSets = new FaceSet[7];
            for (int i = 0; i < data.faceSets.Length; i++)
            {
                data.faceSets[i] = new();
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

                face.texture = ParseTextureFace(textureLine.Substring(colonIndex, textureLine.Length - colonIndex));
                
                // read positions
                reader.ReadNextNonEmptyLine();  // position header
                for (int v = 0; v < 4; v++)
                {
                    reader.ReadNextNonEmptyLine();
                    Vector3 position = new Vector3(reader.ParseLineFloat(0), reader.ParseLineFloat(2), reader.ParseLineFloat(1));
                    position = Blockland.StudsToUnity(Blockland.BlocklandUnitsToStuds(position));
                    position.y *= Blockland.plateStudRatio;
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
                case "side":
                    return TextureFace.Side;
                case "ramp":
                    return TextureFace.Ramp;
                case "top":
                    return TextureFace.Top;
            }

            return TextureFace.Side;
        }
    }
    public enum BrickType
    {
        Brick,
        Special,
        SpecialBrick
    }
    public enum TextureFace
    {
        BottomEdge,
        Ramp,
        Side,
        Top
    }
    [System.Serializable]
    public class FaceSet
    {
        public Face[] faces;
    }
    [System.Serializable]
    public struct Face
    {
        public FaceVertex this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return a;
                    case 1: return b;
                    case 2: return c;
                    case 3: return d;
                    default: return default;
                }
            }
            set
            {
                switch (i)
                {
                    case 0: a = value; return;
                    case 1: b = value; return;
                    case 2: c = value; return;
                    case 3: d = value; return;
                }
            }
        }
        public TextureFace texture;
        public FaceVertex a, b, c, d;
        public bool colorOverride;
        public Color color;
    }
    [System.Serializable]
    public struct FaceVertex
    {
        public Vector3 position;
        public Color color;
        public Vector2 uv;
    }
}
