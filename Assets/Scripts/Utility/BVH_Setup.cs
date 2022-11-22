using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BVH_Setup : MonoBehaviour
{
    BoundingVolume root;
    NodeMeshConstructor nmc;
    private List<Vector3> points = null;


    // Start is called before the first frame update
    void Start()
    {
        root = new BoundingVolume(nmc.points, GetBounds(nmc.points.ToArray()));
    }

    Vector3[] GetBounds(Vector3[] points) 
    {
        Vector3 tr = Vector3.positiveInfinity;
        Vector3 bl = Vector3.negativeInfinity;

        return new Vector3[] { bl, tr };
    }

    void CreateBVH() 
    {
        bool loop = true;

        while (loop)
        {

        }
    }
}

public class BoundingVolume 
{
    public Bounds bounds;
    public List<Vector3> points;
    public List<BoundingVolume> children;
    private Vector3[] vector3s;

    public BoundingVolume(List<Vector3> m_points, Vector3[] corners)
    {
        points = m_points;
        bounds = new Bounds((corners[0] + corners[1]) / 2, corners[1] - corners[0]);
        
    }

    public void Insert(BoundingVolume m_volume) 
    {
        if (children == null) children = new List<BoundingVolume>();

        children.Add(m_volume);
    }

    public BoundingVolume[] Traverse() 
    {
        return children.ToArray();
    }
}
