using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuinnMeshes 
{
    public class Triangle
    {
        public Vertex[] vertices;

        internal bool CheckCollision(LayerMask layer, Transform _tf)
        {
            foreach (Vertex v in vertices)
            {
                foreach (Vertex o in vertices)
                {
                    if (v.position != o.position)
                    {
                        Vector3 posOne = _tf.TransformPoint(v.position);
                        Vector3 posTwo = _tf.TransformPoint(o.position);
                        RaycastHit hit;
                        if (Physics.Raycast(posOne, posOne - posTwo, out hit, Vector3.Distance(posOne, posTwo), layer))
                        {
                            //Debug.DrawLine(posOne, posTwo, Color.red, 60);
                            //return true;
                        }
                        else
                        {
                            //Debug.DrawLine(posOne, posTwo, Color.blue, 60);
                        }
                    }
                }
            }

            return false;
        }
    }

    public struct Vertex 
    {
        public Vector3 position;
        public Vector2 uv;

        public Vertex(Vector3 _pos, Vector2 _uv) 
        {
            position = _pos;
            uv = _uv;
        }
    }

    public class qMesh 
    {
        public List<Triangle> triangles = new List<Triangle>();

        public Mesh ConvertToMesh() 
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            foreach (Triangle item in triangles)
            {
                foreach (Vertex vertex in item.vertices)
                {
                    //if (!vertices.Contains(vertex.position))
                    //{
                    //    vertices.Add(vertex.position);
                    //    uvs.Add(vertex.uv);
                    //}
                    //tris.Add(vertices.IndexOf(vertex.position));

                    vertices.Add(vertex.position);
                    uvs.Add(vertex.uv);
                    tris.Add(vertices.Count - 1);
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = tris.ToArray();
            return mesh;
        }
    }
}
