using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Manager : Singleton
{
    public Transform target;
    public Transform focus;
    public float moveSpeed;
    public float forwardOffset;

    // Update is called once per frame
    void Update()
    {
        focus.position = Vector3.Lerp(focus.position,
            target.position + (target.forward * forwardOffset),
            moveSpeed * Time.deltaTime);
    }
}
