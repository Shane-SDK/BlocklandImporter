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

            if (bottomEdge.sqrMagnitude < float.Epsilon) return false;
            if (topEdge.sqrMagnitude < float.Epsilon) return false;
            if (leftEdge.sqrMagnitude < float.Epsilon) return false;
            if (rightEdge.sqrMagnitude < float.Epsilon) return false;

            if (Mathf.Abs(Vector3.Dot(bottomEdge, topEdge)) != 1.0f)
                return false;

            if (Mathf.Abs(Vector3.Dot(leftEdge, rightEdge)) != 1.0f)
                return false;

            if (Mathf.Abs(Vector3.Dot(leftEdge, bottomEdge)) != 0.0f)
                return false;

            return true;
        }
        public Color AveragedColor()
        {
            if (colorOverride) return color;

            return (a.color + b.color + c.color + d.color) / 4.0f;
        }
        public void SetPosition(int index, Vector3 pos)
        {
            index = index % 4;

            if (index == 0)
                a.position = pos;
            else if (index == 1)
                b.position = pos;
            else if (index == 2)
                c.position = pos;
            else if (index == 3)
                d.position = pos;
        }
    }
}
