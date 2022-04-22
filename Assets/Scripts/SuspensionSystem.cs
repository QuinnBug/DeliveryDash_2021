using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Wheel 
{
    internal Vector3 position;
    internal bool grounded = false;
    internal Vector3 groundPos;

    public Vector3 offset;
    public SuspensionSettings settings;

    private float minLength;
    private float maxLength;
    private float lastLength;
    private float springLength;
    private float springVelocity;
    private float springForce;
    private float damperForce;

    public void UpdatePosition(Transform _tf) 
    {
        position = _tf.position + (_tf.rotation * offset);
    }

    public bool UpdateGrounded(Vector3 _dir)
    {
        minLength = settings.restLength - settings.springTravel;
        maxLength = settings.restLength + settings.springTravel;

        RaycastHit hit;
        LayerMask mask = 1 << LayerMask.NameToLayer("Ground");
        mask |= (1 << LayerMask.NameToLayer("Road"));

        if (Physics.Raycast(position, _dir, out hit, maxLength + settings.wheelRadius, mask))
        {
            grounded = true;
            groundPos = hit.point;
        }
        else
        {
            grounded = false;
            groundPos = position + (_dir * 100);
        }

        return grounded;
    }

    public Vector3 GetForce(Vector3 _dir) 
    {
        minLength = settings.restLength - settings.springTravel;
        maxLength = settings.restLength + settings.springTravel;
        lastLength = springLength;

        springLength = Vector3.Distance(position, groundPos) - settings.wheelRadius;
        springLength = Mathf.Clamp(springLength, minLength, maxLength);
        springVelocity = (lastLength - springLength) / Time.fixedDeltaTime;

        springForce = settings.springStiffness * (settings.restLength - springLength);
        damperForce = settings.damperStiffness * springVelocity;

        return (springForce + damperForce) * _dir;
    }
}

public class SuspensionSystem : MonoBehaviour
{
    private Rigidbody rb;

    public Wheel[] wheels;
    public SuspensionSettings baseSettings;

    // Start is called before the first frame update
    void Start()
    {
        rb = transform.root.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        foreach (Wheel wheel in wheels)
        {
            wheel.settings = baseSettings;
            wheel.UpdatePosition(transform);
            if (wheel.UpdateGrounded(-transform.up)) 
            {
                rb.AddForceAtPosition(wheel.GetForce(transform.up), wheel.position);
            }
        }
    }

    public float GroundedPercent() 
    {
        int groundedI = 0;
        foreach (Wheel wheel in wheels)
        {
            if (wheel.grounded)
            {
                groundedI++;
            }
        }

        return groundedI / wheels.Length;
    }

    private void OnDrawGizmos()
    {
        foreach (Wheel wheel in wheels)
        {
            wheel.settings = baseSettings;
            wheel.UpdatePosition(transform);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(wheel.position, wheel.settings.wheelRadius);
            if (wheel.grounded) 
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(wheel.groundPos, wheel.settings.wheelRadius);
            }
        }
    }

    //https://www.youtube.com/watch?v=x0LUiE0dxP0
}

[System.Serializable]
public struct SuspensionSettings 
{
    public float restLength;
    public float springTravel;
    public float springStiffness;
    public float damperStiffness;
    public float wheelRadius;
}
