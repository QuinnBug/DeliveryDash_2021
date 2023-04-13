using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed;
    public float turnSpeed;

    internal Rigidbody rb;
    internal GameObject head;
    Vector3 movementInput;
    Vector3 rotationInput;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (movementInput.sqrMagnitude >= 0.1f)
        {
            Movement();
        }

        if (movementInput.sqrMagnitude >= 0.1f)
        {
            Rotation();
        }
    }

    public void Movement()
    {
        transform.Translate(movementInput * moveSpeed);
    }

    public void Rotation() 
    {
        transform.Rotate(Vector3.up, rotationInput.y);
        head.transform.Rotate(Vector3.right, rotationInput.x);
    }

    public void MovementInput(InputAction.CallbackContext context)
    {
        Vector2 _input = context.ReadValue<Vector2>();

        movementInput.x = _input.x;
        movementInput.z = _input.y;
    }

    public void RotationInput(InputAction.CallbackContext context)
    {
        Vector2 _input = context.ReadValue<Vector2>();

        rotationInput.y = _input.x;
        rotationInput.x = _input.y;
    }
}
