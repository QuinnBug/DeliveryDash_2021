using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Wheel 
{
    internal Vector3 position;
    internal bool grounded;
    internal Vector3 groundPos;

    public Vector3 offset;
    public float restLength;
    public float springTravel;
    public float springStiffness;
    public float damperStiffness;
    public float wheelRadius;
    public float gravForce;

    private float minLength;
    private float maxLength;
    private float lastLength;
    private float springLength;
    private float springVelocity;
    private float springForce;
    private float damperForce;

    public void UpdatePosition(Vector3 _pos) 
    {
        position = _pos + offset;
    }

    public bool UpdateGrounded(Vector3 _dir)
    {
        minLength = restLength - springTravel;
        maxLength = restLength + springTravel;

        RaycastHit hit;
        LayerMask mask = 1 << LayerMask.NameToLayer("Ground");
        if (Physics.Raycast(position, _dir, out hit, maxLength + wheelRadius, mask))
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
        lastLength = springLength;

        springLength = Vector3.Distance(position, groundPos) - wheelRadius;
        springLength = Mathf.Clamp(springLength, minLength, maxLength);
        springVelocity = (lastLength - springLength) / Time.fixedDeltaTime;

        springForce = springStiffness * (restLength - springLength);
        damperForce = damperStiffness * springVelocity;

        return (springForce + damperForce) * _dir;
    }
}

public class SuspensionSystem : MonoBehaviour
{
    private Rigidbody rb;

    public Wheel[] wheels;

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
            wheel.UpdatePosition(transform.position);
            if (wheel.UpdateGrounded(-transform.up)) 
            {
                rb.AddForceAtPosition(wheel.GetForce(transform.up), wheel.groundPos);
            }
        }
    }

    private void OnDrawGizmos()
    {
        foreach (Wheel wheel in wheels)
        {
            wheel.UpdatePosition(transform.position);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(wheel.position, wheel.wheelRadius);
        }
    }

    //https://www.youtube.com/watch?v=x0LUiE0dxP0
}
