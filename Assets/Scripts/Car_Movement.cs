using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Car_Movement : MonoBehaviour
{
    public float moveSpeed;
    public float turnSpeed;

    private SuspensionSystem suspension;

    internal Rigidbody rb;
    Vector3 movementInput;
    bool active = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        suspension = GetComponent<SuspensionSystem>();
        Event_Manager.Instance._OnBuildingsGenerated.AddListener(Activate);
    }

    public void Activate() 
    {
        if (rb != null) 
        {
            rb.useGravity = true;
        }

        active = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!active) return;

        if (movementInput.sqrMagnitude >= 0.1f) 
        {
            Movement();
        }

        if (rb.velocity.sqrMagnitude >= 0.1f)
        {
            Rotation();
        }
    }

    public void Movement() 
    {
        foreach (Wheel wheel in suspension.wheels)
        {
            if (wheel.grounded)
            {
                //rb.AddForce((transform.forward * moveSpeed) / suspension.wheels.Length);
                rb.AddForce((movementInput * moveSpeed) / suspension.wheels.Length);
            }
        }

        
    }

    public void Rotation() 
    {
        if (movementInput.sqrMagnitude > 0.1f)
        {
            if (suspension.GroundedPercent() > 0f)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.LookRotation(movementInput, Vector3.up), turnSpeed * Time.deltaTime);
            }
            else
            {
                //rb.AddTorque(movementInput * turnSpeed);
            }
        }
    }

    public void MovementInput(InputAction.CallbackContext context) 
    {
        Vector2 _input = context.ReadValue<Vector2>();

        movementInput.x = _input.x;
        movementInput.z = _input.y;
    }
}
