using Blockland.Objects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blockland.Meshing
{
    public class FaceOptimizer
    {
        IList<Face> faces;

        public FaceOptimizer(IList<Face> faces)
        {
            this.faces = faces;
        }
        public void OptimizeFaces()
        {
            // split faces by plane
            // convert to 2D faces
            Dictionary<Plane, List<Face>> flatFaces = new Dictionary<Plane, List<Face>>();

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

                Debug.Log(Vector3.Cross(face[0].position - face[2].position, face[0].position - face[1].position).normalized);

                faceIndices.Add(face);
            }
        }
    }
}
