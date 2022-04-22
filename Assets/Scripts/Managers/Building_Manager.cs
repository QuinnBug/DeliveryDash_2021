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

    public List<GameObject> buildings;

    public void PopulateRoad(Road road) 
    {
        //GenerateSpaceMesh(Vector3.Lerp(road.startPoint, road.endPoint, 0.5f) + (road.transform.right * road.width));
        //GenerateSpaceMesh(Vector3.Lerp(road.startPoint, road.endPoint, 0.5f) + (road.transform.right * -road.width));

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

        high += 1;

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

    //void GenerateSpaceMesh(Vector3 _pos) 
    //{

    //    LayerMask mask = 1 << LayerMask.NameToLayer("Road");
    //    HashSet<Vector3> corners = new HashSet<Vector3>();

    //    Vector3 initialContact = Vector3.zero;
    //    Vector3 nextDirection = Vector3.zero;
    //    Quaternion nextRotation = Quaternion.identity;

    //    //Debug.DrawRay(_pos, omniDirections[0] * 60, Color.red, 10);

    //    RaycastHit hit;
    //    if (Physics.BoxCast(_pos, new Vector3(0.025f, 60, 0.025f), omniDirections[0], out hit, Quaternion.identity, 100, mask))
    //    {
    //        initialContact = hit.point;
    //        nextDirection = GetNextDirection(_pos, initialContact, hit.collider.transform);
    //        nextRotation = hit.collider.transform.rotation;
    //        Debug.DrawLine(_pos, hit.point, Color.yellow, 60);
    //    }
    //    else
    //    {
    //        Debug.Log("no initial point");
    //        return;
    //    }

    //    Vector3 nextPosition = initialContact;

    //    for (int i = 0; i < 8; i++)
    //    {
            
    //        if (Physics.BoxCast(nextPosition, new Vector3(0.0001f, 60, 0.0001f), nextDirection, out hit, nextRotation, 100, mask))
    //        {
    //            Debug.DrawLine(nextPosition + Vector3.up, hit.point + Vector3.up, Color.blue, 60);

    //            nextDirection = GetNextDirection(nextPosition, hit.point, hit.collider.transform);
    //            nextPosition = hit.point;

    //            corners.Add(nextPosition);
    //        }
    //        else
    //        {
    //            Debug.Log("Failed " + i.ToString());
    //            Debug.DrawRay(nextPosition + Vector3.up, nextDirection * 60, Color.red, 10);
    //            return;
    //        }
    //    }

    //    Debug.Log(corners.Count);

    //    #region old
    //    //List<Vector3> initialContactPoints = new List<Vector3>();

    //    //foreach (Vector3 _dir in omniDirections)
    //    //{
    //    //    RaycastHit hit;
    //    //    if (Physics.BoxCast(_pos, new Vector3(0.05f, 10, 0.05f), _dir, out hit, Quaternion.identity, 25, mask))
    //    //    {
    //    //        initialContactPoints.Add(hit.point);
    //    //    }
    //    //}

    //    //if (initialContactPoints.Count < 4)
    //    //{
    //    //    Debug.Log("Not enough contact points");
    //    //    return;
    //    //}

    //    //foreach (Vector3 point in initialContactPoints)
    //    //{

    //    //}
    //    #endregion
    //}

    //Vector3 GetNextDirection(Vector3 _start, Vector3 _end, Transform hitTransform) 
    //{
    //    Vector3 dir = Vector3.right;

    //    _start.y = 0;
    //    _end.y = 0;

    //    Vector3 rightDir = _start - _end;
    //    rightDir = Quaternion.Euler(0, 90, 0) * dir;

    //    float angleF = Vector3.Angle(hitTransform.forward, rightDir);
    //    float angleB = Vector3.Angle(hitTransform.forward * -1, rightDir);

    //    Debug.Log(angleF + " : " + angleB);

    //    return angleF > angleB ? hitTransform.forward : hitTransform.forward*-1;

    //}

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
