using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum RoadConnection 
{
    START,
    END
}

public class Road : MonoBehaviour
{
    public Vector3 startPoint, endPoint;
    public float width = 1;
    public int vertexCount;
    public int vertexWidth = 2;
    internal float length = 0;
    private Mesh mesh;
    [Space]
    public Vector3[] vertices;
    public List<Road> startConnectedRoads = new List<Road>();
    public List<Road> endConnectedRoads = new List<Road>();

    public bool Init(Vector3 _start, Vector3 _end)
    {
        startPoint = _start;
        endPoint = _end;
        return GenerateMesh();
    }

    public bool GenerateMesh() 
    {
        LayerMask groundLayer = 1 << LayerMask.NameToLayer("Ground");
        startPoint = Vector3.MoveTowards(startPoint, endPoint, -width/4);
        endPoint = Vector3.MoveTowards(endPoint, startPoint, -width/4);
        Vector3 center = Vector3.Lerp(startPoint, endPoint, 0.5f);

        length = Vector3.Distance(endPoint, startPoint);
        Collider[] colliders = Physics.OverlapBox(center, new Vector3(1, 25, 1));
        if (colliders.Length > 0)
        {
            Road otherRoad;
            foreach (Collider col in colliders)
            {
                if (col.gameObject != this && col.TryGetComponent(out otherRoad))
                {
                    Road_Manager.Instance.DestroyRoad(this);
                    return false;
                }
            }
        }

        mesh = new Mesh();
        transform.position = startPoint;
        transform.LookAt(endPoint, Vector3.up);


        #region vertices region
        vertices = new Vector3[(vertexWidth + 1) * (vertexCount + 1)];
        Vector4[] tangents = new Vector4[vertices.Length];
        Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);
        Vector2[] uv = new Vector2[vertices.Length];
        float previousSegmentsY = transform.position.y;
        float lastY = transform.position.y;
        for (int i = 0, z = 0; z <= vertexCount; z++)
        {
            Vector3 flatVertexPos = Vector3.zero;
            Vector3 vertexPos = Vector3.zero;
            for (float x = -vertexWidth/2.0f; x <= vertexWidth/2.0f; x++, i++)
            {
                flatVertexPos = new Vector3(x * (width / vertexWidth), 0, z * (length / vertexCount));
                vertexPos = flatVertexPos;
                vertexPos.y = lastY;
                RaycastHit[] hits = Physics.RaycastAll(transform.TransformPoint(flatVertexPos), Vector3.down, 100, groundLayer);
                //Debug.DrawRay(transform.TransformPoint(flatVertexPos), Vector3.down * 30, Color.red, 10);
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.gameObject.tag == "Terrain")
                    {
                        vertexPos.y = transform.InverseTransformPoint(hit.point).y + 0.2f;
                        lastY = vertexPos.y; 
                        //vertexPos.y = hit.point.y;
                        break;
                    }
                }
                vertices[i] = vertexPos;
                uv[i] = new Vector2((float)x / (width/vertexWidth), (float)z / (length / vertexCount));
                tangents[i] = tangent;
            }
        }
        #endregion

        mesh.vertices = vertices;

        #region tri generation
        int[] triangles = new int[vertexWidth * vertexCount * 6];
        for (int ti = 0, vi = 0, y = 0; y < vertexCount; y++, vi++)
        {
            for (int x = 0; x < vertexWidth; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + vertexWidth + 1;
                triangles[ti + 5] = vi + vertexWidth + 2;
            }
        }
        #endregion

        mesh.triangles = triangles;

        mesh.uv = uv;
        mesh.tangents = tangents;

        mesh.RecalculateNormals();

        mesh.RecalculateBounds();

        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        return true;
    }

    internal Road GetRandomConnected(RoadConnection roadConnection)
    {
        switch (roadConnection)
        {
            case RoadConnection.START:
                return startConnectedRoads[Random.Range(0, startConnectedRoads.Count)];

            case RoadConnection.END:
                return endConnectedRoads[Random.Range(0, endConnectedRoads.Count)];
        }
        return null;
    }

    public void SetupConnections() 
    {
        LayerMask mask = 1 << LayerMask.NameToLayer("Road");
        Collider[] colliders = Physics.OverlapBox(startPoint, new Vector3(1, 50, 1), transform.rotation, mask);

        foreach (Collider col in colliders)
        {
            if (col.gameObject != gameObject)
            {
                startConnectedRoads.Add(col.GetComponent<Road>());
            }
        }

        colliders = Physics.OverlapBox(endPoint, new Vector3(1,50,1), transform.rotation, mask);

        foreach (Collider col in colliders)
        {
            if (col.gameObject != gameObject)
            {
                endConnectedRoads.Add(col.GetComponent<Road>());
            }
        }
    }

    internal Vector3 GetMeshCenter()
    {
        Vector3 center = Vector3.Lerp(startPoint, endPoint, 0.5f);
        Debug.DrawLine(center + Vector3.up, center - Vector3.up);
        return center;
    }
}
