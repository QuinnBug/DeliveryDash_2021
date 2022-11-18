using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGen : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Terrain_Manager.Instance.CreateTerrain();
        Road_Manager.Instance.VisualizeSequence();
    }
}
