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
        Movement();
        Rotation();
    }

    public void Movement() 
    {
        if (movementInput.sqrMagnitude < 0.1f)
        {
            return;
        }

        int wheels = 0;
        foreach (Wheel wheel in suspension.wheels)
        {
            if (wheel.grounded) wheels++;
        }

        if (wheels >= 2)
        {
            rb.AddForce(transform.forward * moveSpeed);
        }

    }

    public void Rotation() 
    {
        Vector3 flatMoveDir = movementInput;
        flatMoveDir.y = 0;

        if (flatMoveDir.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LerpUnclamped(transform.rotation,
                Quaternion.LookRotation(flatMoveDir, Vector3.up), turnSpeed * Time.deltaTime);
        }
    }

    public void MovementInput(InputAction.CallbackContext context) 
    {
        Vector2 _input = context.ReadValue<Vector2>();

        movementInput.x = _input.x;
        movementInput.z = _input.y;
    }
}
