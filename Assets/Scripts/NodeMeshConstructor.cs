using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeMeshConstructor : MonoBehaviour
{
    LNode_Manager nodeManager;

    public float cornerDistance;
    [SerializeField]
    private float abDistance;

    public List<Vector3> points = null;
    public List<Bounds> squares = null;

    //display variables
    public bool drawPoints;
    public bool drawLines;
    public int currentP = 0;
    public int pCount = 64;
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

        foreach (Node item in nodeManager.nodes)
        {
            squares.Add(GetNodeCorners(item));
        }

        for (int i = 0; i < squares.Count; i++)
        {
            for (int j = 0; j < squares.Count; j++)
            {
                
            }
        }

        Debug.Log("Mesh Points Done");
    }

    Bounds GetNodeCorners(Node node) 
    {
        Vector3[] points = new Vector3[4];

        Quaternion rotation = Quaternion.LookRotation(node.forward, Vector3.up);

        int i = 0;
        for (int x = -1; x <= 1; x+=2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                points[i] = node.point + (rotation * (new Vector3(x, 0, z) * abDistance));
                i++;
            }
        }

        return new Bounds(points);
    }

    private void OnDrawGizmosSelected()
    {
        if (points != null && points.Count >= pCount)
        {
            pTimer -= Time.deltaTime;

            if (pTimer <= 0)
            {
                pTimer = switchTime;
                currentP += 4; //4 corners means we jump forward 1 node;
            }

            if (currentP >= points.Count - pCount) currentP = 0;

            if (drawPoints)
            {
                for (int i = currentP; i < currentP + pCount; i++)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(points[i], 3);
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
