using UnityEngine;

namespace Blockland.Resources
{
    public static class ResourceFactory
    {
        public static Vector3 ParseLine(string line)
        {
            Vector3 vector = new Vector3(1, 1, 1);
            string[] numbers = line.Split(' ');
            for (int i = 0; i < Mathf.Min(3, numbers.Length); i++)
                if (float.TryParse(numbers[i], out float result))
                    vector[i] = result;

            return vector;
        }

        public static bool CreateResource<T>(System.IO.StreamReader reader, ResourceType type, out T resource) where T : IResource
        {
            switch (type)
            {
                case ResourceType.Brick:
                    // Determine which type of brick/shape this is
                    Vector3 colliderSize = ParseLine(reader.ReadLine());
                    colliderSize = new Vector3(colliderSize.x, colliderSize.z, colliderSize.y);

                    string brickTypeString = reader.ReadLine().ToLower();
                    Brick.BrickType brickType = Brick.BrickType.Brick;
                    if (brickTypeString == "brick")
                        brickType = Brick.BrickType.Brick;
                    else if (brickTypeString == "special")
                        brickType = Brick.BrickType.Mesh;

                    resource = (T)(IResource)new Brick { brickGeometryType = brickType, colliderSize = colliderSize };  // ??? cast goop
                    return true;
            }

            resource = default;
            return false;
        }
    }
}
