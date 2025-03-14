using Blockland.Objects;
using Octree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blockland
{
    public static class Extensions
    {
        public static void DrawBox(Vector3 pos, Quaternion rot, Vector3 scale, Color c, float time = 0.1f)
        {
            // create matrix
            Matrix4x4 m = new Matrix4x4();
            m.SetTRS(pos, rot, scale);

            var point1 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, 0.5f));
            var point2 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, 0.5f));
            var point3 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, -0.5f));
            var point4 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, -0.5f));

            var point5 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, 0.5f));
            var point6 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, 0.5f));
            var point7 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, -0.5f));
            var point8 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, -0.5f));

            Debug.DrawLine(point1, point2, c, time);
            Debug.DrawLine(point2, point3, c, time);
            Debug.DrawLine(point3, point4, c, time);
            Debug.DrawLine(point4, point1, c, time);

            Debug.DrawLine(point5, point6, c, time);
            Debug.DrawLine(point6, point7, c, time);
            Debug.DrawLine(point7, point8, c, time);
            Debug.DrawLine(point8, point5, c, time);

            Debug.DrawLine(point1, point5, c, time);
            Debug.DrawLine(point2, point6, c, time);
            Debug.DrawLine(point3, point7, c, time);
            Debug.DrawLine(point4, point8, c, time);

            //// optional axis display
            //Debug.DrawRay(m.GetPosition(), m * Vector3.forward, Color.magenta, time);
            //Debug.DrawRay(m.GetPosition(), m * Vector3.up, Color.yellow, time);
            //Debug.DrawRay(m.GetPosition(), m * Vector3.right, Color.red, time);
        }
        public static System.Numerics.Vector3 Vec3(Vector3 v)
        {
            return new System.Numerics.Vector3(v.x, v.y, v.z);
        }
        public static Vector3 Vec3(System.Numerics.Vector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }
        public static void DrawOctree<T>(BoundsOctree<T> tree, Color nodeColor, Color objectColor, float time = 1)
        {
            Queue<BoundsOctree<T>.Node> nodes = new();
            nodes.Enqueue(tree.Root);
            while (nodes.Count > 0)
            {
                var node = nodes.Dequeue();

                foreach (var child in node.Children)
                    nodes.Enqueue(child);

                Extensions.DrawBox(Extensions.Vec3(node.Center), Quaternion.identity, Extensions.Vec3(node.Bounds.Size), nodeColor, time);

                foreach (var obj in node.Objects)
                {
                    Extensions.DrawBox(Extensions.Vec3(obj.Bounds.Center), Quaternion.identity, Extensions.Vec3(obj.Bounds.Size), objectColor, time);
                }
            }
        }
        public static Octree.BoundingBox BoundingBox(Bounds bounds)
        {
            return new BoundingBox(Vec3(bounds.center), Vec3(bounds.size));
        }
        public static Bounds BoundingBox(BoundingBox bounds)
        {
            return new Bounds(Vec3(bounds.Center), Vec3(bounds.Size));
        }
        public static BoundsOctree<int> OctreeFromSave(SaveData save, float looseness = 1.2f)
        {
            // create octree
            int treeSize = Mathf.Max(save.bounds.size.x, save.bounds.size.y, save.bounds.size.z);
            Octree.BoundsOctree<int> brickTree = new Octree.BoundsOctree<int>(treeSize, Extensions.Vec3(save.bounds.center), 1, looseness);
            for (int i = 0; i < save.bricks.Count; i++)
            {
                save.bricks[i].GetTransformedBounds(out BoundsInt instanceBounds);
                Octree.BoundingBox box = new Octree.BoundingBox(Extensions.Vec3(instanceBounds.center), Extensions.Vec3(instanceBounds.size));
                brickTree.Add(i, box);
            }

            return brickTree;
        }
    }
}
