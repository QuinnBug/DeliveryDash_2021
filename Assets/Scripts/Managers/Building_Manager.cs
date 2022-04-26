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
            Building_Manager.Instance.PopulateRoad(road);
            yield return new WaitForSeconds(timePerBuilding);
        }
        Debug.Log("Buildings Done");
        Event_Manager.Instance._OnBuildingsGenerated.Invoke();
    }

    public void PopulateRoad(Road road) 
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
            GenerateBaseMesh(left, fb.x, fb.y, high, low, road, Direction.LEFT);
        }

        //Right side of the road
        int randomCountRight = Random.Range(buildingCountRange.x, buildingCountRange.y);
        for (int i = 0; i < randomCountRight; i++)
        {
            Vector2 fb = GetAdjustedFB(randomCountRight, front, back, i, road.width);
            GenerateBaseMesh(right, fb.x, fb.y, high, low, road, Direction.RIGHT);
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

    void GenerateBaseMesh(float road_x, float front, float back, float high, float low, Road parent, Direction direction) 
    {
        bool doMerge = false;
        List<GameObject> mergeObjects = null;

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


        //run a check to make sure that the building doesn't overlap things
        GameObject[] colliders;
        bool redo = true;
        int loopCounter = 0;
        while (redo)
        {
            //Debug.Log(loopCounter + " loop");
            colliders = CheckCollisions(right, left, front, back, high, low, parent);
            redo = false;
            foreach (GameObject col in colliders)
            {
                if (col.tag == "Road" && col.gameObject != parent.gameObject)
                {
                    //Don't Make building or make the building shrink away from the road? will consider
                    Road colRoad = col.GetComponent<Road>();

                    if (Vector3.Distance(colRoad.GetMeshCenter(), new Vector3(left, low, front)) >
                        Vector3.Distance(colRoad.GetMeshCenter(), new Vector3(left, low, back)))
                    {
                        back += Mathf.Abs(front - back) * 0.01f;
                    }
                    else
                    {
                        front -= Mathf.Abs(front - back) * 0.01f;
                    }
                    redo = true;
                }

                if (col.tag == "Building")
                {
                    //make the merging work

                    //if (doMerge == false)
                    //{
                    //    doMerge = true;
                    //    mergeObjects = new List<GameObject>();
                    //}

                    //if (mergeObjects != null)
                    //{
                    //    mergeObjects.Add(col.gameObject);
                    //}
                    return;
                }
            }

            if (loopCounter > 50)
            {
                redo = false;
                return;
            }
            loopCounter++;
        }

        high += Random.Range(0.5f, 7.5f);
        low -= 5;

        // create vertices and triangles
        Vector3[] vertices = {
            new Vector3 (left, low, back),
            new Vector3 (right, low, back),
            new Vector3 (right, high, back),
            new Vector3 (left, high, back),
            new Vector3 (left, high, front),
            new Vector3 (right, high, front),
            new Vector3 (right, low, front),
            new Vector3 (left, low, front),
        };

        int[] triangles = {
            0, 2, 1, //face front
			0, 3, 2,
            2, 3, 4, //face top
			2, 4, 5,
            1, 2, 5, //face right
			1, 5, 6,
            0, 7, 4, //face left
			0, 4, 3,
            5, 4, 7, //face back
			5, 7, 6,
            0, 6, 7, //face bottom
			0, 1, 6
        };

        GameObject go = new GameObject("BuildingBase");

        go.transform.parent = parent.transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(Vector3.zero);
        go.tag = "Building";

        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshRenderer rend = go.AddComponent<MeshRenderer>();
        MeshCollider collider = go.AddComponent<MeshCollider>();

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        
        //need to make merging work at some point
        if (doMerge)
        {
            go.name += " with merge";
            Debug.Log("Merge Time " + parent.name);
            List<CombineInstance> combines = new List<CombineInstance>();
            foreach (GameObject item in mergeObjects)
            {
                Mesh otherMesh = item.GetComponent<MeshFilter>().mesh;
                CombineInstance ci = new CombineInstance();
                ci.mesh = otherMesh;
                combines.Add(ci);
                item.name = "merged to other";
                item.SetActive(false);
            }
            mesh.CombineMeshes(combines.ToArray(), true);
        }

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        filter.sharedMesh = mesh;
        collider.sharedMesh = mesh;
        rend.material = materials[Random.Range(0, materials.Length)];

        //Add Building Script
        Building b = go.AddComponent<Building>();
        b.road = parent;
        b.topRight = go.transform.TransformPoint(new Vector3(right, high, front));
        b.bottomLeft = go.transform.TransformPoint(new Vector3(left, low, back));
        b.roadSide = direction;
        buildings.Add(b);

    }

    GameObject[] CheckCollisions(float right, float left, float front, float back, float high, float low, Road parent) 
    {
        List<GameObject> gameObjects = new List<GameObject>();

        Vector3 meshBottomLeft, meshTopRight;
        meshBottomLeft = parent.transform.TransformPoint(new Vector3(left, low, front));
        meshTopRight = parent.transform.TransformPoint(new Vector3(right, high, back));
        Vector3 meshCenter = Vector3.Lerp(meshBottomLeft, meshTopRight, 0.5f);
        Collider[] colliders = Physics.OverlapBox(meshCenter, (new Vector3(right - left, 15, front - back) / 2.15f), parent.transform.rotation);

        foreach (Collider col in colliders)
        {
            if (col.tag == "Road" && col.gameObject != parent.gameObject)
            {
                gameObjects.Add(col.gameObject);
            }
            else if (col.tag == "Building")
            {
                gameObjects.Add(col.gameObject);
            }
        }


        return gameObjects.ToArray();
    }

    GameObject MakeGoWithMesh(Vector3 backLeft, Vector3 backRight, Vector3 frontLeft, Vector3 frontRight) 
    {
        Vector3[] vertices = {
            backLeft,
            backRight,
            backRight + Vector3.up,
            backLeft + Vector3.up,
            frontLeft + Vector3.up,
            frontRight + Vector3.up,
            frontRight,
            frontLeft
        };

        int[] triangles = {
            0, 2, 1, //face front
			0, 3, 2,
            2, 3, 4, //face top
			2, 4, 5,
            1, 2, 5, //face right
			1, 5, 6,
            0, 7, 4, //face left
			0, 4, 3,
            5, 4, 7, //face back
			5, 7, 6,
            0, 6, 7, //face bottom
			0, 1, 6
        };

        GameObject go = new GameObject();

        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshRenderer rend = go.AddComponent<MeshRenderer>();
        MeshCollider collider = go.AddComponent<MeshCollider>();

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        filter.sharedMesh = mesh;
        collider.sharedMesh = mesh;
        //rend.material = materials[Random.Range(0, materials.Length)];
        rend.material = materials[0];

        return go;
    }
}

public enum Direction
{
    RIGHT,
    LEFT
}
