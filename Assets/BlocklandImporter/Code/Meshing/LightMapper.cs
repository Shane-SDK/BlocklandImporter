using RectpackSharp;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Blockland.Meshing
{
    public class LightMapper
    {
        public static void GenerateUVs(IList<Face> faces, out Vector2[] uvs)
        {
            // separate faces by plane
            // transform face set to 2D
            // further separate faces by edge continuity
            // create bounding box around each set
            // put boxes into packing algo
            // transform set to packed bounding box
            Dictionary<Edge, (int, int)> CreateEdgeMap(IList<int> indices)
            {
                Dictionary<Edge, (int, int)> edgeMap = new(new Edge());
                for (int i = 0; i < indices.Count; i++)
                {
                    int faceIndex = indices[i];
                    Face face = faces[faceIndex];
                    foreach (Side side in Face.GetSides())
                    {
                        Edge edge = face.GetEdge(side);
                        if (edgeMap.TryGetValue(edge, out (int, int) pair))
                        {
                            // set other pair index
                            edgeMap[edge] = (pair.Item1, faceIndex);
                        }
                        else
                        {
                            edgeMap[edge] = (faceIndex, -1);
                        }
                    }
                }

                return edgeMap;
            }

            Dictionary<Plane, List<int>> planeSet = new();
            for (int i = 0 ; i < faces.Count; i++)
            {
                Face face = faces[i];
                Plane plane = face.Plane;

                if (!planeSet.TryGetValue(plane, out List<int> faceIndices))
                {
                    faceIndices = new();
                    planeSet[plane] = faceIndices;
                }

                faceIndices.Add(i);
            }

            List<List<int>> disjointIndexSets = new();

            int iterations = 0;

            foreach (List<int> indices in planeSet.Values)
            {
                Dictionary<Edge, (int, int)> edgeMap = CreateEdgeMap(indices);
                HashSet<int> exploredIndices = new();

                while (exploredIndices.Count != faces.Count)
                {
                    iterations++;

                    if (iterations > 1000)
                        break;

                    // get the next face index to start 'exploring' from for a disjoint set
                    int startIndex = -1;
                    for (int i = 0; i <  faces.Count; i++)
                    {
                        if (!exploredIndices.Contains(i))
                        {
                            startIndex = i;
                            break;
                        }
                    }

                    if (startIndex == -1) break;  // This shouldn't happen

                    Queue<int> indexQueue = new();
                    indexQueue.Enqueue(startIndex);
                    List<int> disjointIndexSet = new();  // indices going in the final set of UV faces

                    while (indexQueue.Count > 0)
                    {
                        iterations++;

                        if (iterations > 1000)
                            break;

                        int index = indexQueue.Dequeue();
                        if (exploredIndices.Contains(index)) continue;
                        exploredIndices.Add(index);
                        disjointIndexSet.Add(index);

                        // check if each edge has a shared face
                        // if not already added in the set, enqueue it and add to set
                        Face face = faces[index];
                        foreach (Edge edge in face.GetEdges())
                        {
                            if (edgeMap.TryGetValue(edge, out (int, int) pair))
                            {
                                if (pair.Item1 == -1 || pair.Item2 == -1) continue;

                                int otherFaceIndex = pair.Item1 == index ? pair.Item2 : pair.Item1;

                                if (exploredIndices.Contains(otherFaceIndex)) continue;

                                indexQueue.Enqueue(otherFaceIndex);
                            }
                        }
                    }

                    if (disjointIndexSet.Count > 0)
                    {
                        disjointIndexSets.Add(disjointIndexSet);
                    }
                }
            }

            //Debug.Log($"{faces.Count} faces => {disjointIndexSets.Count}");

            uvs = new Vector2[faces.Count * 4];
            System.Array.Fill(uvs, Vector2.zero);
            PackingRectangle[] rectangles = new PackingRectangle[disjointIndexSets.Count];
            (Vector2, Vector2)[] bounds = new (Vector2, Vector2)[disjointIndexSets.Count];

            for (int setIndex = 0; setIndex < disjointIndexSets.Count; setIndex++)
            {
                List<int> indices = disjointIndexSets[setIndex];
                Quaternion rotation = Quaternion.Inverse(Quaternion.LookRotation(faces[indices[0]].Plane.normal));
                Vector2 mins = Vector2.one * float.MaxValue;
                Vector2 maxs = Vector2.one * float.MinValue;

                float scalingFactor = 100;

                foreach (int index in indices)
                {
                    Face face = faces[index];
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 transformed = rotation * face[i].position;
                        mins = Vector2.Min(mins, transformed);
                        maxs = Vector2.Max(maxs, transformed);
                        uvs[index * 4 + i] = transformed;
                    }
                }

                bounds[setIndex] = (mins, maxs);
                Vector2Int size = Vector2Int.CeilToInt((maxs - mins) * scalingFactor);
                for (int i = 0; i < 2; i++)
                    size[i] = Mathf.Max(size[i], 1);
                rectangles[setIndex] = new PackingRectangle(0, 0, (uint)size.x, (uint)size.y, setIndex);
            }

            RectanglePacker.Pack(rectangles, out PackingRectangle packedBounds);

            foreach (PackingRectangle rect in rectangles)
            {
                List<int> indices = disjointIndexSets[rect.Id];

                foreach (int index in indices)
                {
                    for (int i = 0; i < 4; i++)
                    {                
                        // map uv positions from original bounds to bounds in packed rectangle
                        Vector2 uv = uvs[index * 4 + i];

                        Vector2 normRectangleMin = new Vector2(rect.X / (float)packedBounds.Width, rect.Y / (float)packedBounds.Height);
                        Vector2 normRectangleMax = new Vector2((rect.X + rect.Width) / (float)packedBounds.Width, (rect.Y + rect.Height) / (float)packedBounds.Height);

                        for (int c = 0; c < 2; c++)
                        {
                            uv[c] = MapRange(uv[c], bounds[index].Item1[c], bounds[index].Item2[c], normRectangleMin[c], normRectangleMax[c]);
                        }

                        uvs[index * 4 + i] = uv;
                    }
                }
            }
        }
        public static float MapRange(float x, float inMin, float inMax, float outMin, float outMax)
        {
            float inSize = inMax - inMin;
            float norm = (x - inMin) / inSize;

            return outMin + norm * (outMax - outMin);
        }
    }
}
