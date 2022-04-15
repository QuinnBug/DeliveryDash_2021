using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Terrain_Manager : Singleton<Terrain_Manager>
{
    public GameObject tilePrefab;
    public TileSettings tileSettings;
    [Space]
    public Vector3 worldStart;
    [Space]
    [SerializeField]
    private Vector2 randomOffset;
    private List<TerrainTile> tiles = new List<TerrainTile>();
    public Vector2 terrainSize;

    void GenerateTile(Vector2Int _position) 
    {
        Vector2 posXSize = _position * tileSettings.size;
        Vector3 worldPos = new Vector3(posXSize.x, 0, posXSize.y) + worldStart;
        GameObject tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);

        TerrainTile tile = tileObj.GetComponent<TerrainTile>();
        tile.settings = tileSettings;
        tile.tilePosition = _position;
        tile.perlinOffset = randomOffset;
        tile.GenerateMesh();

        tiles.Add(tile);
    }

    private void Start()
    {
        randomOffset = Random.insideUnitCircle * Random.Range(0.0f, 99999.0f);
        for (int x = 0; x < terrainSize.x; x++)
        {
            for (int y = 0; y < terrainSize.y; y++)
            {
                GenerateTile(new Vector2Int(x,y));
            }
        }

        Street_Manager.Instance.VisualizeSequence();
    }
}
