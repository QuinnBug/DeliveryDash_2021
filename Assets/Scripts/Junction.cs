using QuinnMeshes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Junction : MonoBehaviour
{
    public float pointDistanceFromCenter = 1;
    [Space]
    public Vector3 centralPoint;
    public List<Road> connectedRoads;
    [Space]
    public Vector3[] debugPoints;
    public List<Vector3> centerPoints = new List<Vector3>();

    public void Init() 
    {
        transform.position = centralPoint;

        if (connectedRoads.Count < 2) return;

        //SortConnectedRoads();

        List<Vector3> points = new List<Vector3>();

        //for each road: get 2 points either side of the new road
        foreach (Road road in connectedRoads)
        {
            bool startConnect = ConnectedToRoadStart(road);
            //Vector3 _endPoint = startConnect ? road.endPoint : road.startPoint;

            //get the left most point of the mesh and the right most point of the road mesh.
            int depth = startConnect ? 0 : road.vertexCount;

            points.Add(road.transform.TransformPoint(road.points[0][depth].position));
            points.Add(road.transform.TransformPoint(road.points[road.vertexWidth][depth].position));

            Debug.Log(points[points.Count - 2] + " && " + points[points.Count - 1] + " :: " + depth);
            Debug.DrawLine(points[points.Count - 2], points[points.Count - 1], Color.cyan, 60);

            //Vector3 roadCenter = transform.InverseTransformPoint(Vector3.MoveTowards(position, _endPoint, pointDistanceFromCenter));
            //Vector3 pointDir = Quaternion.Euler(0, 90, 0) * roadCenter.normalized;

            //points.Add(roadCenter + (pointDir * (road.width / 2)));
            //points.Add(roadCenter - (pointDir * (road.width / 2)));
        }

        points = SortPointsClockwise(points);

        for (int i = 0; i < points.Count; i++)
        {
            int j = i + 1;
            if (j >= points.Count) j = 0;

            Debug.DrawLine(points[i], points[j], Color.blue, 60);
            points[i] = transform.InverseTransformPoint(points[i]);
            Debug.DrawLine(points[i], points[j], Color.red, 60);
        }

        //add 0 to get the center point
        points.Add(new Vector3(0, points[0].y, 0));

        #region floor raycase
        //do the ol' raycast down to get floor height
        //LayerMask groundLayer = 1 << LayerMask.NameToLayer("Ground");
        //for (int i = 0; i < points.Count; i++)
        //{
        //    Vector3 adjPoint = points[i];

        //    RaycastHit hit;
        //    if (Physics.Raycast(transform.TransformPoint(adjPoint), Vector3.down, out hit, 100, groundLayer))
        //    {
        //        if (hit.collider.gameObject.tag == "Terrain")
        //        {
        //            adjPoint.y = transform.InverseTransformPoint(hit.point).y + 0.2f;
        //        }
        //    }

        //    points[i] = adjPoint;
        //}
        #endregion

        //remove the 0 from the array and set position to that
        centralPoint = points.Last();
        points.RemoveAt(points.Count - 1);

        debugPoints = points.ToArray();

        QMesh qMesh = new QMesh();
        //for (int i = 0; i < points.Count; i++)
        for (int i = 0; i < points.Count; i += 2)
        {
            int j = i + 1;
            if (j == points.Count) j = 0;
            int k = j + 1;
            if (k == points.Count) k = 0;
            

            Triangle tri = new Triangle();

            //Vertex[] _v = new Vertex[] {
            //    new Vertex(points[i], new Vector2(0, 1)),
            //    new Vertex(centralPoint, new Vector2(0.5f, 0)),
            //    new Vertex(points[j], new Vector2(1, 1)),
            //};

            Vertex[] _v = new Vertex[] {
                new Vertex(points[i], new Vector2(1, 1)),
                new Vertex(points[k], new Vector2(1, 1)),
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

    public bool ConnectedToRoadStart(Road road) 
    {
        if (road.startJunction == this)
        {
            return true;
        }
        else if (road.endJunction == this)
        {
            return false;
        }
        else
        {
            Debug.LogError("Road is not connected to this junction");
            return false;
        }
    }

    private List<Vector3> SortPointsClockwise(List<Vector3> points)
    {
        Vector3 open, check;
        int j;
        bool loop;

        for (int i = 1; i < points.Count; i++)
        {
            loop = true;
            j = i - 1;

            open = points[i];
            float openAngle = Vector3.SignedAngle(Vector3.forward, open, Vector3.up);
            if (openAngle < 0) openAngle += 360;

            while (j >= 0 && loop)
            {
                check = points[j];
                float checkAngle = Vector3.SignedAngle(Vector3.forward, check, Vector3.up);
                if (checkAngle < 0) checkAngle += 360;

                loop = checkAngle < openAngle;
                if (loop)
                {
                    points[j + 1] = points[j];
                    j--;
                }
            }
            points[j + 1] = open;
        }

        return points;
    }

    private void OnDrawGizmosSelected()
    {
        if (debugPoints != null)
        {
            foreach (var item in debugPoints)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(transform.TransformPoint(item), 1.5f);
            }
        }

        if (centerPoints.Count > 0)
        {
            foreach (var item in centerPoints)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.TransformPoint(item), 0.5f);
            }
        }
    }
}
