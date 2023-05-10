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
            StartCoroutine(PlaceBuildings());
            spawnBuildings = false;
        }
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

                if (! Physics.CheckBox(position, spaceSize * 0.6f, Quaternion.identity, layerMask))
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
