using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building_Manager : Singleton<Building_Manager>
{
    public Vector2Int buildingCountRange;
    public float buildingDepth = 5;
    public Material[] materials;

    public List<GameObject> buildings;

    public void PopulateRoad(Road road) 
    {
        GenerateSpaceMesh(road);

        //float right = -99999, left = 99999, front = -99999, back = 99999, low = 99999, high = -99999;

        //foreach (Vector3 vertex in road.vertices)
        //{
        //    if (vertex.x > right) right = vertex.x;
        //    if (vertex.x < left) left = vertex.x;
        //    if (vertex.z > front) front = vertex.z;
        //    if (vertex.z < back) back = vertex.z;
        //    if (vertex.y > high) high = vertex.y;
        //    if (vertex.y < low) low = vertex.y;
        //}

        ////Left side of the road
        //int randomCountLeft = Random.Range(buildingCountRange.x, buildingCountRange.y);
        //for (int i = 0; i < randomCountLeft; i++)
        //{
        //    Vector2 fb = GetAdjustedFB(randomCountLeft, front, back, i, road.width);
        //    GenerateBaseMesh(left, fb.x, fb.y, high, low, road, Direction.LEFT);
        //}

        ////Right side of the road
        //int randomCountRight = Random.Range(buildingCountRange.x, buildingCountRange.y);
        //for (int i = 0; i < randomCountRight; i++)
        //{
        //    Vector2 fb = GetAdjustedFB(randomCountRight, front, back, i, road.width);
        //    GenerateBaseMesh(right, fb.x, fb.y, high, low, road, Direction.RIGHT);
        //}
    }

    Vector2 GetAdjustedFB(int count, float front, float back, int index, float roadWidth) 
    {
        float tempFront = front - (roadWidth * 0.5f);
        float tempBack = back + (roadWidth * 0.5f);

        float buildingWidth = (tempFront - tempBack) / count;

        tempFront -= index * buildingWidth;
        tempBack = tempFront - buildingWidth;

        if (tempFront > tempBack)
        {
            return new Vector2(tempFront, tempBack);
        }
        else if(tempFront < tempBack)
        {
            Debug.Log("???");
            return new Vector2(tempBack, tempFront);
        }
        else
        {
            Debug.Log("??? this is real bad");
            return new Vector2(tempFront + 1, tempBack - 1);
        }
        
    }

    void GenerateBaseMesh(float road_x, float front, float back, float high, float low, Road parent, Direction direction) 
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


        //run a check to make sure that the building doesn't overlap a road
        Vector3 meshBottomLeft, meshTopRight;
        meshBottomLeft = parent.transform.TransformPoint(new Vector3(left, low, front));
        meshTopRight = parent.transform.TransformPoint(new Vector3(right, high, back));
        Vector3 meshCenter = Vector3.Lerp(meshBottomLeft, meshTopRight, 0.5f);
        Collider[] colliders = Physics.OverlapBox(meshCenter, (new Vector3(right - left, 15, front - back)/2.15f), parent.transform.rotation);
        
        foreach (Collider col in colliders)
        {
            if (col.tag == "Road" && col.gameObject != parent.gameObject)
            {
                //Don't Make building
                return;
            }

            if (col.tag == "Building")
            {
                //Set a flag to merge the meshes together?
                return;
            }
        }

        high += 0.1f;

        //Debug.Log(parent.name + " = " + right + " > " + left + " ~~ " + high + " > " + low + " ~~ " + front + " > " + back);

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
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        filter.sharedMesh = mesh;
        collider.sharedMesh = mesh;
        //rend.material = materials[Random.Range(0, materials.Length)];
        rend.material = materials[1];

        //Add Building Script
    }

    void GenerateSpaceMesh(Road road) 
    {
        // need to change it to get the points that are the corners of the shape

        LayerMask mask = 1 << LayerMask.NameToLayer("Road");
        Vector3 frontLeft = Vector3.zero, frontRight = Vector3.zero, backLeft = Vector3.zero, backRight = Vector3.zero;

        List<Vector3> leftVs = new List<Vector3>();
        List<Vector3> rightVs = new List<Vector3>();

        foreach (Vector3 vertex in road.vertices)
        {
            if (vertex.x >= road.width/3)
            {
                rightVs.Add(vertex);
            }
            else if(vertex.x <= -road.width/3)
            {
                leftVs.Add(vertex);
            }
        }

        bool frontBool, backBool;
        frontBool = false;
        backBool = false;

        if (rightVs.Count > 0)
        {
            int rvc = Mathf.FloorToInt(rightVs.Count / 2);
            for (int i = 0; i < rvc; i++)
            {
                Vector3 flatPosition = Vector3.zero;
                Vector3 vertex = road.transform.TransformPoint(rightVs[i]);
                float distance = 0;

                RaycastHit hit;
                if (Physics.BoxCast(vertex + (road.transform.right * 0.105f), new Vector3(0.1f, 10, 0.1f), road.transform.right, out hit, road.transform.rotation, 25, mask)) 
                {
                    flatPosition = hit.point;
                    if (flatPosition.y > vertex.y)
                    {
                        vertex.y = flatPosition.y;
                    }
                    else
                    {
                        flatPosition.y = vertex.y;
                    }

                    distance = Vector3.Distance(vertex, flatPosition);
                    if (distance > 0.1f)
                    {
                        frontRight = flatPosition;
                        frontLeft = vertex;
                        frontBool = true;
                        break;
                    }
                }
            }

            for (int i = rightVs.Count-1; i > rvc; i--)
            {
                Vector3 flatPosition = Vector3.zero;
                Vector3 vertex = road.transform.TransformPoint(rightVs[i]);
                float distance = 0;

                RaycastHit hit;
                if (Physics.BoxCast(vertex + (road.transform.right * 0.105f), new Vector3(0.05f, 10, 0.05f), road.transform.right, out hit, road.transform.rotation, 25, mask)) 
                {
                    flatPosition = hit.point;
                    if (flatPosition.y > vertex.y)
                    {
                        vertex.y = flatPosition.y;
                    }
                    else
                    {
                        flatPosition.y = vertex.y;
                    }

                    distance = Vector3.Distance(vertex, flatPosition);
                    if (distance > 0.1f)
                    {
                        backRight = flatPosition;
                        backLeft = vertex;
                        backBool = true;
                        break;
                    }
                }
            }

            if (frontBool && backBool)
            {
                GameObject rightBase = MakeGoWithMesh(backLeft, backRight, frontLeft, frontRight);

                rightBase.name = "Platform";
                rightBase.layer = LayerMask.NameToLayer("Ground");
                rightBase.tag = "Building";
                rightBase.transform.parent = road.transform;
                rightBase.transform.position = Vector3.zero;
                rightBase.transform.rotation = Quaternion.Euler(Vector3.zero);
            }
            else
            {
                Debug.Log("Right Failed");
            }
        }

        frontBool = false;
        backBool = false;

        if (leftVs.Count > 0)
        {
            int lvc = Mathf.FloorToInt(leftVs.Count / 2);
            for (int i = 0; i < lvc; i++)
            {
                Vector3 flatPosition = Vector3.zero;
                Vector3 vertex = road.transform.TransformPoint(leftVs[i]);
                float distance = 0;

                RaycastHit hit;
                if (Physics.BoxCast(vertex, new Vector3(0.1f, 10, 0.1f), road.transform.right * -1, out hit, Quaternion.identity, 25, mask))
                {
                    flatPosition = hit.point;
                    if (flatPosition.y > vertex.y)
                    {
                        vertex.y = flatPosition.y;
                    }
                    else
                    {
                        flatPosition.y = vertex.y;
                    }

                    distance = Vector3.Distance(vertex, flatPosition);
                    if (distance > 0.1f)
                    {
                        frontLeft = flatPosition;
                        frontRight = vertex;
                        frontBool = true;
                        break;
                    }
                }
            }

            for (int i = leftVs.Count - 1; i > lvc; i--)
            {
                Vector3 flatPosition = Vector3.zero;
                Vector3 vertex = road.transform.TransformPoint(leftVs[i]);
                float distance = 0;

                RaycastHit hit;
                if (Physics.BoxCast(vertex, new Vector3(0.05f, 10, 0.05f), road.transform.right * -1, out hit, Quaternion.identity, 25, mask))
                {
                    flatPosition = hit.point;
                    if (flatPosition.y > vertex.y)
                    {
                        vertex.y = flatPosition.y;
                    }
                    else
                    {
                        flatPosition.y = vertex.y;
                    }

                    distance = Vector3.Distance(vertex, flatPosition);
                    if (distance > 0.1f)
                    {
                        backLeft = flatPosition;
                        backRight = vertex;
                        backBool = true;
                        break;
                    }
                }
            }

            if (frontBool && backBool) 
            {
                GameObject leftBase = MakeGoWithMesh(backLeft, backRight, frontLeft, frontRight);

                leftBase.name = "Platform";
                leftBase.layer = LayerMask.NameToLayer("Ground");
                leftBase.tag = "Building";
                leftBase.transform.parent = road.transform;
                leftBase.transform.position = Vector3.zero;
                leftBase.transform.rotation = Quaternion.Euler(Vector3.zero);
            }
            else
            {
                Debug.Log("Left Failed");
            }
        }
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

    public enum Direction 
    {
        RIGHT,
        LEFT
    }
}
