using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    public float speed;
    public float rotSpeed;

    Rigidbody rb;
    float movement;
    Vector3 rotation;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        movement = 0;
        rotation = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) movement += 1;
        if (Input.GetKey(KeyCode.S)) movement += -1;
        if (Input.GetKey(KeyCode.D)) rotation.y += 1;
        if (Input.GetKey(KeyCode.A)) rotation.y += -1;

        if (Input.GetKeyDown(KeyCode.Space)) rb.useGravity = !rb.useGravity;

        rb.AddForce(transform.forward * movement * speed);
        transform.Rotate(rotation * rotSpeed);
    }
}
