using Blockland.Objects;
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
                        return new Edge(face[1].position, face[2].position, type);
                    case Side.Left:
                        return new Edge(face[2].position, face[3].position, type);
                    case Side.Right:
                        return new Edge(face[1].position, face[0].position, type);
                    case Side.Top:
                        return new Edge(face[3].position, face[0].position, type);
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
                    Face face = faces[i];
                    foreach (Edge edge in Edge.GetEdges(face))
                    {
                        if (edgeMap.TryGetValue(edge, out (int, int) pair))
                        {
                            // set other pair index
                            edgeMap[edge] = (pair.Item1, i);
                            Face otherFace = faces[pair.Item1];
                            // set first entry to be whichever face is either the bottom-most or left-most
                            if (edge.side == Edge.Side.Left || edge.side == Edge.Side.Right)
                            {
                                if (Edge.FromFace(otherFace, edge.side).a.x > edge.a.x)
                                    edgeMap[edge] = (i, pair.Item1);
                            }
                            
                            if (edge.side == Edge.Side.Top || edge.side == Edge.Side.Bottom)
                            {
                                if (Edge.FromFace(otherFace, edge.side).a.y > edge.a.y)
                                    edgeMap[edge] = (i, pair.Item1);
                            }
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

                        Edge.GetIndices(faceAOppositeEdge, out int faceA0, out int faceA1);
                        Edge.GetIndices(faceBOppositeEdge, out int faceB0, out int faceB1);

                        if (a[faceA0].color != b[faceB0].color) continue;

                        // compare UVs
                        // if the ratio between the edge length and UV tiling is not the same, they cannot be merged
                        Edge.Side faceAAdjacentSide = Edge.GetAdjacentSide(aEdgeSide);
                        Edge.Side faceBAdjacentSide = Edge.GetAdjacentSide(bEdgeSide);

                        Edge.GetIndices(faceAAdjacentSide, out int adjacentAIndex0, out int adjacentAIndex1);
                        Edge.GetIndices(faceBAdjacentSide, out int adjacentBIndex0, out int adjacentBIndex1);
                        float edgeALength = (a[adjacentAIndex0].position - a[adjacentAIndex1].position).magnitude;
                        float edgeBLength = (b[adjacentBIndex0].position - b[adjacentBIndex1].position).magnitude;
                        float edgeAUVLength = (a[adjacentAIndex0].uv - a[adjacentAIndex1].uv).magnitude;
                        float edgeBUVLength = (b[adjacentBIndex0].uv - b[adjacentBIndex1].uv).magnitude;
                        float edgeAUVRatio = (edgeALength / edgeAUVLength);
                        float edgeBUVRatio = (edgeBLength / edgeBUVLength);

                        if (Mathf.Abs(edgeAUVRatio - edgeBUVRatio) > 0.001f) continue;

                        Face newFace = a;
                        newFace[faceA0] = a[faceA0];
                        newFace[faceA1] = a[faceA1];
                        newFace[faceB0] = b[faceB0];
                        newFace[faceB1] = b[faceB1];

                        FaceVertex a0 = newFace[faceA0];
                        FaceVertex a4 = newFace[faceA1];
                        FaceVertex b2 = newFace[faceB0];
                        FaceVertex b3 = newFace[faceB1];

                        if (edge.side == Edge.Side.Right || edge.side == Edge.Side.Left)
                        {
                            b2.uv.x = edgeAUVLength + edgeBUVLength;
                            b3.uv.x = edgeAUVLength + edgeBUVLength;

                            newFace[faceB0] = b2;
                            newFace[faceB1] = b3;
                        }
                        else if (edge.side == Edge.Side.Top || edge.side == Edge.Side.Bottom)
                        {
                            a4.uv.y = edgeAUVLength + edgeBUVLength;
                            b3.uv.y = edgeAUVLength + edgeBUVLength;

                            newFace[faceA1] = a4;
                            newFace[faceB1] = b3;
                        }

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
