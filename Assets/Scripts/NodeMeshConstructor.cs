using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeMeshConstructor : MonoBehaviour
{
    LNode_Manager nodeManager;

    public float minVDistance;
    public float cornerDistance;
    private float abDistance;

    [Space]
    public HashSet<Node> visitedNodes;
    public List<Node> finalPath;
    [Space]
    public List<Vector3> shapePoints = null;
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

        shapePoints = null;
        visitedNodes = null;
        finalPath = null;

        abDistance = Mathf.Sqrt((cornerDistance * cornerDistance) / 2);
    }

    // Update is called once per frame
    void Update()
    {
        if (nodeManager.nodeGenDone && shapePoints == null)
        {
            //GetAllPoints();
        }

        if (nodeManager.nodeGenDone && visitedNodes == null)
        {
            GetPolygonFromNodes();
            Sweepline.Instance.polyPoints = shapePoints;
        }
    }

    void GetAllPoints() 
    {
        shapePoints = new List<Vector3>();
        shapes = new List<Bounds>();

        foreach (Node item in nodeManager.nodes)
        {
            if (item == null) continue;

            shapes.Add(GetMeshPointsFromNode(item));
        }

        for (int i = 0; i < shapes.Count; i++)
        {
            for (int j = 0; j < shapes.Count; j++)
            {
                
            }
        }

        Debug.Log("Mesh Points Done");
    }

    Bounds GetMeshPointsFromNode(Node node) 
    {
        //for each conn
        //get the direction from node to conn[i]
        //find midIPoint xDist in direction
        //get sidePoints to the left & right of midIPoint
        //add sidePoints to points if sidepoint is further away than mid corner dist from other points
        //sort points
        //return points;

        List<Vector3> points = new List<Vector3>();

        Quaternion rotation;
        Vector3 midDirPoint;

        foreach (Node conn in node.connections)
        {
            if ((conn.point - node.point).sqrMagnitude == 0) { Debug.Log(node.point + " " + conn.point); continue; }
            rotation = Quaternion.LookRotation(conn.point - node.point, Vector3.up);
            midDirPoint = node.point + (rotation * (Vector3.forward * abDistance));
            //does -1 and 1 for left & right
            for (int i = -1; i < 2; i += 2)
            {
                //                        (Quaternion * Vector3) gives us the vector rotated by the Quaternion
                Vector3 p = midDirPoint + (rotation * (Vector3.right * abDistance * i));
                if (points.Contains(p)) continue;
                points.Add(p);
            }
        }

        if (node.connections.Count == 1)
        {
            points.Add(node.point);
        }

        return new Bounds(points.ToArray());
    }

    void GetPolygonFromNodes()
    {
        //setting the start node.
        int k = 0;
        while (nodeManager.nodes[k].connections.Count > 1)
        {
            k++;
        }
        Node startNode = nodeManager.nodes[k];

        visitedNodes = new HashSet<Node>() { startNode };
        finalPath = new List<Node>();

        finalPath.AddRange(ExploreBranch(startNode, 0));

        //We have a list of nodes that we can follow to visit all connected nodes

        int j = 0;
        shapePoints = new List<Vector3>();
        Quaternion rotation;
        Vector3 midDirPoint;

        for (int i = 0; i < finalPath.Count; i++)
        {
            j = i + 1;

            if (j >= finalPath.Count)
            {
                j = 0;
            }
            //we need to get a point to the left of the mid point between current and next
            //(Quaternion * Vector3) gives us the vector rotated by the Quaternion

            if ((finalPath[j].point - finalPath[i].point).sqrMagnitude == 0) { Debug.Log(finalPath[i].point + " " + finalPath[j].point); continue; }
            rotation = Quaternion.LookRotation(finalPath[j].point - finalPath[i].point, Vector3.up);

            float distance = Vector3.Distance(finalPath[i].point, finalPath[j].point); 

            //a quarter of the way between the points and to the left.

            midDirPoint = finalPath[i].point + (rotation * (Vector3.forward * distance * 0.25f));
            shapePoints.Add(midDirPoint + (rotation * (Vector3.right * abDistance * -1)));

            midDirPoint = finalPath[i].point + (rotation * (Vector3.forward * distance * 0.75f));
            shapePoints.Add(midDirPoint + (rotation * (Vector3.right * abDistance * -1)));
        }

        //shapePoints.Add(shapePoints[0]);

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
            List<Node> connections = new List<Node>(ValidConnections(current, path));

            visitedNodes.Add(current);

            //we continue exploring until reaching a deadend or a branch
            exploring = connections.Count == 1;

            //if we only have one path we move to that one
            if (exploring)
            {
                //Debug.Log("Found Connection");
                path.Add(current);
                current = connections[0];
            }
            else
            {
                List<Node> rev_path = new List<Node>(path);
                rev_path.Reverse();

                path.Add(current);

                //if we've reached a branching node it will need to explore those paths
                if (connections.Count > 1)
                {
                    ConnectionSort cs = new ConnectionSort();
                    cs.start = startNode;
                    cs.current = current;
                    connections.Sort(cs);

                    //Debug.Log("Reached Branch @ " + current.point);
                    foreach (Node connection in connections)
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
                    Node previousNode = path[path.Count - 1];
                    if (current.connections.Count > 1)
                    {
                        foreach (Node newNode in current.connections)
                        {
                            if (newNode == previousNode) continue;

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
            //if (!path.Contains(connection) && !visitedNodes.Contains(testNode)) valids.Add(connection);
            if (!visitedNodes.Contains(connection) && !valids.Contains(connection)) valids.Add(connection);
        }

        return valids.ToArray();
    }

    private void TriangulateFromPoints(List<Vector3> points) 
    {
        //Ear Clipping method
    }

    private void OnDrawGizmosSelected()
    {
        //if (finalPath != null && finalPath.Count >= displayCount)
        //{
        //    pTimer -= Time.deltaTime;

        //    if (pTimer <= 0)
        //    {
        //        pTimer = switchTime;
        //        currentP++;
        //    }

        //    if (currentP >= finalPath.Count - displayCount) currentP = 0;

        //    for (int i = currentP; i < currentP + displayCount; i++)
        //    {
        //        Gizmos.color = Color.magenta;
        //        Gizmos.DrawSphere(finalPath[i].point, 30);
        //    }
        //}

        if (shapePoints != null && shapePoints.Count >= displayCount)
        {
            pTimer -= Time.deltaTime;

            if (pTimer <= 0)
            {
                pTimer = switchTime;
                currentP++;
            }

            if (currentP >= shapePoints.Count - displayCount) currentP = 0;

            if (drawPoints)
            {
                for (int i = currentP; i < currentP + displayCount; i++)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(shapePoints[i] + Vector3.up, 10);
                }
            }

            if (drawLines)
            {
                for (int i = currentP; i < currentP + displayCount - 1; i++)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(shapePoints[i], shapePoints[i+1]);
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

    [System.Serializable]
    public struct Path 
    {
        public Node[] points;

        public Path(Node[] path) 
        {
            points = path;
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
