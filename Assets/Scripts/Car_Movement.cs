using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car_Movement : MonoBehaviour
{
    public float moveSpeed;
    public float turnSpeed;
    public Transform body;

    Rigidbody rb;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.A)) 
        {
            movement.x -= moveSpeed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            movement.x += moveSpeed;
        }
        if (Input.GetKey(KeyCode.W))
        {
            movement.z += moveSpeed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            movement.z -= moveSpeed;
        }

        rb.AddForce(movement);
        if (movement.sqrMagnitude > 0)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(movement, Vector3.up), turnSpeed);
        }
    }
}
