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
    public bool showConnections = false;
    public bool showNodes = false;
    [Space]
    public LSystem lSys = new LSystem();
    public int count = 5;
    [Space]
    public int length;
    public int angle;
    //below the minimum the nodes combine, above the maximum connections are broken
    public Range nodeLimitRange;
    public int nodesPerStep = 50;
    public float timePerStep;
    [Space]
    internal List<Node> nodes = new List<Node>();

    internal bool nodeGenDone = false;

    public void Start()
    {
        VisualizeSequence();
    }

    //This is the start point of generating a route
    public void VisualizeSequence() 
    {
        lSys.GenerateSequence(count);
        StartCoroutine(CreateRouteCoroutine(lSys.finalString));
    }

    public IEnumerator CreateRouteCoroutine(string sequence)
    {
        int counter = 0;
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
                    counter++;
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

            //Debug.Log("Counter = " + counter);
            if (counter % nodesPerStep == 0)
            {
                //Debug.Log("Step");
                yield return new WaitForSeconds(timePerStep);
            }
        }

        Debug.Log("Route Done");

        //sort each nodes connections
        foreach (Node item in nodes)
        {
            item.SortConnections();
        }

        nodeGenDone = true;
    }

    private Node AddNode(Vector3 pos, Node parent)
    {
        bool crossover = false;

        Node newSpace = new Node(pos);
        Line connLine = new Line(pos, parent.point);
        foreach (Node item in nodes)
        {
            if (pos == item.point || Vector3.Distance(pos, item.point) <= nodeLimitRange.min)
            {
                parent.AddConnection(item);
                return item;
            }
        }

        foreach (Node item in nodes)
        {
            Line testLine = new Line(item.point, pos);
            for (int i = 0; i < item.connections.Count; i++)
            {
                Node otherItem = item.connections[i];
                testLine.b = otherItem.point;
                if (connLine.DoesIntersect(testLine, out Vector3 intersection))
                {
                    item.RemoveConnection(otherItem);
                    i--;

                    //item.AddConnection(parent);
                    //item.AddConnection(newSpace);

                    //otherItem.AddConnection(parent);
                    //otherItem.AddConnection(newSpace);

                    //crossover = true;
                }
            }
        }

        if(!crossover) newSpace.AddConnection(parent);
        nodes.Add(newSpace);
        return newSpace;
    }

    private void OnValidate()
    {
        if (nodeLimitRange.min >= length)
        {
            nodeLimitRange.min = length - 5;
        }

        if (nodeLimitRange.max <= length)
        {
            nodeLimitRange.max = nodeLimitRange.min + length + 5;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (nodes != null)
        {
            foreach (Node item in nodes)
            {
                if (showNodes)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(item.point, length / 10.0f);
                }

                if (showConnections)
                {
                    foreach (Node node in item.connections)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(item.point, node.point);
                    }
                }
            }
        }
    }
}

//[System.Serializable]
public class Node
{
    public Vector3 point;
    internal Vector3 forward;
    internal List<Node> connections;

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
        //if node

        if (node == this || connections.Contains(node) || Vector3.Distance(point, node.point) >= LNode_Manager.Instance.nodeLimitRange.max)
            return;
        else 
        {
            connections.Add(node);
            node.connections.Add(this);
        }

    }

    public void RemoveConnection(Node node) 
    {
        if (node == this || !connections.Contains(node))
            return;
        else
        {
            node.connections.Remove(this);
            connections.Remove(node);
        }
    }

    public void RemoveConnection(int i) 
    {
        RemoveConnection(connections[i]);
    }

    public void SortConnections()
    {
        connections.Sort(new ClockwiseComparer(Vector2.right));
    }
}

public class ClockwiseComparer : IComparer<Node>
{
    private Vector2 m_Origin;

    #region Properties

    public Vector2 origin { get { return m_Origin; } set { m_Origin = value; } }

    #endregion

    /// <summary>
    ///     Initializes a new instance of the ClockwiseComparer class.
    /// </summary>
    /// <param name="origin">Origin.</param>
    public ClockwiseComparer(Vector2 origin)
    {
        m_Origin = origin;
    }

    #region IComparer Methods

    /// <summary>
    ///     Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
    /// </summary>
    /// <param name="first">First.</param>
    /// <param name="second">Second.</param>
    public int Compare(Node first, Node second)
    {
        return IsClockwise(first.point, second.point, m_Origin);
    }

    #endregion

    /// <summary>
    ///     Returns 1 if first comes before second in clockwise order.
    ///     Returns -1 if second comes before first.
    ///     Returns 0 if the points are identical.
    /// </summary>
    /// <param name="first">First.</param>
    /// <param name="second">Second.</param>
    /// <param name="origin">Origin.</param>
    public static int IsClockwise(Vector2 first, Vector2 second, Vector2 origin)
    {
        if (first == second)
            return 0;

        Vector2 firstOffset = first - origin;
        Vector2 secondOffset = second - origin;

        float angle1 = Mathf.Atan2(firstOffset.x, firstOffset.y);
        float angle2 = Mathf.Atan2(secondOffset.x, secondOffset.y);

        if (angle1 < angle2)
            return -1;

        if (angle1 > angle2)
            return 1;

        // Check to see which point is closest
        //return (firstOffset.sqrMagnitude < secondOffset.sqrMagnitude) ? -1 : 1;

        return (firstOffset.sqrMagnitude < secondOffset.sqrMagnitude) ? -1 : 1;
    }
}