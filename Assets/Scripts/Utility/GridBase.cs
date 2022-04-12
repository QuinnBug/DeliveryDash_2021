using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GridBase : MonoBehaviour
{
    public bool m_ShowGrid = true;
    public Vector2Int size;
    public float perlinZoom;
    public float perlinHeight;
    public float perlinHeightLimit;

    private Vector3[] vertices;
    private Mesh mesh;

    public void GenerateMesh() 
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Grid";

        #region vertices generation
        vertices = new Vector3[(size.x + 1) * (size.y + 1)];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);
        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0, z = 0; z <= size.y; z++)
        {
            for (int x = 0; x <= size.x; x++, i++)
            {
                float y = (Mathf.PerlinNoise(x * perlinZoom, z * perlinZoom) - 0.5f) * perlinHeight;

                if (y > perlinHeightLimit) y = perlinHeightLimit;
                //else if (y < -perlinHeightLimit) y = -perlinHeightLimit;

                vertices[i] = new Vector3(x, y, z);
                uv[i] = new Vector2((float)x / size.x, (float)z / size.y);
                tangents[i] = tangent;
            }
        }
        #endregion

        mesh.vertices = vertices;

        #region tri generation
        int[] triangles = new int[size.x * size.y * 6];
        for (int ti = 0, vi = 0, y = 0; y < size.y; y++, vi++)
        {
            for (int x = 0; x < size.x; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + size.x + 1;
                triangles[ti + 5] = vi + size.x + 2;
            }
        }
        #endregion

        mesh.triangles = triangles;

        mesh.uv = uv;
        mesh.tangents = tangents;

        mesh.RecalculateNormals();

        mesh.RecalculateBounds();
        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    private void OnDrawGizmosSelected()
    {
        if (m_ShowGrid)
        {
            m_ShowGrid = false;
            GenerateMesh();
        }

        //if (vertices != null)
        //{
        //    Gizmos.color = Color.black;
        //    for (int i = 0; i < vertices.Length; i++)
        //    {
        //        Gizmos.DrawSphere(transform.TransformPoint(vertices[i]), 0.1f);
        //    }
        //}
    }
}
