using UnityEngine;

namespace Blockland.Objects
{
    [System.Serializable]
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
