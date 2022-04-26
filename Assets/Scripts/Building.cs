using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    public Material material;
    public Renderer rend;
    bool isTarget = false;
    internal Road road;
    public Vector3 bottomLeft, topRight;
    public Direction roadSide;
    Vector3 deliveryCenter;
    Vector3 extents;

    public void Start()
    {
        deliveryCenter = Vector3.Lerp(bottomLeft, topRight, 0.5f) + (transform.right * (roadSide == Direction.RIGHT ? -1 : 1) * Building_Manager.Instance.buildingDepth);
        extents = topRight - bottomLeft;

        rend = GetComponent<Renderer>();
        material = rend.material;
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

    private void OnDrawGizmos()
    {
        if (deliveryCenter != null && isTarget)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(bottomLeft, topRight);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(deliveryCenter, deliveryCenter + extents);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(deliveryCenter, deliveryCenter - extents);
        }
    }
}
