using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Turret : MonoBehaviour
{
    Transform parent;

    Vector3 aimingInput;

    public float turnSpeed;

    // Start is called before the first frame update
    void Start()
    {
        parent = transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
        Rotation();
    }

    public void Rotation() 
    {
        //parent pos + localPos + (parentRotation with 0 y * aimingInput)
        Vector3 flatPR = parent.rotation.eulerAngles;
        flatPR.y = 0;
        Vector3 targetPos = parent.position + transform.localPosition + (Quaternion.Euler(flatPR) * aimingInput);
        Debug.DrawLine(transform.position, targetPos);
        Quaternion targetRot = Quaternion.LookRotation(transform.position - targetPos, parent.up);
        Vector3 rot = Quaternion.Lerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime).eulerAngles;
        rot.x = 0;
        rot.z = 0;
        transform.localRotation = Quaternion.Euler(rot);
        //transform.LookAt(targetPos, parent.up);
        //transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
    }

    public void Fire(InputAction.CallbackContext context) 
    {

    }

    public void AimingInput(InputAction.CallbackContext context) 
    {
        Vector2 input = context.ReadValue<Vector2>();
        
        if (input != Vector2.zero)
        {
            aimingInput.x = input.x;
            aimingInput.z = input.y;
        }
    }
}
