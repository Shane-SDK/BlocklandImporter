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

                //Quaternion rotation = Quaternion.Inverse(Quaternion.LookRotation(plane.normal));
                //for (int v = 0; v < 4; v++)
                //    face[v] = new FaceVertex { color = face[v].color, uv = face[v].uv, position = rotation * face[v].position };

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
                        //vertex.position = PlaneToWorld(vertex.position);
                        worldFace[v] = vertex;
                    }

                    output.Add(worldFace);
                }
            }
        }
        public static bool TryMergeFaces(in Face a, in Face b, in Edge sharedEdge, out Face mergedFace)
        {
            mergedFace = default;
            // Skip faces that have non-mergeable values
            if (a.texture != b.texture) return false;
            if (a.colorOverride != b.colorOverride) return false;
            if (a.colorOverride && b.colorOverride)
                if (a.color != b.color) return false;
            if (a.AveragedColor() != b.AveragedColor()) return false;
            if (!a.IsOrth() || !b.IsOrth()) return false;

            // Faces are both rectangular at this point

            // Get the side of each face that's the shared edge
            Side faceASharedSide = a.GetSideOfEdge(sharedEdge);
            Side faceBSharedSide = b.GetSideOfEdge(sharedEdge);

            if (!Edge.AreOpposite(faceASharedSide, faceBSharedSide)) return false;  // Necessary check for correctly merging UVs

            // Get the two indices of face B's edges that are making the new face
            int topBIndex = GetOtherFaceIndexFromSharedEdgeVertex(in b, sharedEdge.a, faceBSharedSide, out Side topBSide);
            int bottomBIndex = GetOtherFaceIndexFromSharedEdgeVertex(in b, sharedEdge.b, faceBSharedSide, out Side bottomBSide);

            if (topBIndex == -1 || bottomBIndex == -1)
                return false;

            // transform face b's indices to face a's
            Face.GetIndices(faceASharedSide, out int sharedATopIndex, out int sharedABottomIndex);

            mergedFace = a;
            bool orderCheck = a[sharedATopIndex].position == sharedEdge.a;
            int mergedTopIndex = orderCheck ? topBIndex : bottomBIndex;
            int mergedBottomIndex = orderCheck ? bottomBIndex : topBIndex;

            mergedFace.SetPosition(sharedATopIndex, b[mergedTopIndex].position);
            mergedFace.SetPosition(sharedABottomIndex, b[mergedBottomIndex].position);

            // Adjust uvs
            // Ensure the world length and uv lengths of both faces are the same ratios
            // only check one edge of each because they are square
            Edge aEdge = a.GetEdge(Edge.GetAdjacentSide(faceASharedSide));
            Edge bEdge = b.GetEdge(Edge.GetAdjacentSide(faceBSharedSide));

            Face.GetIndices(Edge.GetAdjacentSide(faceASharedSide), out int aUVFaceIndex0, out int aUVFaceIndex1);
            Face.GetIndices(Edge.GetAdjacentSide(faceBSharedSide), out int bUVFaceIndex0, out int bUVFaceIndex1);

            float bFaceUVLength = (b[bUVFaceIndex0].uv - b[bUVFaceIndex1].uv).magnitude;

            float aUVRatio = aEdge.Length / (a[aUVFaceIndex0].uv - a[aUVFaceIndex1].uv).magnitude;
            float bUVRatio = bEdge.Length / bFaceUVLength;
            if (Mathf.Abs(aUVRatio - bUVRatio) > 0.01f) return false;

            // adjust UV length of merged face
            Vector2 uvOffset = default;
            switch (faceASharedSide)
            {
                case Side.Left:
                    uvOffset = new Vector2(-bFaceUVLength, 0); break;
                case Side.Right:
                    uvOffset = new Vector2(bFaceUVLength, 0); break;
                case Side.Top:
                    uvOffset = new Vector2(0, bFaceUVLength); break;
                case Side.Bottom:
                    uvOffset = new Vector2(0, -bFaceUVLength); break;
            }

            mergedFace.SetUV(mergedTopIndex, a[sharedATopIndex].uv + uvOffset);
            mergedFace.SetUV(mergedBottomIndex, a[sharedABottomIndex].uv + uvOffset);

            return true;
        }
        public static int GetOtherFaceIndexFromSharedEdgeVertex(in Face face, Vector3 sharedVertex, Side mySide, out Side otherSide)
        {
            Edge.GetConnectingSides(mySide, out Side sideA, out Side sideB);

            Face.GetIndices(sideA, out int sideA_A, out int sideA_B);
            otherSide = sideA;
            if (face[sideA_A].position == sharedVertex)
                return sideA_B;
            if (face[sideA_B].position == sharedVertex)
                return sideA_A;

            otherSide = sideB;
            Face.GetIndices(sideB, out int sideB_A, out int sideB_B);
            if (face[sideB_A].position == sharedVertex)
                return sideB_B;
            if (face[sideB_B].position == sharedVertex)
                return sideB_A;

            return -1;
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
                    foreach (Side side in Face.GetSides())
                    {
                        Edge edge = face.GetEdge(side);
                        if (edgeMap.TryGetValue(edge, out (int, int) pair))
                        {
                            // set other pair index
                            edgeMap[edge] = (pair.Item1, i);
                            //Face otherFace = faces[pair.Item1];
                            //Edge otherFaceEdge = otherFace.GetEdge(side);
                            //// set first entry to be whichever face is either the bottom-most or left-most
                            //if (side == Side.Left || side == Side.Right)
                            //{
                            //    if (otherFaceEdge.a.x > edge.a.x)
                            //        edgeMap[edge] = (i, pair.Item1);
                            //}

                            //if (side == Side.Top || side == Side.Bottom)
                            //{
                            //    if (otherFaceEdge.a.y > edge.a.y)
                            //        edgeMap[edge] = (i, pair.Item1);
                            //}
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

                        // check if UVs can tile
                        if (TryMergeFaces(in a, in b, in edge, out Face mergedFace) == false)
                            continue;

                        newMergedFaces.Add(mergedFace);
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
    public enum Side
    {
        Top,
        Bottom,
        Left,
        Right
    }
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
