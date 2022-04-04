using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Car_Movement : MonoBehaviour
{
    public float moveSpeed;
    public float turnSpeed;
    public Transform body;
    private SuspensionSystem suspension;

    Rigidbody rb;
    Vector3 movementInput;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        suspension = GetComponent<SuspensionSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        if (movementInput.sqrMagnitude >= 0.1f)
        {
            Movement();
            Rotation();
        }
    }

    public void Movement() 
    {
        foreach (Wheel wheel in suspension.wheels)
        {
            if (wheel.grounded)
            {
                rb.AddForce((movementInput * moveSpeed) / suspension.wheels.Length);
            }
        }

        
    }

    public void Rotation() 
    {
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 flattenedVelocity = rb.velocity.normalized;
            flattenedVelocity.y = 0;
            transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.LookRotation(flattenedVelocity, Vector3.up), turnSpeed * Time.deltaTime);
        }
    }

    public void MovementInput(InputAction.CallbackContext context) 
    {
        Vector2 _input = context.ReadValue<Vector2>();

        movementInput.x = _input.x;
        movementInput.z = _input.y;
    }
}
