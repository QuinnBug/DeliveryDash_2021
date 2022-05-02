using QuinnMeshes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum RoadConnection 
{
    NULL = -1,
    START = 0,
    END = 1
}

[Serializable]
public class Road : MonoBehaviour
{
    public Vector3 startPoint, endPoint;
    public float width = 1;
    public int vertexCount;
    public int vertexWidth = 2;
    internal float length = 0;
    private Mesh mesh;
    private qMesh qMesh;
    [Space]
    internal Vector3[] vertices;
    internal Vertex[][] points;

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
        LayerMask groundLayer = 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Road");
        transform.position = startPoint;
        transform.LookAt(endPoint, Vector3.up);

        Vector3 oStart = startPoint;
        //startPoint = Vector3.MoveTowards(startPoint, endPoint, -width/4);
        Vector3 oEnd = endPoint;
        //endPoint = Vector3.MoveTowards(endPoint, startPoint, -width/4);
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

        qMesh = new qMesh();

        #region set up vertices
        points = new Vertex[vertexWidth+1][];
        for (float x = 0; x <= vertexWidth; x++)
        {
            points[(int)x] = new Vertex[vertexCount+1];
            for (float z = 0; z <= vertexCount; z++) 
            {
                Vector3 flatVertexPoint = new Vector3((x - vertexWidth/2.0f) * (width / vertexWidth), 0, z * (length / vertexCount));
                Vector3 vertexPoint = flatVertexPoint;
                Vector3 adjCenter = new Vector3(vertexPoint.x, 0, transform.InverseTransformPoint(Vector3.Lerp(startPoint, endPoint, 0.5f)).z);
                //Debug.Log("adjCenter " + adjCenter);

                bool loop = true;
                while (loop)
                {
                    flatVertexPoint = vertexPoint;
                    flatVertexPoint.y = 0;

                    loop = false;

                    bool roadDone = false;
                    float distanceMoved = 0;

                    RaycastHit[] hits = Physics.RaycastAll(transform.TransformPoint(flatVertexPoint), Vector3.down, 100, groundLayer);
                    foreach (RaycastHit hit in hits)
                    {
                        if (hit.collider.gameObject.tag == "Terrain")
                        {
                            vertexPoint.y = transform.InverseTransformPoint(hit.point).y + 0.2f;
                        }
                        else if (hit.collider.gameObject.tag == "Road" && !roadDone)
                        {
                            //Debug.DrawLine(transform.TransformPoint(vertexPoint), transform.TransformPoint(Vector3.Lerp(vertexPoint, adjCenter, 0.001f)), Color.red, 60);
                            //distanceMoved += Vector3.Distance(vertexPoint, adjCenter) * 0.001f;
                            //vertexPoint = Vector3.Lerp(vertexPoint, adjCenter, 0.001f);
                            //loop = distanceMoved < length/2;
                            roadDone = true;
                        }
                    }
                }


                points[(int)x][(int)z] = new Vertex(vertexPoint, new Vector2(((x - vertexWidth / 2.0f) + (vertexWidth / 2.0f)) / vertexWidth, z / vertexCount));
            }
        }
        #endregion

        #region set up tris
        LayerMask roadLayer = 1 << LayerMask.NameToLayer("Road");
        for (int x = 0; x < vertexWidth; x++)
        {
            for (int z = 0; z < vertexCount; z++)
            {
                Triangle tri = new Triangle();

                // bottom left, top left, top right
                tri.vertices = new Vertex[3] { points[x][z], points[x][z + 1], points[x + 1][z + 1] };
                if (!tri.CheckCollision(roadLayer, transform)) 
                {
                    qMesh.triangles.Add(tri);
                }

                // bottom left, top right, bottom left
                Triangle tri1 = new Triangle();
                tri1.vertices = new Vertex[3] { points[x + 1][z + 1], points[x + 1][z], points[x][z] };
                if (!tri1.CheckCollision(roadLayer, transform))
                {
                    qMesh.triangles.Add(tri1);
                }
            }
        }
        #endregion

        mesh = qMesh.ConvertToMesh();
        vertices = mesh.vertices;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;

        startPoint = oStart;
        endPoint = oEnd;

        return true;
    }

    internal RoadConnection GetConnectionToRoad(Road otherRoad) 
    {
        if (endConnectedRoads.Contains(otherRoad))
        {
            return RoadConnection.END;
        }
        else if (startConnectedRoads.Contains(otherRoad))
        {
            return RoadConnection.START;
        }
        else
        {
            Debug.LogError("Road not connected to other road");
            return RoadConnection.NULL;
        }

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
