using System.Collections.Generic;
using UnityEngine;

namespace Blockland.Meshing
{
    public struct Edge : IEqualityComparer<Edge>
    {
        public Vector3 this[int i]
        {
            get
            {
                if (i % 2 == 0)
                    return a;
                else
                    return b;
            }
            set
            {
                if (i % 2 == 0)
                {
                    a = value;
                }
                else b = value;
            }
        }
        public float Length => (a - b).magnitude;
        public Vector3 Direction => (b - a).normalized;
        public Vector3 a;
        public Vector3 b;
        public Edge(Vector3 a, Vector3 b)
        {
            this.a = a;
            this.b = b;
        }
        public bool ContainsPoint(Vector3 c)
        {
            return c == a || c == b;
        }
        static public Side GetAdjacentSide(Side type)
        {
            if (type == Side.Bottom || type == Side.Top)
                return Side.Left;
            if (type == Side.Left || type == Side.Right)
                return Side.Top;

            return default;
        }
        static public Side GetOppositeSide(Side type)
        {
            if (type == Side.Bottom) return Side.Top;
            if (type == Side.Top) return Side.Bottom;
            if (type == Side.Right) return Side.Left;
            if (type == Side.Left) return Side.Right;

            return default;
        }
        public bool Equals(Edge a, Edge b)
        {
            if (Compare(a.a, b.a) && Compare(a.b, b.b)) return true;
            if (Compare(a.a, b.b) && Compare(a.b, b.a)) return true;

            return false;
        }
        public int GetHashCode(Edge obj)
        {
            return obj.a.GetHashCode() + obj.b.GetHashCode();
        }
        public static bool AreOpposite(Side a, Side b)
        {
            if (a == Side.Left && b == Side.Right) return true;
            if (a == Side.Right && b == Side.Left) return true;
            if (a == Side.Top && b == Side.Bottom) return true;
            if (a == Side.Bottom && b == Side.Top) return true;

            return false;
        }
        public static void GetConnectingSides(Side side, out Side sideA, out Side sideB)
        {
            if (side == Side.Left)
            {
                sideA = Side.Top;
                sideB = Side.Bottom;
                return;
            }
            if (side == Side.Right)
            {
                sideA = Side.Top;
                sideB = Side.Bottom;
                return;
            }
            if (side == Side.Top)
            {
                sideA = Side.Left;
                sideB = Side.Right;
                return;
            }
            if (side == Side.Bottom)
            {
                sideA = Side.Left;
                sideB = Side.Right;
                return;
            }

            sideA = default;
            sideB = default;
        }
        public static bool IsParallel(Edge a, Edge b)
        {
            return 1.0f - Mathf.Abs(Vector3.Dot(a.Direction, b.Direction)) <= float.Epsilon;
        }
        public static bool IsPerpendicular(Edge a, Edge b)
        {
            return Mathf.Abs(Vector3.Dot(a.Direction, b.Direction)) <= float.Epsilon;
        }
        public static bool Compare(Vector3 a, Vector3 b)
        {
            return (a - b).magnitude <= float.Epsilon;
        }
    }
}
