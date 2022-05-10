using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building_Manager : Singleton<Building_Manager>
{
    Vector3[] omniDirections = new Vector3[]{
        Vector3.forward, Vector3.forward + Vector3.right, Vector3.right, Vector3.right + Vector3.back,
        Vector3.back, Vector3.back + Vector3.left, Vector3.left, Vector3.left +Vector3.forward};

    public Vector2Int buildingCountRange;
    public float buildingDepth = 5;
    public Material[] materials;
    [Space]
    public float timePerBuilding;

    internal List<Building> buildings = new List<Building>();

    public void Start()
    {
        Event_Manager.Instance._OnRoadsGenerated.AddListener(CreateBuildings);
    }

    public void CreateBuildings() 
    {
        StartCoroutine(GenerateBuildingsCoRoutine());
    }

    internal Building GetRandomBuilding()
    {
        return buildings[Random.Range(0, buildings.Count)];
    }

    IEnumerator GenerateBuildingsCoRoutine()
    {
        foreach (Road road in Road_Manager.Instance.roads)
        {
            StartCoroutine(PopulateRoad(road));
            yield return new WaitForSeconds(timePerBuilding * buildingCountRange.y * 2);
        }
        Debug.Log("Buildings Done");
        Event_Manager.Instance._OnBuildingsGenerated.Invoke();
    }

    public IEnumerator PopulateRoad(Road road) 
    {
        float right = -99999, left = 99999, front = -99999, back = 99999, low = 99999, high = -99999;

        foreach (Vector3 vertex in road.vertices)
        {
            if (vertex.x > right) right = vertex.x;
            if (vertex.x < left) left = vertex.x;
            if (vertex.z > front) front = vertex.z;
            if (vertex.z < back) back = vertex.z;
            if (vertex.y > high) high = vertex.y;
            if (vertex.y < low) low = vertex.y;
        }

        //Left side of the road
        int randomCountLeft = Random.Range(buildingCountRange.x, buildingCountRange.y);
        for (int i = 0; i < randomCountLeft; i++)
        {
            Vector2 fb = GetAdjustedFB(randomCountLeft, front, back, i, road.width);
            GenerateBaseMesh(left, fb.x, fb.y, low, road, Direction.LEFT);
            yield return new WaitForSeconds(timePerBuilding);
        }

        //Right side of the road
        int randomCountRight = Random.Range(buildingCountRange.x, buildingCountRange.y);
        for (int i = 0; i < randomCountRight; i++)
        {
            Vector2 fb = GetAdjustedFB(randomCountRight, front, back, i, road.width);
            GenerateBaseMesh(right, fb.x, fb.y, low, road, Direction.RIGHT);
            yield return new WaitForSeconds(timePerBuilding);
        }
    }

    Vector2 GetAdjustedFB(int count, float front, float back, int index, float roadWidth) 
    {
        //float tempFront = front - (roadWidth * 0.5f);
        float tempFront = front;
        //float tempBack = back + (roadWidth * 0.5f);
        float tempBack = back;

        float buildingWidth = (tempFront - tempBack) / count;

        tempFront -= index * buildingWidth;
        tempBack = tempFront - buildingWidth;

        if (tempFront > tempBack)
        {
            return new Vector2(tempFront, tempBack);
        }
        else if(tempFront < tempBack)
        {
            return new Vector2(tempBack, tempFront);
        }
        else
        {
            return new Vector2(tempFront + 1, tempBack - 1);
        }
        
    }

    void GenerateBaseMesh(float road_x, float front, float back, float y, Road parent, Direction direction) 
    {
        //set up right, left from side of road
        float right = 0, left = 0;
        switch (direction)
        {
            case Direction.RIGHT:
                left = road_x;
                right = road_x + buildingDepth;
                break;
            case Direction.LEFT:
                right = road_x;
                left = road_x - buildingDepth;
                break;
        }

        GameObject go = new GameObject("Building " + Random.Range(0,99999).ToString());

        go.transform.parent = parent.transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(Vector3.zero);
        go.tag = "Building";
        go.layer = LayerMask.NameToLayer("Building");

        //Add Building Script
        Building b = go.AddComponent<Building>();
        b.Init(new List<Vector3>(GetRandomBuildingLayout(right,left,front,back,y)) , parent, materials[Random.Range(0, materials.Length)], new Range(left, right), new Range(back, front));
        buildings.Add(b);
    }

    Vector3[] GetRandomBuildingLayout(float right, float left, float front, float back, float y) 
    {
        //Vector3 centerPoint = Vector3.Lerp(new Vector3(left, y, back), new Vector3(right, y, front), 0.5f);

        float x_diff = Mathf.Abs(right - left)/4;
        float z_diff = Mathf.Abs(front - back)/4;

        #region new broken
        List<Vector3>[] sides = new List<Vector3>[4];
        int segments = Random.Range(2, 5);
        segments *= 2;

        //add the corners to the start of each side
        sides[0] = new List<Vector3>();
        sides[0].Add(new Vector3(left, y, back));

        sides[1] = new List<Vector3>();
        sides[1].Add(new Vector3(right, y, back));

        sides[2] = new List<Vector3>();
        sides[2].Add(new Vector3(right, y, front));

        sides[3] = new List<Vector3>();
        sides[3].Add(new Vector3(left, y, front));

        //create segments along each side from corner towards next corner
        for (int i = 0; i < 4; i++)
        {
            int j = i + 1;
            if (j == 4)
            {
                j = 0;
            }

            for (int k = 0; k < segments; k++)
            {
                sides[i].Add(Vector3.Lerp(sides[i][0], sides[j][0], (float)((1.0f / segments) * (k + 1.0f))));
            }
        }

        // take each point from the sides and add it to a list
        List<Vector3> points = new List<Vector3>();
        foreach (List<Vector3> _p in sides)
        {
            foreach (Vector3 p in _p)
            {
                points.Add(p);
            }
        }

        return points.ToArray();
        #endregion

        #region old
        //Vector3[] points;
        //switch (Random.Range(0, 4))
        //{
        //    case 0:
        //    case 1:
        //        points = new Vector3[] {
        //            new Vector3(left + x_diff, y, back),
        //            new Vector3(right - x_diff, y, back),
        //            new Vector3(right, y, back + z_diff),
        //            new Vector3(right, y, front - z_diff),
        //            new Vector3(right - x_diff, y, front),
        //            new Vector3(left + x_diff, y, front),
        //            new Vector3(left, y, front - z_diff),
        //            new Vector3(left, y, back + z_diff),
        //        };
        //        break;

        //    default:
        //        points = new Vector3[] {
        //            new Vector3 (left, y, back),
        //            new Vector3 (right, y, back),
        //            new Vector3 (right, y, front),
        //            new Vector3 (left, y, front),
        //        };
        //        break;
        //}
        //return points;
        #endregion
    }
}

public enum Direction
{
    RIGHT,
    BACK,
    LEFT,
    FORWARD
}
