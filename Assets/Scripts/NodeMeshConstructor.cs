using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeMeshConstructor : MonoBehaviour
{
    LNode_Manager nodeManager;

    public float cornerDistance;

    public List<Vector3> points = null;

    // Start is called before the first frame update
    void Start()
    {
        nodeManager = LNode_Manager.Instance;
        points = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (nodeManager.nodeGenDone && points == null)
        {
            GetAllPoints();
        }
    }

    void GetAllPoints() 
    {
        points = new List<Vector3>();

        foreach (Node item in nodeManager.nodes)
        {
            points.AddRange(GetNodeCorners(item));
        }
        Debug.Log("Mesh Points Done");
    }

    Vector3[] GetNodeCorners(Node node) 
    {
        Vector3[] points = new Vector3[4];

        Quaternion rotation = Quaternion.LookRotation(node.forward, Vector3.up);

        int i = 0;
        for (int x = -1; x <= 1; x+=2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                points[i] = node.point + (rotation * (new Vector3(x, 0, z) * cornerDistance));
                i++;
            }
        }

        return points;
    }

    private void OnDrawGizmosSelected()
    {
        if (points != null)
        {
            foreach (Vector3 item in points)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawCube(item, Vector3.one);
            }
        }
    }
}
