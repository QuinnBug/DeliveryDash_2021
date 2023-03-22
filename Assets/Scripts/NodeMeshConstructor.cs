using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class NodeMeshConstructor : MonoBehaviour
{
    LNode_Manager nodeManager;

    public float minVDistance;
    public float cornerDistance;
    public float nodeRadius;
    private float abDistance;

    [Space]
    public HashSet<Node> visitedNodes;
    public List<Node> finalPath;
    [Space]
    public List<Vector3> shapePoints = null;
    public List<Line> shapeLines = null;
    [Space]
    [Tooltip("Set above 1 to force all connections to be skipped, below 0 to do all connections")]
    public float connSkipChance = 0.5f;
    [Space]
    //display variables
    public bool drawPoints;
    public bool drawLines;
    public int currentP = 0;
    public int displayCount = 64;
    public float pTimer = 0;
    public float switchTime = 0.5f;

    private Node startNode;

    // Start is called before the first frame update
    void Start()
    {
        nodeManager = LNode_Manager.Instance;

        shapePoints = null;
        visitedNodes = null;
        finalPath = null;

        abDistance = Mathf.Sqrt((cornerDistance * cornerDistance) / 2);
    }

    // Update is called once per frame
    void Update()
    {
        if (nodeManager.nodeGenDone && visitedNodes == null)
        {
            //GetFullExplorationPath();
            CreatePolygonFromNodes();
            Sweepline.Instance.polyPoints = shapePoints;
        }
    }

    void GetFullExplorationPath()
    {
        //setting the start node.
        int k = 0;
        while (nodeManager.nodes[k].connections.Count > 1)
        {
            k++;
        }
        startNode = nodeManager.nodes[k];

        visitedNodes = new HashSet<Node>() { startNode };
        finalPath = new List<Node>();

        finalPath.AddRange(ExploreBranch(startNode, 0));
    }

    void CreatePolygonFromNodes() 
    {
        shapeLines = new List<Line>();
        visitedNodes = new HashSet<Node>();

        foreach (Node node in nodeManager.nodes)
        {
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

                if (lines[0].CircleIntersections(node.point, nodeRadius, out Vector3 onePoint))
                {
                    Debug.DrawLine(lines[0].a, onePoint, Color.red, 60);
                    lines[0].a = onePoint;
                }
                else
                {
                    Debug.Log("How come line 0 doesn't intersect the node?");
                }

                if (lines[2].CircleIntersections(node.point, nodeRadius, out Vector3 twoPoint))
                {
                    Debug.DrawLine(twoPoint, lines[2].b, Color.magenta, 60);
                    lines[2].b = twoPoint;
                }
                else
                {
                    Debug.Log("How come line 2 doesn't intersect the node?");
                }

                if (nodeLines.Count > 0)
                {
                    nodeLines.Add(new Line(nodeLines[nodeLines.Count - 1].b, lines[0].a));
                }
                nodeLines.AddRange(lines);
            }

            if (node.connections.Count == 1)
            {
                //this is a dead end node so we need to draw around the node a lil extra (and replace this line)
                nodeLines.Add(new Line(nodeLines[nodeLines.Count - 1].b, nodeLines[0].a));
            }
            else
            {
                nodeLines.Add(new Line(nodeLines[nodeLines.Count - 1].b, nodeLines[0].a));
            }

            shapeLines.AddRange(nodeLines);
        }
    }

    /// <summary>
    /// Recursive exploration function. It will find a path that will move from the start node to any dead end or branching points
    /// </summary>
    private Node[] ExploreBranch(Node startNode, int pathToFollow)
    {
        bool exploring = true;
        visitedNodes.Add(startNode);

        List<Node> path = new List<Node>();
        path.Add(startNode);


        Node current = startNode.connections[pathToFollow];
        
        while (exploring)
        {
            visitedNodes.Add(current);
            List<Node> validConnections = new List<Node>(ValidConnections(current, path));

            //we continue exploring until reaching a deadend or a branch
            exploring = validConnections.Count == 1;

            //if we only have one path we move to that one
            if (exploring)
            {
                //Debug.Log("Found Connection");
                path.Add(current);
                current = validConnections[0];
            }
            else
            {
                List<Node> rev_path = new List<Node>(path);
                rev_path.Reverse();

                path.Add(current);

                //if we've reached a branching node it will need to explore those paths
                if (validConnections.Count > 1)
                {
                    ConnectionSort cs = new ConnectionSort();
                    cs.start = startNode;
                    cs.current = current;
                    validConnections.Sort(cs);

                    //Debug.Log("Reached Branch @ " + current.point);
                    foreach (Node connection in validConnections)
                    {
                        path.AddRange(ExploreBranch(current, current.connections.IndexOf(connection)));
                        path.Add(current);
                    }
                    //path.Add(current);
                }
                else
                {
                    //Debug.Log("Reached Deadend @ " + current.point);
                    //if this node has more than one connection it should reach out to each of it's connections before returning, except for the connection we just came from.
                    Node previousNode = path[path.Count - 2];
                    
                    if (current.connections.Count > 1)
                    {
                        float rndNum;
                        foreach (Node newNode in current.connections)
                        {
                            if (newNode == previousNode) continue;
                            rndNum = Random.Range(0.0f, 1.0f);
                            if (rndNum > connSkipChance) { /*Debug.Log("Skipped Connection");*/ continue; }

                            path.Add(newNode);
                            path.Add(current);
                        }
                    }
                }

                path.AddRange(rev_path);
            }
        }

        int length = path.Count - 1;
        for (int i = 0; i < length; i++)
        {
            if (path[i] == path[i + 1])
            {
                path.RemoveAt(i + 1);
                length--;
                i--;
            }
        }

        return path.ToArray();
    }

    //returns all connections that aren't present in the path or the overall full path
    private Node[] ValidConnections(Node testNode, List<Node> path) 
    {
        List<Node> valids = new List<Node>();
        foreach (Node connection in testNode.connections)
        {
            if (!visitedNodes.Contains(connection) && !valids.Contains(connection)) valids.Add(connection);
        }

        return valids.ToArray();
    }

    private void OnDrawGizmosSelected()
    {
        if (shapeLines != null && shapeLines.Count > 0)
        {
            if (drawPoints)
            {
                foreach (Node node in nodeManager.nodes)
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawSphere(node.point, nodeRadius);
                }
            }

            if (drawLines)
            {
                for (int i = 0; i < shapeLines.Count; i++)
                {
                    Gizmos.color = i % 2 == 0 ? Color.green : Color.cyan;
                    Gizmos.DrawLine(shapeLines[i].a, shapeLines[i].b);
                }
            }
        }

        //if (shapePoints != null && shapePoints.Count >= displayCount)
        //{
        //    pTimer -= Time.deltaTime;

        //    if (pTimer <= 0)
        //    {
        //        pTimer = switchTime;
        //        currentP++;
        //    }

        //    if (currentP > shapePoints.Count - displayCount) currentP = 0;

        //    if (drawPoints)
        //    {
        //        for (int i = currentP; i < currentP + displayCount; i++)
        //        {
        //            Gizmos.color = Color.green;
        //            Gizmos.DrawSphere(shapePoints[i] + Vector3.up, 10);
        //        }
        //    }

        //    if (drawLines)
        //    {
        //        for (int i = currentP; i < currentP + displayCount - 1; i++)
        //        {
        //            Gizmos.color = Color.green;
        //            Gizmos.DrawLine(shapePoints[i], shapePoints[i+1]);
        //        }
        //    }
        //}
        //else
        //{
        //    currentP = 0;
        //    pTimer = switchTime;
        //}
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

            float xRot = Vector3.SignedAngle(incomingDir, Vector3.Normalize(x.point - start.point), Vector3.up);
            float yRot = Vector3.SignedAngle(incomingDir, Vector3.Normalize(y.point - start.point), Vector3.up);

            if (xRot == yRot) return 0;

            return  xRot > yRot ? 1 : -1;
        }
    }
}
