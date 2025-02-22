using Blockland.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Blockland.Meshing
{
    public class FaceOptimizer
    {
        IList<Face> faces;
        Dictionary<Edge, (int, int)> edgeMap = new();

        public FaceOptimizer(IList<Face> faces)
        {
            this.faces = faces;
        }
        public void OptimizeFaces(ref ICollection<Face> newFaces)
        {
            // split faces by plane
            // convert to 2D faces
            Dictionary<Plane, List<Face>> flatFaces = new Dictionary<Plane, List<Face>>();
            Dictionary<Edge, (int, int)> edgeMap = new();

            // Organize faces by plane, convert faces from world space to plane space
            for (int i = 0; i < faces.Count; i++)
            {
                Face face = faces[i];
                Plane plane = face.Plane;
                Quaternion rotation = Quaternion.Inverse(Quaternion.LookRotation(plane.normal));

                for (int v = 0; v < 4; v++)
                    face[v] = new FaceVertex { color = face[v].color, uv = face[v].uv, position = rotation * face[v].position };

                // flatten positions using plane
                if (!flatFaces.TryGetValue(plane, out List<Face> faceIndices))
                {
                    faceIndices = new List<Face>();
                    flatFaces[plane] = faceIndices;
                }

                faceIndices.Add(face);
            }

            foreach (Plane plane in flatFaces.Keys)
            {
                // initialize edges
                List<Face> faces = flatFaces[plane];
                for (int i = 0; i < faces.Count; i++)
                {
                    newFaces.Add(faces[i]);
                    foreach (Edge edge in Edge.GetEdges(faces[i]))
                    {
                        if (edgeMap.TryGetValue(edge, out (int, int) pair))
                        {
                            // set other pair index
                            edgeMap[edge] = (pair.Item1, i);
                        }
                        else
                        {
                            edgeMap[edge] = (pair.Item1, -1);
                        }
                    }
                }
            }
        }

        public struct Edge : IEquatable<Edge>
        {
            public Vector2 a;
            public Vector2 b;
            public Edge(Vector2 a, Vector2 b)
            {
                this.a = a;
                this.b = b;
            }
            public bool Equals(Edge other)
            {
                return other.GetHashCode().Equals(this.GetHashCode());
            }
            public override int GetHashCode()
            {
                return a.GetHashCode() * b.GetHashCode();
            }

            static public IEnumerable<Edge> GetEdges(Face face)
            {
                yield return new Edge(face.a.position, face.b.position);
                yield return new Edge(face.a.position, face.d.position);
                yield return new Edge(face.c.position, face.b.position);
                yield return new Edge(face.c.position, face.d.position);
            }
        }
    }
}
