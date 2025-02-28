using Blockland.Objects;
using System.Collections.Generic;
using UnityEngine;

namespace Blockland.Meshing
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
        public void SetUV(int index, Vector2 uv)
        {
            index = index % 4;

            if (index == 0)
                a.uv = uv;
            else if (index == 1)
                b.uv = uv;
            else if (index == 2)
                c.uv = uv;
            else if (index == 3)
                d.uv = uv;
        }
        public int IndexOf(Vector3 point)
        {
            for (int i = 0; i < 4; i++)
            {
                if (this[i].position == point)
                    return i;
            }

            return 0;
        }
        public Edge GetEdge(Side type)
        {
            switch (type)
            {
                case Side.Bottom:
                    return new Edge(this[1].position, this[2].position);
                case Side.Left:
                    return new Edge(this[2].position, this[3].position);
                case Side.Right:
                    return new Edge(this[1].position, this[0].position);
                case Side.Top:
                    return new Edge(this[3].position, this[0].position);
            }

            return default;
        }
        public IEnumerable<Edge> GetEdges()
        {
            yield return GetEdge(Side.Bottom);
            yield return GetEdge(Side.Left);
            yield return GetEdge(Side.Right);
            yield return GetEdge(Side.Top);
        }
        static public void GetIndices(Side side, out int a, out int b)
        {
            if (side == Side.Bottom)
            {
                a = 1;
                b = 2;
                return;
            }
            if (side == Side.Top)
            {
                a = 0;
                b = 3;
                return;
            }
            if (side == Side.Left)
            {
                a = 2;
                b = 3;
                return;
            }
            if (side == Side.Right)
            {
                a = 0;
                b = 1;
                return;
            }

            a = 0;
            b = 0;
            return;
        }
        public Side GetSideOfEdge(Edge edge)
        {
            if (edge.Equals(GetEdge(Side.Left), edge)) return Side.Left;
            if (edge.Equals(GetEdge(Side.Right), edge)) return Side.Right;
            if (edge.Equals(GetEdge(Side.Top), edge)) return Side.Top;
            if (edge.Equals(GetEdge(Side.Bottom), edge)) return Side.Bottom;

            return default;
        }
        public static IEnumerable<Side> GetSides()
        {
            yield return Side.Top;
            yield return Side.Bottom;
            yield return Side.Left;
            yield return Side.Right;
        }
    }
}
