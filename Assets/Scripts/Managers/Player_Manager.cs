using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player_Manager : Singleton<Player_Manager>
{
    public PlayerStats stats;

    internal bool active = false;
    internal Rigidbody rb;
    internal Car_Movement movement;
    internal Turret turret;
    [Space]
    public UiPointer arrow = new UiPointer();

    public void Start()
    {
        Event_Manager.Instance._OnBuildingsGenerated.AddListener(Activate);

        arrow.transform = transform;
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<Car_Movement>();
        turret = GetComponentInChildren<Turret>();

        //any loading of stats needs to happen before assigning the starting values
        stats.Init();
    }

    void Update()
    {
        //turret updates
        turret.active = active;
        turret.ammo = stats.currentAmmo;
        turret.fireDelay = 1 / stats.shotsPerSecond;

        //movement updates
        movement.active = active;
        movement.moveSpeed = stats.moveSpeed;
        movement.turnSpeed = stats.turnSpeed;
        movement.fuel = stats.currentFuel;
        movement.fuelDrain = stats.fuelDrain;

        //arrow update
        arrow.Update(active);
    }

    public void Activate()
    {
        active = true;
    }

    
}

[System.Serializable]
public class UiPointer 
{
    public float turnSpeed;
    public float moveSpeed;
    public Image pointer;
    public Range xScreenRange, yScreenRange;
    public float pointerDisplayRange;
    public Color fullColour;
    public Color emptyColour;

    private Vector2 pointerPosition = Vector2.zero;
    internal Transform transform;

    public void Update(bool active)
    {
        pointer.enabled = CheckPointerState();

        if (active && Delivery_Manager.Instance.targets.Count > 0)
        {
            Rotation();
            Position();
            pointer.color = Color.Lerp(fullColour, emptyColour, pointerDisplayRange / Vector3.Distance(Vector3.zero, pointerPosition) );
        }
    }

    void Rotation()
    {
        Vector3 targetPos = Delivery_Manager.Instance.targets[0].meshCenter;
        Vector3 diff = targetPos - transform.position;
        diff.y = 0;
        diff.Normalize();
        float rot_z = Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg;
        Quaternion desiredRot = Quaternion.Euler(0, 0, rot_z - 90);
        pointer.transform.rotation = Quaternion.Lerp(pointer.transform.rotation, desiredRot, turnSpeed * Time.deltaTime);
    }

    void Position()
    {
        Vector3 targetPos = Delivery_Manager.Instance.targets[0].meshCenter;
        Vector3 diff = (targetPos - transform.position);
        Vector2 flatDiff = new Vector2(Mathf.Clamp(diff.x, xScreenRange.min, xScreenRange.max), Mathf.Clamp(diff.z, yScreenRange.min, yScreenRange.max));
        pointer.transform.localPosition = Vector3.Lerp(pointer.transform.localPosition, flatDiff, moveSpeed * Time.deltaTime);
        pointerPosition = pointer.transform.localPosition;
    }

    bool CheckPointerState()
    {
        if (Delivery_Manager.Instance.targets.Count == 0) return false;

        return true;
    }
}

[System.Serializable]
public struct Range
{
    public float min;
    public float max;
}
