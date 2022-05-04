using QuinnMeshes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Building : MonoBehaviour
{
    public Material material;
    public Renderer rend;
    bool isTarget = false;
    internal Road road;

    Vector3 deliveryCenter;
    public Vector3 meshCenter;
    Vector3 extents;

    public void Init(Vector3[] points, Road parent, Material _mat)
    {
        road = parent;

        float height = Random.Range(5, 10);
        Vector3 up = Vector3.up * height;

        qMesh _qMesh = new qMesh();

        //side walls
        for (int i = 0; i < points.Length; i++)
        {
            int j = i + 1;
            if (j >= points.Length)
            {
                j = 0;
            }

            Vertex[] _v = new Vertex[] { 
                new Vertex(points[i], new Vector2(0, 0)),
                new Vertex(points[j], new Vector2(1, 0)),
                new Vertex(points[i] + up, new Vector2(0, 1)),
                new Vertex(points[j] + up, new Vector2(1, 1)),
            };

            Triangle tri1 = new Triangle();
            tri1.vertices = new Vertex[3] { _v[0], _v[2], _v[3] };
            _qMesh.triangles.Add(tri1);

            Triangle tri2 = new Triangle();
            tri2.vertices = new Vertex[3] { _v[0], _v[3], _v[1] };
            _qMesh.triangles.Add(tri2);
        }

        Vector3 centerpoint = Vector3.zero;
        foreach (Vector3 point in points)
        {
            centerpoint += point;
        }
        centerpoint /= points.Length;
        //top and bottom
        for (int i = 0; i < points.Length; i++)
        {
            int j = i + 1;
            if (j >= points.Length)
            {
                j = 0;
            }

            Vertex[] _v = new Vertex[] {
                new Vertex(points[i], new Vector2(0, 0)),
                new Vertex(points[j], new Vector2(0, 0)),
                new Vertex(centerpoint, new Vector2(0, 0)),
                new Vertex(points[i] + up, new Vector2(0, 0)),
                new Vertex(points[j] + up, new Vector2(0, 0)),
                new Vertex(centerpoint + up, new Vector2(0, 0))
            };

            Triangle tri1 = new Triangle();
            tri1.vertices = new Vertex[3] { _v[0], _v[1], _v[2] };
            _qMesh.triangles.Add(tri1);

            Triangle tri2 = new Triangle();
            tri2.vertices = new Vertex[3] { _v[5], _v[4], _v[3] };
            _qMesh.triangles.Add(tri2);
        }

        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        MeshCollider collider = gameObject.AddComponent<MeshCollider>();
        rend = gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = _qMesh.ConvertToMesh();

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        filter.sharedMesh = collider.sharedMesh = mesh;
        rend.material = _mat;
        material = _mat;

        meshCenter = mesh.bounds.center;
        
        //deliveryCenter = meshCenter + (transform.right * (roadSide == Direction.RIGHT ? -1 : 1) * Building_Manager.Instance.buildingDepth);
        //extents = topRight - bottomLeft;

    }

    public void Update()
    {
        if (isTarget) 
        {
            CheckDeliveryZone();
        }
    }

    private void CheckDeliveryZone()
    {
        Car_Movement player;
        Collider[] colliders = Physics.OverlapBox(deliveryCenter, new Vector3(1,20,1), transform.rotation);
        foreach (Collider collider in colliders) 
        {
            if (collider.TryGetComponent(out player))
            {
                CompleteDelivery();
                return;
            }
        }
    }

    public void SetAsTarget() 
    {
        if (isTarget) return;

        // this probably needs to be more sophisticated, some sort of actual highlighting system;

        rend.material = Delivery_Manager.Instance.targetMaterial;
        isTarget = true;
    }

    public void CompleteDelivery() 
    {
        if (!isTarget) return;

        rend.material = material;
        isTarget = false;
        Event_Manager.Instance._DeliveryMade.Invoke(this);
    }

    internal bool IsSameRoad(Building other)
    {
        return road == other.road;
    }

    private void OnDrawGizmosSelected()
    {
        if (deliveryCenter != null && isTarget)
        {
            //Gizmos.color = Color.blue;
            //Gizmos.DrawLine(bottomLeft, topRight);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(deliveryCenter, deliveryCenter + extents);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(deliveryCenter, deliveryCenter - extents);
        }
    }
}
