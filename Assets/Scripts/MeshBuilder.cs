using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Earclipping;
using System;

public class MeshBuilder : MonoBehaviour
{
    public bool spawnMesh;
    public GameObject prefabObj;
    public Transform roadHolder;
    [Space]
    EarClipper clipper;
    public Mesh[] meshes;
    public GameObject[] roads;

    // Start is called before the first frame update
    void Start()
    {
        meshes = null;
        roads = null;
        clipper = GetComponent<EarClipper>();
    }

    // Update is called once per frame
    void Update()
    {
        if (clipper.clippingDone && meshes == null)
        {
            CreateMeshes(clipper.triList);
        }

        if (meshes != null && spawnMesh == true)
        {
            spawnMesh = false;
            CreateRoads();
            roadHolder.transform.Rotate(180, 0, 0);
        }
    }

    private void CreateRoads()
    {
        roads = new GameObject[meshes.Length];
        for (int i = 0; i < meshes.Length; i++)
        {
            roads[i] = Instantiate(prefabObj, roadHolder);
            roads[i].transform.position = clipper.nmc.polygons[i].center;

            roads[i].GetComponent<MeshFilter>().mesh = meshes[i];
            roads[i].GetComponent<MeshCollider>().sharedMesh = meshes[i];
        }
    }

    private void CreateMeshes(List<Triangle[]> triList)
    {
        meshes = new Mesh[triList.Count];
        int i = 0;
        foreach (Triangle[] polyTris in triList)
        {
            meshes[i] = new Mesh();

            List<Vector3> verts = new List<Vector3>();
            List<int> idxList = new List<int>();
            foreach (Triangle tri in polyTris)
            {
                foreach (Vector3 v in tri.vertices)
                {
                    if (!verts.Contains(v))
                    {
                        verts.Add(v);
                    }
                    idxList.Add(verts.IndexOf(v));
                }
            }
            meshes[i].vertices = verts.ToArray();
            meshes[i].triangles = idxList.ToArray();
            meshes[i].tangents = new Vector4[verts.Count];
            i++;
        }
    }
}
