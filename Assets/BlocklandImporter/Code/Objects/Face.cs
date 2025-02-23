using UnityEngine;

namespace Blockland.Objects
{
    [System.Serializable]
    public struct Face
    {
        public Plane Plane => new Plane(a.position, c.position, b.position);
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
        public bool IsOrth()
        {
            Vector3 bottomEdge = (this[1].position - this[2].position).normalized;
            Vector3 topEdge = (this[0].position - this[3].position).normalized;

            Vector3 leftEdge = (this[2].position - this[3].position).normalized;
            Vector3 rightEdge = (this[0].position - this[1].position).normalized;

            if (Mathf.Abs(Vector3.Dot(bottomEdge, topEdge)) != 1.0f)
                return false;

            if (Mathf.Abs(Vector3.Dot(leftEdge, rightEdge)) != 1.0f)
                return false;

            if (Mathf.Abs(Vector3.Dot(leftEdge, bottomEdge)) != 0.0f)
                return false;

            return true;
        }
    }
}
