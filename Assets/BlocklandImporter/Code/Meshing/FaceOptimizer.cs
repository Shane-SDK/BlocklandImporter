using Blockland.Objects;
using PlasticGui.WorkspaceWindow;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Blockland.Meshing
{
    public class FaceOptimizer
    {
        public void OptimizeFaces(IList<Face> input, ICollection<Face> output, int maxIterations = 100)
        {
            // split faces by plane
            // convert to 2D faces
            Dictionary<Plane, FaceSet> faceSets = new();

            // Organize faces by plane, convert faces from world space to plane space
            for (int i = 0; i < input.Count; i++)
            {
                Face face = input[i];
                Plane plane = face.Plane;
                if (plane.normal.sqrMagnitude == 0)  // Bad geometry, skip processing
                {
                    output.Add(face);
                    continue;
                }
                Quaternion rotation = Quaternion.Inverse(Quaternion.LookRotation(plane.normal));

                for (int v = 0; v < 4; v++)
                    face[v] = new FaceVertex { color = face[v].color, uv = face[v].uv, position = rotation * face[v].position };

                // flatten positions using plane
                if (!faceSets.TryGetValue(plane, out FaceSet set))
                {
                    set = new FaceSet(plane);
                    faceSets[plane] = set;
                }

                set.faces.Add(face);
            }

            // Create edge map
            foreach (FaceSet set in faceSets.Values)
            {
                set.CreateEdges();
                set.MergeFaces(maxIterations);
            }

            foreach (FaceSet set in faceSets.Values)
            {
                Quaternion localToWorldRotation = Quaternion.LookRotation(set.plane.normal);
                Vector3 PlaneToWorld(Vector3 local)
                {
                    return localToWorldRotation * local;
                }
                UnityEngine.Random.InitState(0);
                Color color = UnityEngine.Random.ColorHSV();
                foreach (Face face in set.faces)
                {
                    Face worldFace = face;
                    for (int v = 0; v < 4; v++)
                    {
                        FaceVertex vertex = face[v];
                        //vertex.color = color;
                        vertex.position = PlaneToWorld(vertex.position);
                        worldFace[v] = vertex;
                    }

                    output.Add(worldFace);
                }
            }
        }
        public struct Edge : IEqualityComparer<Edge>
        {
            public float Length => (a - b).magnitude;
            public Vector2 a;
            public Vector2 b;
            public Side side;
            public Edge(Vector2 a, Vector2 b, Side type)
            {
                this.a = a;
                this.b = b;
                this.side = type;
            }
            static public IEnumerable<Edge> GetEdges(Face face)
            {
                yield return FromFace(face, Side.Bottom);
                yield return FromFace(face, Side.Left);
                yield return FromFace(face, Side.Right);
                yield return FromFace(face, Side.Top);
            }
            static public Edge FromFace(Face face, Side type)
            {
                switch (type)
                {
                    case Side.Bottom:
                        return new Edge(face.a.position, face.b.position, type);
                    case Side.Left:
                        return new Edge(face.a.position, face.d.position, type);
                    case Side.Right:
                        return new Edge(face.c.position, face.b.position, type);
                    case Side.Top:
                        return new Edge(face.c.position, face.d.position, type);
                }

                return default;
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
            static public void GetIndices(Side side, out int a, out int b)
            {
                if (side == Side.Bottom)
                {
                    a = 0;
                    b = 1;
                    return;
                }
                if (side == Side.Top)
                {
                    a = 2;
                    b = 3;
                    return;
                }
                if (side == Side.Left)
                {
                    a = 0;
                    b = 3;
                    return;
                }
                if (side == Side.Right)
                {
                    a = 1;
                    b = 2;
                    return;
                }

                a = 0;
                b = 0;
                return;
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
            public static bool Compare(Vector2 a, Vector2 b)
            {
                return (a - b).magnitude <= float.Epsilon;
            }
            public static Side GetSideOfEdge(Face face, Edge edge)
            {
                if (edge.Equals(FromFace(face, Side.Left), edge)) return Side.Left;
                if (edge.Equals(FromFace(face, Side.Right), edge)) return Side.Right;
                if (edge.Equals(FromFace(face, Side.Top), edge)) return Side.Top;
                if (edge.Equals(FromFace(face, Side.Bottom), edge)) return Side.Bottom;

                return default;
            }
            public static bool AreOpposite(Side a, Side b)
            {
                if (a == Side.Left && b == Side.Right) return true;
                if (a == Side.Right && b == Side.Left) return true;
                if (a == Side.Top && b == Side.Bottom) return true;
                if (a == Side.Bottom && b == Side.Top) return true;

                return false;
            }
            public static float GetLengthUVRatio(Face face, Side side)
            {
                GetIndices(side, out int a, out int b);
                float worldLength = (face[a].position - face[b].position).magnitude;
                float uvLength = (face[a].uv - face[b].uv).magnitude;

                return worldLength / uvLength;
            }
            public enum Side
            {
                Top,
                Bottom,
                Left,
                Right
            }
        }
        public class FaceSet
        {
            readonly public Plane plane;
            public List<Face> faces = new();
            public Dictionary<Edge, (int, int)> edgeMap = new(new Edge());
            public FaceSet(Plane plane)
            {
                this.plane = plane;
            }
            public void CreateEdges()
            {
                edgeMap.Clear();
                for (int i = 0; i < faces.Count; i++)
                {
                    foreach (Edge edge in Edge.GetEdges(faces[i]))
                    {
                        if (edgeMap.TryGetValue(edge, out (int, int) pair))
                        {
                            // set other pair index
                            edgeMap[edge] = (pair.Item1, i);
                        }
                        else
                        {
                            edgeMap[edge] = (i, -1);
                        }
                    }
                }
            }
            public void MergeFaces(int maxIterations)
            {
                bool Compare(float a, float b)
                {
                    return Mathf.Abs(a - b) <= float.Epsilon;
                }
                int iter = 0;
                while(true)
                {
                    if (iter >= maxIterations)
                    {
                        //Debug.LogError($"Bad merging");
                        break;
                    }

                    iter++;

                    HashSet<int> remainingFaces = new HashSet<int>();
                    for (int i = 0; i < faces.Count; i++)
                        remainingFaces.Add(i);

                    List<Face> newMergedFaces = new();

                    // see if every edge pair can be merged
                    foreach (Edge edge in edgeMap.Keys)
                    {
                        (int, int) pair = edgeMap[edge];

                        if (pair.Item1 == -1 || pair.Item2 == -1) continue;  // Cannot be merged

                        if (!remainingFaces.Contains(pair.Item2) || !remainingFaces.Contains(pair.Item1)) continue;  // faces already got merged this iteration

                        // Get faces
                        Face a = faces[pair.Item1];
                        Face b = faces[pair.Item2];

                        // Skip faces that have non-mergeable values
                        if (a.texture != b.texture) continue;
                        if (a.colorOverride != b.colorOverride) continue;
                        if (a.colorOverride && b.colorOverride)
                            if (a.color != b.color) continue;
                        if (!a.IsOrth() || !b.IsOrth()) continue;

                        // get the opposite edge of each face
                        Edge.Side aEdgeSide = Edge.GetSideOfEdge(a, edge);
                        Edge.Side bEdgeSide = Edge.GetSideOfEdge(b, edge);

                        if (!Edge.AreOpposite(aEdgeSide, bEdgeSide)) continue;

                        Edge.Side faceAOppositeEdge = Edge.GetOppositeSide(aEdgeSide);
                        Edge.Side faceBOppositeEdge = Edge.GetOppositeSide(bEdgeSide);

                        Edge.GetIndices(faceAOppositeEdge, out int faceA0, out int faceA4);
                        Edge.GetIndices(faceBOppositeEdge, out int faceB2, out int faceB3);

                        if (a[faceA0].color != b[faceB2].color) continue;

                        // compare UVs
                        // if the ratio between the edge length and UV tiling is not the same, they cannot be merged
                        Edge.Side faceAAdjacentSide = Edge.GetAdjacentSide(aEdgeSide);
                        Edge.Side faceBAdjacentSide = Edge.GetAdjacentSide(bEdgeSide);

                        float edgeALengthUVRatio = Edge.GetLengthUVRatio(a, faceAAdjacentSide);
                        float edgeBLengthUVRatio = Edge.GetLengthUVRatio(b, faceBAdjacentSide);
                        if (Mathf.Abs(edgeALengthUVRatio - edgeBLengthUVRatio) > float.Epsilon) continue;

                        Face newFace = a;
                        newFace[faceA0] = a[faceA0];
                        newFace[faceA4] = a[faceA4];
                        newFace[faceB2] = b[faceB2];
                        newFace[faceB3] = b[faceB3];

                        FaceVertex a0 = newFace[faceA0];
                        FaceVertex a4 = newFace[faceA4];
                        FaceVertex b2 = newFace[faceB2];
                        FaceVertex b3 = newFace[faceB3];

                        if (faceBOppositeEdge == Edge.Side.Right)
                        {
                            b2.uv.x *= 2;
                            b3.uv.x *= 2;

                            newFace[faceB2] = b2;
                            newFace[faceB3] = b3;
                        }
                        else if (faceBOppositeEdge == Edge.Side.Top)
                        {
                            a4.uv.y *= 2;
                            b3.uv.y *= 2;

                            newFace[faceA4] = a4;
                            newFace[faceB3] = b3;
                        }
                        
                        //else
                        //{
                        //    b2.uv.x *= 2;
                        //    b3.uv.x *= 2;
                        //}

                        

                        newMergedFaces.Add(newFace);
                        remainingFaces.Remove(pair.Item1);
                        remainingFaces.Remove(pair.Item2);
                    }

                    if (newMergedFaces.Count == 0)  // If no faces were merged, processing complete
                    {
                        //Debug.Log($"Processed using {iter} iterations");
                        break;
                    }
                    // create new face list to repeat on
                    // new faces will contain merged ones and any remaining indices in the hashset
                    foreach (int index in remainingFaces)
                    {
                        newMergedFaces.Add(faces[index]);
                    }

                    faces.Clear();
                    faces.AddRange(newMergedFaces);
                    CreateEdges();
                }
            }
        }
    }
}
