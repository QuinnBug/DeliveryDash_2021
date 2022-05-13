using QuinnMeshes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Junction : MonoBehaviour
{
    public float pointDistanceFromCenter = 1;
    [Space]
    public Vector3 position;
    public List<Road> connectedRoads;

    public void Init() 
    {
        if (connectedRoads.Count < 2) return;

        List<Vector3> points = new List<Vector3>();

        //for each road: get 2 points either side of the new road
        foreach (Road road in connectedRoads)
        {
            Vector3 _endPoint = position;
            Vector3 pointDir = road.transform.right;

            if (road.startJunction == this)
            {
                _endPoint = road.endPoint;
                pointDir *= -1;
            }
            else if (road.endJunction == this)
            {
                _endPoint = road.startPoint;
            }

            Vector3 roadCenter = Vector3.MoveTowards(position, _endPoint, pointDistanceFromCenter);

            points.Add(roadCenter + (pointDir * (road.width / 2)));
            points.Add(roadCenter + (pointDir * -(road.width / 2)));
        }

        //create triangles connecting each point togeter

        QMesh qMesh = new QMesh();
        for (int i = 0; i < points.Count; i++)
        {
            int j = i + 1;
            if (j == points.Count)
            {
                j = 0;
            }

            Triangle tri = new Triangle();

            Vertex[] _v = new Vertex[] {
                new Vertex(position, new Vector2(0.5f, 0)),
                new Vertex(points[i], new Vector2(0, 1)),
                new Vertex(points[j], new Vector2(1, 1))
            };

            tri.vertices = _v;

            qMesh.triangles.Add(tri);
        }

        Mesh mesh = qMesh.ConvertToMesh();

        MeshFilter filter = GetComponent<MeshFilter>();
        MeshCollider coll = GetComponent<MeshCollider>();
        filter.sharedMesh = coll.sharedMesh = mesh;
    }
}
