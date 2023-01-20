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
    public List<Node> fullPath;
    public List<Path> pathList;
    [Space]
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
        fullPath = null;

        abDistance = Mathf.Sqrt((cornerDistance * cornerDistance) / 2);
    }

    // Update is called once per frame
    void Update()
    {
        if (nodeManager.nodeGenDone && points == null)
        {
            //GetAllPoints();
        }

        if (nodeManager.nodeGenDone && fullPath == null)
        {
            GetPolygonFromNodes();
        }
    }

    void GetAllPoints() 
    {
        points = new List<Vector3>();
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
        /*
         * 1. First we need to save the start node which needs to be a node with only 1 connection
         * 2. Then we need to explore the connected nodes until we reach a node with branches
         * 3. we then need to explore each branch until they reach another branching node or the end of the branch
         * 4. If we have a branch that ends then we finish exploring that branch to completion
         * 5. If we only have branches that reach branching nodes we go from step 2 for the available branch
         * 6. If we reach a point where there are no more available branches we path back to the previous branch
         * 7. when we reach the start node, we should have explored all branches
         */

        //setting the start node.
        int i = 0;
        Node startNode = nodeManager.nodes[0];
        //In theory having multiple connections is valid for the start node, but we need to start without that for testing
        while (startNode.connections.Count > 1 && i < nodeManager.nodes.Count)
        {
            i++;
            startNode = nodeManager.nodes[i];
        }

        Debug.Log("Starting at node " + i);

        fullPath = new List<Node>() { startNode };
        ExploreBranch(startNode, 0);
    }

    private Node[] ExploreBranch(Node startNode, int pathToFollow)
    {
        bool exploring = true;

        List<Node> path = new List<Node>();

        Node current = startNode.connections[pathToFollow];
        
        while (exploring)
        {
            Node[] connections = ValidConnections(current, path);
            exploring = connections.Length == 1;

            //if we only have one path we move to that one
            if (connections.Length == 1)
            {
                Debug.Log("Found Connection");
                path.Add(current);
                current = connections[0];
            }
            //if we've reached a dead end
            else if (connections.Length == 0)
            {
                List<Node> rev_path = new List<Node>(path);
                rev_path.Reverse();

                path.Add(current);
                path.AddRange(rev_path);

                pathList.Add(new Path(path.ToArray()));
            }
            //if we've reached a branch;
            else if (connections.Length > 1)
            {
                //we need to explore all the paths
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
            if (!path.Contains(connection) && !fullPath.Contains(testNode)) valids.Add(connection);
        }

        return valids.ToArray();
    }

    private void TriangulateFromPoints(List<Vector3> points) 
    {
        //Ear Clipping method
    }

    private void OnDrawGizmos()
    {
        if (fullPath != null && fullPath.Count >= displayCount)
        {
            pTimer -= Time.deltaTime;

            if (pTimer <= 0)
            {
                pTimer = switchTime;
                currentP++;
            }

            if (currentP >= fullPath.Count - displayCount) currentP = 0;

            for (int i = currentP; i < currentP + displayCount; i++)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(fullPath[i].point, 15);
            }
        }
        else if (shapes != null && shapes.Count >= displayCount)
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
}
