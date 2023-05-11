using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BuildingPopulator : Singleton<BuildingPopulator>
{
    public float timePerStep;
    public float iterationsPerStep;
    [Space]
    public GameObject[] prefabs;
    [Space]
    public Vector2Int gridSize;
    public Vector3 spaceSize;
    public Vector3 startPoint;
    [Space]
    public MeshBuilder mb;

    private GameObject[][] buildings;
    private LayerMask layerMask;
    private int lastPrefab;

    internal bool spawnBuildings;
    internal int stepIterations = 0;

    private void Start()
    {
        lastPrefab = -1;
        layerMask = 1 << LayerMask.NameToLayer("Road");
        spawnBuildings = false;
        spaceSize.y = 100;
    }


    private void Update()
    {
        if (spawnBuildings)
        {
            Vector3[] bounds = FindLowestPoint();

            startPoint = bounds[0] - (spaceSize * 3);
            startPoint.y = bounds[0].y;

            Vector3 gridSizeFloat = ((bounds[1] + (spaceSize*3)) - startPoint);
            gridSize.x = Mathf.CeilToInt(gridSizeFloat.x / spaceSize.x);
            gridSize.y = Mathf.CeilToInt(gridSizeFloat.z / spaceSize.z);

            StartCoroutine(PlaceBuildings());
            spawnBuildings = false;
        }
    }

    private Vector3[] FindLowestPoint()
    {
        Vector3 lowestPoint = Vector3.positiveInfinity;
        Vector3 highestPoint = Vector3.negativeInfinity;

        foreach (Mesh mesh in mb.meshes)
        {
            foreach (Vector3 vertex in mesh.vertices)
            {
                if (vertex.x < lowestPoint.x) lowestPoint.x = vertex.x;
                if (vertex.z < lowestPoint.z) lowestPoint.z = vertex.z;

                if (vertex.x > highestPoint.x) highestPoint.x = vertex.x;
                if (vertex.z > highestPoint.z) highestPoint.z = vertex.z;

            }
        }

        lowestPoint.y = highestPoint.y = startPoint.y;

        return new Vector3[] { lowestPoint, highestPoint };
    }

    IEnumerator PlaceBuildings() 
    {
        buildings = new GameObject[gridSize.x][];
        Vector3 position = startPoint;

        for (int x = 0; x < gridSize.x; x++)
        {
            buildings[x] = new GameObject[gridSize.y];
            for (int y = 0; y < gridSize.y; y++)
            {
                //idx - 
                position = startPoint;
                position.x += x * spaceSize.x + (spaceSize.x / 2);
                position.z += y * spaceSize.z + (spaceSize.z / 2);

                if (! Physics.CheckBox(position, spaceSize * 0.4f, Quaternion.identity, layerMask))
                {
                    buildings[x][y] = Instantiate(RandomPrefab(), position, Quaternion.identity, transform);
                }
                stepIterations++;

                if (stepIterations >= iterationsPerStep)
                {
                    yield return new WaitForSeconds(timePerStep);
                    stepIterations = 0;
                }
            }
        }
    }

    private GameObject RandomPrefab()
    {
        int rndNum = lastPrefab;

        while (rndNum == lastPrefab)
        {
            rndNum = Random.Range(0, prefabs.Length);
        }

        lastPrefab = rndNum;
        return prefabs[rndNum];
    }
}
