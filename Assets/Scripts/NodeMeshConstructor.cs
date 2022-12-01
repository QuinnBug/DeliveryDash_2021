using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeMeshConstructor : MonoBehaviour
{
    LNode_Manager nodeManager;

    public float minVDistance;
    public float cornerDistance;
    private float abDistance;

    public List<Vector3> points = null;
    public List<Bounds> shapes = null;

    //display variables
    public bool drawPoints;
    public bool drawLines;
    public int currentP = 0;
    public int displayCount = 64;
    public float pTimer = 0;
    public float switchTime = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        nodeManager = LNode_Manager.Instance;
        points = null;

        abDistance = Mathf.Sqrt((cornerDistance * cornerDistance) / 2);
    }

    // Update is called once per frame
    void Update()
    {
        if (nodeManager.nodeGenDone && points == null)
        {
            GetAllPoints();
        }
    }

    void GetAllPoints() 
    {
        points = new List<Vector3>();
        shapes = new List<Bounds>();

        foreach (Node item in nodeManager.nodes)
        {
            if (item == null) continue;

            shapes.Add(GetNodeCorners(item));
        }

        for (int i = 0; i < shapes.Count; i++)
        {
            for (int j = 0; j < shapes.Count; j++)
            {
                
            }
        }

        Debug.Log("Mesh Points Done");
    }

    Bounds GetNodeCorners(Node node) 
    {
        //for each conn
        //get the direction from node to conn[i]
        //find midIPoint xDist in direction
        //get sidePoints to the left & right of midIPoint
        //add sidePoints to points if sidepoint is further away than mid corner dist from other points
        //sort points in clockwise order
        //return points;

        List<Vector3> points = new List<Vector3>();

        Quaternion rotation;
        Vector3 midDirPoint;
        foreach (Node conn in node.connections)
        {
            //rotation = Quaternion.FromToRotation(Vector3.forward, (node.point - conn.point).normalized);
            if ((conn.point - node.point).sqrMagnitude == 0) { Debug.Log(node.point + " " + conn.point); continue; }
            rotation = Quaternion.LookRotation(conn.point - node.point, Vector3.up);
            midDirPoint = node.point + (rotation * (Vector3.forward * abDistance));
            //does -1 and 1 for left & right
            for (int i = -1; i < 2; i += 2)
            {
                points.Add(midDirPoint + (rotation * (Vector3.right * abDistance * i)));
            }
            //points.Add(midDirPoint);
        }

        //remove duplicate points

        if (node.connections.Count == 1)
        {
            points.Add(node.point);
            //needs to add the points that would make the road form a square
        }

        return new Bounds(points.ToArray());
    }

    private void OnDrawGizmosSelected()
    {
        if (shapes != null && shapes.Count >= displayCount)
        {
            pTimer -= Time.deltaTime;

            if (pTimer <= 0)
            {
                pTimer = switchTime;
                currentP ++;
            }

            if (currentP >= shapes.Count - displayCount) currentP = 0;

            if (drawPoints)
            {
                for (int i = currentP; i < currentP + displayCount; i++)
                {
                    foreach (Vector3 dot in shapes[i].corners)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(dot + Vector3.up * 5, 3);
                    }
                }
            }
        }
        else
        {
            currentP = 0;
            pTimer = switchTime;
        }
    }

    public class Bounds 
    {
        public Vector3[] corners = new Vector3[4];
        
        public Bounds(Vector3[] points) 
        {
            corners = points;
        }

        public Vector3[] GetSegment(int startPoint) 
        {
            int endPoint = startPoint + 1;
            if (endPoint >= corners.Length) endPoint = 0;

            return new Vector3[] { corners[startPoint], corners[endPoint] };
        }

        public Vector3 GetOverlap(Vector3[] otherLine) 
        {
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3[] currentLine = GetSegment(i);
                if (DoesIntersect(otherLine, currentLine)) 
                {
                    //Line1
                    float A1 = otherLine[1].z - otherLine[0].z;
                    float B1 = otherLine[0].x - otherLine[1].x;
                    float C1 = A1 * otherLine[0].x + B1 * otherLine[0].z;

                    //Line2
                    float A2 = currentLine[1].z - currentLine[0].z;
                    float B2 = currentLine[0].x - currentLine[1].x;
                    float C2 = A1 * currentLine[0].x + B1 * currentLine[0].z;

                    float det = A1 * B2 - A2 * B1;
                    if (det != 0)
                    {
                        float x = (B2 * C1 - B1 * C2) / det;
                        float z = (A1 * C2 - A2 * C1) / det;
                        return new Vector3(x, 0, z);
                    }
                }
            }

            return Vector3.negativeInfinity;
        }
        //https://bryceboe.com/2006/10/23/line-segment-intersection-algorithm/
        
        public bool CounterClockwisePoints(Vector3 a, Vector3 b, Vector3 c) 
        {
            return (c.y - a.y) * (b.x - a.x) > (b.y - a.y) * (c.x - a.x);
        }

        public bool DoesIntersect(Vector3[] lineA, Vector3[] lineB) 
        {
            return CounterClockwisePoints(lineA[0], lineB[0], lineB[1]) != CounterClockwisePoints(lineA[1], lineB[0], lineB[1]) &&
                   CounterClockwisePoints(lineA[0], lineA[1], lineB[0]) != CounterClockwisePoints(lineA[0], lineA[1], lineB[1]);
        }
    }
}
