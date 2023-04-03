using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Earclipping;
using UnityEditor;

public class NodeMeshConstructor : MonoBehaviour
{
    LNode_Manager nodeManager;

    public float minVDistance;
    public float cornerDistance;
    public float nodeRadius;

    [Space]
    public HashSet<Node> visitedNodes;
    public List<Node> finalPath;
    [Space]
    internal List<Vector3> shapePoints = null;
    internal List<Line> shapeLines = null;
    public List<Polygon> polygons = null;
    [Space]
    //display variables
    public bool drawPoints;
    public bool drawLines;
    public bool drawPolygons;
    [Space]
    public float timePerNode;
    public bool meshCreated;

    // Start is called before the first frame update
    void Start()
    {
        nodeManager = LNode_Manager.Instance;

        shapePoints = null;
        visitedNodes = null;
        finalPath = null;

        meshCreated = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (nodeManager.nodeGenDone && visitedNodes == null && !meshCreated)
        {
            StartCoroutine(CreatePolygonFromNodes());
        }
    }

    IEnumerator CreatePolygonFromNodes() 
    {
        shapeLines = new List<Line>();
        polygons = new List<Polygon>();
        visitedNodes = new HashSet<Node>();

        ConnectionSort cs = new ConnectionSort();

        foreach (Node node in nodeManager.nodes)
        {
            cs.current = node;
            cs.start = node.connections[0];

            node.connections.Sort(cs);
            List<Line> nodeLines = new List<Line>();
            foreach (Node conn in node.connections)
            {
                //find midpoint from node to conn
                Vector3 farPoint = Vector3.Lerp(node.point, conn.point, 0.5f);
                Quaternion rotation = Quaternion.LookRotation(conn.point - node.point, Vector3.up);
                Vector3[] points = new Vector3[4];

                //close points
                points[0] = node.point + (rotation * (-Vector3.right * cornerDistance));
                points[3] = node.point + (rotation * (Vector3.right * cornerDistance));

                //middle points
                points[1] = farPoint + (rotation * (-Vector3.right * cornerDistance));
                points[2] = farPoint + (rotation * (Vector3.right * cornerDistance));

                Line[] lines = new Line[3];

                lines[0] = new Line(points[0], points[1]);
                lines[1] = new Line(points[1], points[2]);
                lines[2] = new Line(points[2], points[3]);

                if (lines[0].CircleIntersections(node.point, nodeRadius, out Vector3[] iOne))
                {
                    Vector3 point = lines[0].a;
                    if (iOne.Length == 2)
                    {
                        if (Vector3.Distance(iOne[0], lines[0].b) < Vector3.Distance(iOne[1], lines[0].b))
                        {
                            point = iOne[0];
                        }
                        else
                        {
                            point = iOne[1];
                        }
                    }
                    else point = iOne[0];

                    //Debug.DrawLine(lines[0].a, point, Color.red, 60);
                    lines[0].a = point;
                }
                else
                {
                    Debug.Log("How come line 0 doesn't intersect the node?");
                }

                if (lines[2].CircleIntersections(node.point, nodeRadius, out Vector3[] iTwo))
                {
                    Vector3 point = lines[2].a;
                    if (iTwo.Length == 2)
                    {
                        if (Vector3.Distance(iTwo[0], lines[1].a) < Vector3.Distance(iTwo[1], lines[1].a))
                        {
                            point = iTwo[0];
                        }
                        else
                        {
                            point = iTwo[1];
                        }
                    }
                    else point = iTwo[0];

                    //Debug.DrawLine(point, lines[2].b, Color.magenta, 60);
                    lines[2].b = point;
                }
                else
                {
                    Debug.Log("How come line 2 doesn't intersect the node?");
                }

                if (nodeLines.Count > 0)
                {
                    nodeLines.Add(new Line(lines[2].b, nodeLines[nodeLines.Count - 3].a));
                }

                nodeLines.AddRange(lines);
            }

            if (node.connections.Count == 1)
            {
                //this is a dead end node so we need to draw around the node a lil extra (and replace this line which just cuts through a node)
                nodeLines.Add(new Line(nodeLines[nodeLines.Count - 1].b, nodeLines[0].a));
            }
            else
            {
                nodeLines.Add(new Line(nodeLines[2].b, nodeLines[nodeLines.Count - 3].a));
            }

            //untangling any overlapping lines in the node before adding the final connection line in
            int lineCount = nodeLines.Count;
            for (int i = 0; i < lineCount; i++)
            {
                for (int j = i; j < lineCount; j++)
                {
                    if (j == i) continue;
                    
                    if (nodeLines[i].DoesIntersect(nodeLines[j], out Vector3 intersection))
                    {
                        //Debug.Log("j - i = " + j + " - " + i + " : " + lineCount);

                        int m = 0;
                        for (int k = i; k < lineCount; k++)
                        {
                            if (nodeLines[k] == nodeLines[i] || nodeLines[k] == nodeLines[j]) continue;
                            if (nodeLines[k].SharesPoints(nodeLines[i]) && nodeLines[k].SharesPoints(nodeLines[j]))
                            {
                                m = k;
                                break;
                            }
                        }

                        if (nodeLines[i].CloserToA(node.point)) nodeLines[i].a = intersection;
                        else nodeLines[i].b = intersection;

                        if (nodeLines[j].CloserToA(node.point)) nodeLines[j].a = intersection;
                        else nodeLines[j].b = intersection;

                        nodeLines.RemoveAt(m);
                        lineCount--;
                        j--;
                    }
                }
            }

            polygons.Add(new Polygon(nodeLines));
            shapeLines.AddRange(nodeLines);

            if(timePerNode > 0) yield return new WaitForSeconds(timePerNode);
        }

        meshCreated = true;
    }

    private void OnDrawGizmos()
    {
        if (nodeManager != null && nodeManager.nodes != null && drawPoints)
        {
            foreach (Node node in nodeManager.nodes)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawSphere(node.point, nodeRadius);
            }
        }

        if (shapeLines != null && drawLines)
        {
            for (int i = 0; i < shapeLines.Count; i++)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(shapeLines[i].a, shapeLines[i].b);
            }
        }

        if (polygons != null && drawPolygons)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                //foreach (Line line in polygons[i].lines)
                //{
                //    Gizmos.color = Color.cyan;
                //    Gizmos.DrawLine(line.a, line.b);
                //}


                for (int j = 0; j < polygons[i].vertices.Length; j++)
                {
                    if (j > 0)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(polygons[i].vertices[j], polygons[i].vertices[j - 1]);
                    }
                    //Handles.Label(polygons[i].vertices[j], j.ToString());
                }

                //Gizmos.color = Color.cyan;
                //Gizmos.DrawLine(polygons[i].vertices[0], polygons[i].vertices[polygons[i].vertices.Length - 1]);
            }
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

    [System.Serializable]
    public struct NodePair 
    {
        public Node from;
        public Node to;

        public NodePair(Node a, Node b) 
        {
            from = a;
            to = b;
        }

        public NodePair Inverse()
        {
            return new NodePair(to, from);
        }
    }

    public class ConnectionSort : IComparer<Node>
    {
        public Node start, current;

        //returns which line starts most to the left

        public int Compare(Node x, Node y)
        {
            Vector3 incomingDir = Vector3.Normalize(current.point - start.point);

            float xRot = Vector3.SignedAngle(incomingDir, Vector3.Normalize(current.point - x.point), Vector3.up);
            float yRot = Vector3.SignedAngle(incomingDir, Vector3.Normalize(current.point - y.point), Vector3.up);

            if (xRot == yRot) return 0;

            return  xRot < yRot ? 1 : -1;
        }
    }
}
