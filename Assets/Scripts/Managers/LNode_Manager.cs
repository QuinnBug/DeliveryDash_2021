using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Uses an L system to generate a sequence of roads, and then creates a mesh for each of them
/// </summary>
public class LNode_Manager : Singleton<LNode_Manager>
{
    public bool testConnections = false;
    [Space]
    public LSystem lSys = new LSystem();
    public int count = 5;
    [Space]
    public int length;
    public int angle;
    public float minDistMultiplier;
    public float timePerStep;
    [Space]
    public List<Node> nodes = new List<Node>();

    bool initDone = false;
    internal bool nodeGenDone = false;
    bool testRunning = false;

    public void Start()
    {
        VisualizeSequence();
    }

    public void Update()
    {
        if (testConnections && initDone && !testRunning)
        {
            StartCoroutine(ConnectionTest());
        }
    }

    private IEnumerator ConnectionTest()
    {
        //float delay = 1;
        //testRunning = true;
        //Road currentRoad = roads[0];
        //RoadConnection connection = currentRoad.startConnectedRoads.Count > 0 ? RoadConnection.START : RoadConnection.END;
        //Road nextRoad = currentRoad.GetRandomConnected(connection);

        //Vector3 rayStart, rayEnd;

        //while(testConnections)
        //{
        //    Debug.Log(currentRoad.gameObject.name + " -> " + nextRoad.gameObject.name + " @ " + connection.ToString());
        //    rayStart = currentRoad.startPoint;
        //    rayEnd = currentRoad.endPoint;

        //    Debug.DrawLine(rayStart, rayEnd, Color.cyan, delay + 600);

        //    if (nextRoad.startConnectedRoads.Count > 0 && nextRoad.startConnectedRoads.Contains(currentRoad)) 
        //    {
        //        connection = RoadConnection.END;
        //    }
        //    else if(nextRoad.endConnectedRoads.Count > 0 && nextRoad.endConnectedRoads.Contains(currentRoad))
        //    {
        //        connection = RoadConnection.START;
        //    }
        //    else
        //    {
        //        Debug.Log("There's an error here");
        //    }

        //    switch (connection)
        //    {
        //        case RoadConnection.START:
        //            if (nextRoad.startConnectedRoads.Count == 0)
        //            {
        //                connection = RoadConnection.END;
        //            }
        //            break;
        //        case RoadConnection.END:
        //            if (nextRoad.endConnectedRoads.Count == 0)
        //            {
        //                connection = RoadConnection.START;
        //            }
        //            break;
        //    }

        //    currentRoad = nextRoad;
        //    nextRoad = currentRoad.GetRandomConnected(connection);

        //    yield return new WaitForSeconds(delay);
        //}
        //testRunning = false;
        yield return new WaitForSeconds(1);
    }

    //This is the start point of generating a route
    public void VisualizeSequence() 
    {
        lSys.GenerateSequence(count);
        StartCoroutine(CreateRouteCoroutine(lSys.finalString));
        initDone = true;
    }

    public IEnumerator CreateRouteCoroutine(string sequence)
    {
        int count = 0;
        Stack<LAgent> savePoints = new Stack<LAgent>();
        Vector3 currentPos = transform.position;
        Vector3 tempPos = currentPos;
        Vector3 direction = Vector3.forward;

        nodes = new List<Node>() { new Node(currentPos) };
        Node prevNode = nodes[0];

        foreach (char letter in sequence)
        {
            Instructions _instruction = (Instructions)letter;
            switch (_instruction)
            {
                case Instructions.DRAW:
                    currentPos += direction * length;
                    prevNode = AddNode(currentPos, prevNode);
                    count++;
                    tempPos = currentPos;
                    break;

                case Instructions.LEFT_TURN:
                    direction = Quaternion.Euler(0, angle * -1, 0) * direction;
                    break;

                case Instructions.RIGHT_TURN:
                    direction = Quaternion.Euler(0, angle, 0) * direction;
                    break;

                case Instructions.SAVE:
                    savePoints.Push(new LAgent(currentPos, tempPos, direction, length));
                    break;

                case Instructions.LOAD:
                    if (savePoints.Count > 0)
                    {
                        LAgent ag = savePoints.Pop();
                        currentPos = ag.position;
                        tempPos = ag.tempPos;
                        direction = ag.direction;
                        length = ag.length;

                        foreach (Node item in nodes)
                        {
                            if (currentPos == item.point)
                            {
                                prevNode = item;
                            }
                        }
                    }
                    break;

                default:
                    break;
            }
            yield return new WaitForSeconds(timePerStep);
        }

        Debug.Log("Route Done");
        nodeGenDone = true;
    }

    private Node AddNode(Vector3 pos, Node parent)
    {
        foreach (Node item in nodes)
        {
            if (pos == item.point || Vector3.Distance(pos, item.point) <= length / minDistMultiplier)
            {
                Debug.Log("No new node needed");
                parent.AddConnection(item);
                return item;
            }
        }

        Debug.Log("Create Node");
        Node newSpace = new Node(pos, parent);
        nodes.Add(newSpace);
        return newSpace;
    }

    private void OnDrawGizmos()
    {
        if (nodes != null)
        {
            foreach (Node item in nodes)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(item.point, 5.0f);

                Gizmos.color = Color.red;
                Gizmos.DrawSphere(item.point + (item.forward * 5), 3.0f);

                foreach (Node node in item.connections)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(item.point, node.point);
                }
            }
        }
    }
}

[Serializable]
public class Node
{
    public Vector3 point;
    public Vector3 forward;
    public List<Node> connections;

    public Node(Vector3 m_point, Node parent = null) 
    {
        point = m_point;
        connections = new List<Node>();
        if (parent != null)
        {
            AddConnection(parent);
            forward = (point - parent.point).normalized;
        }
        else
        {
            forward = Vector3.forward;
        }
    }

    public void AddConnection(Node node) 
    {
        if (connections.Contains(node) || Vector3.Distance(point, node.point) >= 100)
            return;
        else 
        {
            connections.Add(node);
            node.AddConnection(node);
        }

    }
}