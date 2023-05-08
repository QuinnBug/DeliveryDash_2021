using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Earclipping;
using Utility;
using System;

public class MeshBuilder : MonoBehaviour
{
    public bool spawnMesh;
    public GameObject prefabObj;
    public Transform roadHolder;
    public Material[] materialPallette;
    [Space]
    EarClipper clipper;
    NodeMeshConstructor nmc;
    public Mesh[] meshes;
    public GameObject[] roads;

    // Start is called before the first frame update
    void Start()
    {
        meshes = null;
        roads = null;
        clipper = GetComponent<EarClipper>();
        nmc = clipper.nmc;
    }

    // Update is called once per frame
    void Update()
    {
        if (nmc.meshCreated && meshes == null)
        {
            CreateMeshes(nmc.polygons);
            spawnMesh = true;
        }

        if (meshes != null && spawnMesh == true)
        {
            spawnMesh = false;
            CreateRoads();
        }
    }

    private void CreateRoads()
    {
        roads = new GameObject[meshes.Length];
        for (int i = 0; i < meshes.Length; i++)
        {
            roads[i] = Instantiate(prefabObj, roadHolder);
            //roads[i].transform.position = clipper.nmc.polygons[i].center;

            roads[i].GetComponent<MeshFilter>().mesh = meshes[i];

            List<Material> mats = new List<Material>() { materialPallette[0] };
            for (int j = 1; j < meshes[i].subMeshCount; j++)
            {
                mats.Add(materialPallette[1]);
            }
            roads[i].GetComponent<MeshRenderer>().sharedMaterials = mats.ToArray();

            roads[i].GetComponent<MeshCollider>().sharedMesh = meshes[i];
        }
    }

    private Vector3[] CalculateNormals(Vector3[] verts, int[] idxList) 
    {
        Vector3[] normals = new Vector3[verts.Length];

        for (int k = 0; k < idxList.Length; k += 3)
        {
            int v1 = idxList[k];
            int v2 = idxList[Lists.ClampListIndex(k + 1, idxList.Length)];
            int v3 = idxList[Lists.ClampListIndex(k + 2, idxList.Length)];

            Vector3 n = Geometry.GetNormalOfPoints(verts[v1], verts[v2], verts[v3]);
            normals[v1] += n;
            normals[v2] += n;
            normals[v3] += n;
        }

        for (int k = 0; k < normals.Length; k++)
        {
            normals[k].Normalize();
        }

        return normals;
    }

    private void CreateMeshes(List<Polygon> polygons)
    {
        meshes = new Mesh[polygons.Count];
        int i = 0;
        foreach (Polygon poly in polygons)
        {
            meshes[i] = new Mesh();

            Triangle[] polyTris = clipper.GetTriangles(poly);

            List<Vector3> verts = new List<Vector3>();
            List<int> idxList = new List<int>();

            foreach (Triangle tri in polyTris)
            {
                for (int v = 0; v < tri.vertices.Length; v++)
                {
                    if (!verts.Contains(tri.vertices[v]))
                    {
                        verts.Add(tri.vertices[v]);
                    }
                    idxList.Add(verts.IndexOf(tri.vertices[v]));
                }
            }

            //need to go through each vert and find the correct normal (possibly need to check the tri formed by the normals)
            List<Vector3> normalList = new List<Vector3>(CalculateNormals(verts.ToArray(), idxList.ToArray()));
            //List<Vector2> uvList = new List<Vector2>(CalculateUVs(poly, verts));

            List<int> idxRev = new List<int>(idxList);
            idxRev.Reverse();
            idxList.AddRange(idxRev);

            meshes[i].vertices = verts.ToArray();
            //meshes[i].SetUVs(0, uvList);
            meshes[i].triangles = idxList.ToArray();
            meshes[i].normals = normalList.ToArray();
            meshes[i].tangents = new Vector4[verts.Count];

            //if the polygon is going to be a 3d mesh we need to build the other linked polygon
            if (poly.isThreeD && poly.linkedPolygons != null)
            {
                List<Mesh> meshList = new List<Mesh>() { meshes[i] };
                
                foreach (Polygon linkedPoly in poly.linkedPolygons)
                {
                    verts.Clear();
                    idxList.Clear();
                    normalList.Clear();
                    if (linkedPoly.isVert)
                    {
                        verts.Clear();
                        foreach (Vertex vertex in linkedPoly.vertices)
                        {
                            verts.Add(vertex.point);
                        }

                        //the list of vertices halved then -1 for 0 start
                        for (int top = 0; top < (linkedPoly.vertices.Length /2); top++)
                        {
                            int bottom = (linkedPoly.vertices.Length - 1) - top;

                            idxList.Add(top);
                            idxList.Add(bottom - 1);
                            idxList.Add(top + 1);

                            idxList.Add(top);
                            idxList.Add(bottom);
                            idxList.Add(bottom - 1);
                        }
                    }
                    else
                    {
                        polyTris = clipper.GetTriangles(linkedPoly);
                        
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
                    }

                    normalList = new List<Vector3>(CalculateNormals(verts.ToArray(), idxList.ToArray()));
                    //uvList = new List<Vector2>(CalculateUVs(linkedPoly, verts));

                    idxRev = new List<int>(idxList);
                    idxRev.Reverse();
                    idxList.AddRange(idxRev);

                    Mesh subMesh = new Mesh();
                    subMesh.vertices = verts.ToArray();
                    //subMesh.SetUVs(0, uvList);
                    subMesh.triangles = idxList.ToArray();
                    subMesh.normals = normalList.ToArray();
                    subMesh.tangents = new Vector4[verts.Count];
                    meshList.Add(subMesh);
                }

                CombineInstance[] combine = new CombineInstance[meshList.Count];
                for (int m = 0; m < meshList.Count; m++)
                {
                    combine[m] = new CombineInstance();
                    combine[m].mesh = meshList[m];
                    combine[m].transform = transform.localToWorldMatrix;
                }

                meshes[i] = new Mesh();
                meshes[i].CombineMeshes(combine, false);
            }

            i++;
        }
    }

    private Vector2[] CalculateUVs(List<int> idxList, List<Vector3> points)
    {
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < idxList.Count; i += 3)
        {
            Vector3 zero, one, two;
            zero = points[idxList[i]];
            one = points[idxList[i]+1];
            two = points[idxList[i]+2];

            Vector3 forward = Geometry.GetNormalOfPoints(zero, one, two);
            Vector3 bottomLeft = Vector3.positiveInfinity;

        }

        return uvs.ToArray();
    }

    //private Vector2[] CalculateUVs(Polygon poly, List<Vector3> points)
    //{
    //    List<Vector2> uvs = new List<Vector2>();
    //    List<Vertex> vertices = new List<Vertex>(poly.vertices);

    //    foreach (Vector3 point in points)
    //    {
    //        uvs.Add(vertices.Find(x => x.point == point).uv);
    //    }

    //    return uvs.ToArray();
    //}
}
