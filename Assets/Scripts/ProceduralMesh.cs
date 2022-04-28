using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuinnMeshes 
{
    public struct Triangle
    {
        public Vertex[] vertices;

        public Triangle(Vertex[] _v) 
        {
            vertices = _v;
        }
    }

    public struct Vertex 
    {
        public Vector3 position;
        public Vector2 uv;
    }

    public class qMesh 
    {
        List<Triangle> triangles;

        public Mesh ConvertToMesh() 
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            foreach (Triangle item in triangles)
            {
                foreach (Vertex vertex in item.vertices)
                {
                    if (!vertices.Contains(vertex.position))
                    {
                        vertices.Add(vertex.position);
                        uvs.Add(vertex.uv);
                    }
                    tris.Add(vertices.IndexOf(vertex.position));
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
