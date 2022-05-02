using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AI_Base : MonoBehaviour
{
    public float speed = 1.0f;
    public float turnSpeed = 1.0f;
    public int pathCount = 2;
    [Space]
    public RoadNavigation nav = new RoadNavigation();
    [Space]
    public List<StreetTravel[]> paths = new List<StreetTravel[]>();

    private int pathIndex = 0;
    private int routeIndex = 0;
    private bool runningTest = false;

    Rigidbody rb;
    SuspensionSystem suspension;

    public void Init()
    {
        rb = GetComponent<Rigidbody>();
        suspension = GetComponent<SuspensionSystem>();

        pathIndex = 0;
        routeIndex = 0;

        nav.currentRoad = Road_Manager.Instance.roads[Random.Range(0, Road_Manager.Instance.roads.Count)];
        nav.targetRoad = Road_Manager.Instance.roads[Random.Range(0, Road_Manager.Instance.roads.Count)];
        
        nav.GeneratePathToTarget();
        paths.Add(nav.path);

        for (int i = 0; i < pathCount; i++)
        {
            nav.targetRoad = Road_Manager.Instance.roads[Random.Range(0, Road_Manager.Instance.roads.Count)];
            nav.GeneratePathToTarget(new StreetTravel[1] { nav.path[nav.path.Length - 1] });
            paths.Add(nav.path);
        }

        nav.targetRoad = paths[0][0].road;
        nav.GeneratePathToTarget(new StreetTravel[1] { nav.path[nav.path.Length - 1] });
        paths.Add(nav.path);

        nav.path = paths[routeIndex];
        transform.position = nav.currentRoad.startPoint;
        transform.rotation = Quaternion.Euler(Vector3.zero);
    }

    public void NewTarget() 
    {
        pathIndex = 0;
        routeIndex++;
        if (routeIndex >= paths.Count)
        {
            routeIndex = 0;
        }
        nav.path = paths[routeIndex];
    }

    public void Update()
    {
        if (nav.path != null)
        {
            MoveAlongRoute();
        }

        if (suspension.GroundedPercent() < 0.5f)
        {
            transform.rotation = Quaternion.Euler(Vector3.zero);
        }
    }

    void MoveAlongRoute() 
    {
        if (pathIndex >= nav.path.Length)
        {
            NewTarget();
        }

        StreetTravel street = nav.path[pathIndex];

        //get the point on the left hand side of the street
        Vector3 streetDirection = street.GetStartPoint() - street.GetExitPoint();
        streetDirection = Quaternion.Euler(0, -90, 0) * streetDirection.normalized;
        Vector3 targetPosition = street.GetExitPoint() + (streetDirection * (-street.road.width / 5) );
        targetPosition.y = transform.position.y;

        if (suspension.GroundedPercent() >= 0.75f)
        {
            rb.AddForce((targetPosition - transform.position).normalized * speed);
            transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.LookRotation(rb.velocity, Vector3.up), turnSpeed * Time.deltaTime);
        }
        //transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(targetPosition, transform.position) < 0.5f)
        {
            pathIndex += 1;
        }
    }

    public void OnDrawGizmosSelected()
    {
        if (nav.currentRoad != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(nav.currentRoad.GetMeshCenter() + Vector3.up, 3.0f);
        }

        if (nav.targetRoad != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(nav.targetRoad.GetMeshCenter() + Vector3.up, 3.0f);
        }
    }
}

[Serializable]
public class RoadNavigation 
{
    public Road currentRoad;
    public Road targetRoad;
    public StreetTravel[] path;

    public void GeneratePathToTarget(StreetTravel[] initialOptions) 
    {
        List<StreetTravel> closedList = new List<StreetTravel>(); // closed list
        List<PathNode> openList = new List<PathNode>(); //open List
        List<StreetTravel> tempHistory = new List<StreetTravel>();
        foreach (StreetTravel item in initialOptions)
        {
            openList.Add(new PathNode(targetRoad, tempHistory, item));
        }


        while (openList.Count > 0)
        {
            PathNode currentNode = openList[0];
            foreach (PathNode node in openList)
            {
                if (node.f < currentNode.f)
                {
                    currentNode = node;
                }
            }

            closedList.Add(currentNode.streetTravel);
            openList.Remove(currentNode);

            if (currentNode.streetTravel.road == targetRoad)
            {
                path = currentNode.history;
                //Debug.Log("Found Path " + path.Length);
                return;
            }

            foreach (Road road in currentNode.streetTravel.GetOutGoingRoads())
            {
                StreetTravel newStreet = new StreetTravel(road, road.GetConnectionToRoad(currentNode.streetTravel.road));

                foreach (StreetTravel closedST in closedList)
                {
                    if (closedST.Equals(newStreet))
                    {
                        continue;
                    }
                }

                bool alreadyOpen = false;

                tempHistory = new List<StreetTravel>(currentNode.history);
                tempHistory.Add(currentNode.streetTravel);
                PathNode newNode = new PathNode(targetRoad, tempHistory, newStreet);

                for (int i = 0; i < openList.Count; i++)
                {
                    PathNode node = openList[i];
                    if (node.streetTravel.Equals(newStreet))
                    {
                        alreadyOpen = true;

                        if (newNode.g < node.g)
                        {
                            openList[i] = newNode;
                        }
                    }
                }

                if (!alreadyOpen)
                {
                    openList.Add(newNode);
                }
            }
        }
        Debug.Log("Failed To Find Path");
    }

    public void GeneratePathToTarget() 
    {
        GeneratePathToTarget(new StreetTravel[2] { new StreetTravel(currentRoad, RoadConnection.START), new StreetTravel(currentRoad, RoadConnection.END) });
    }
}

[Serializable]
public struct StreetTravel 
{
    public Road road;
    public RoadConnection exit;
    public RoadConnection entrance;

    public StreetTravel(Road _road, RoadConnection _entrance) 
    {
        road = _road;
        entrance = _entrance;
        exit = _entrance == RoadConnection.START ? RoadConnection.END : RoadConnection.START;
        switch (entrance)
        {
            case RoadConnection.START:
                if(road.endConnectedRoads.Count != 0 && road.startConnectedRoads.Count == 0)
                {
                    exit = RoadConnection.END;
                }
                else if (road.endConnectedRoads.Count == 0 && road.startConnectedRoads.Count != 0)
                {
                    exit = RoadConnection.START;
                }
                else
                {
                    exit = RoadConnection.NULL;
                }
                break;

            case RoadConnection.END:
                if (road.endConnectedRoads.Count == 0 && road.startConnectedRoads.Count != 0)
                {
                    exit = RoadConnection.START;
                }
                else if (road.endConnectedRoads.Count != 0 && road.startConnectedRoads.Count == 0)
                {
                    exit = RoadConnection.END;
                }
                else
                {
                    exit = RoadConnection.NULL;
                }
                break;

            default:
                Debug.Log("Setting Exit to null " + road.name);
                exit = RoadConnection.NULL;
                break;
        }
    }

    public bool Equals(StreetTravel other) 
    {
        return (road == other.road && exit == other.exit);
    }

    internal Road[] GetOutGoingRoads()
    {
        Road[] roads = new Road[0];
        switch (exit)
        {
            case RoadConnection.START:
                roads = road.startConnectedRoads.ToArray();
                break;

            case RoadConnection.END:
                roads = road.endConnectedRoads.ToArray();
                break;

            default:
                Debug.Log("Invalid Exit state " + road.name + " " + exit);
                if (road.endConnectedRoads.Count != 0)
                {
                    roads = road.endConnectedRoads.ToArray();
                }
                else
                {
                    roads = road.startConnectedRoads.ToArray();
                }
                break;
        }

        return roads;
    }

    internal Vector3 GetExitPoint() 
    {
        return exit == RoadConnection.START ? road.startPoint : road.endPoint;
    }

    internal Vector3 GetStartPoint()
    {
        return entrance == RoadConnection.START ? road.startPoint : road.endPoint;
    }
}

public struct PathNode 
{
    public float f;
    public float g;
    public float h;
    public StreetTravel streetTravel;
    public StreetTravel[] history;

    public PathNode(Road target, List<StreetTravel> _history, StreetTravel _streetTravel) 
    {
        streetTravel = _streetTravel;
        Vector3 current = streetTravel.exit == RoadConnection.START ? streetTravel.road.startPoint : streetTravel.road.endPoint;
        g = _history.Count;
        float s = Vector3.Distance(current, target.startPoint);
        float e = Vector3.Distance(current, target.endPoint);
        h = s > e ? e : s;
        f = g + h;
        history = _history.ToArray();
    }
}
