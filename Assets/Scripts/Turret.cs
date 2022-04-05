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
        if (aimingInput.sqrMagnitude > 0.1f)
        {
            Vector3 flattenedDir = parent.InverseTransformDirection(aimingInput);
            Quaternion lookRot = Quaternion.LookRotation(flattenedDir, Vector3.up);
            transform.localRotation = Quaternion.Lerp(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
        }
    }

    public void Fire(InputAction.CallbackContext context) 
    {

    }

    public void AimingInput(InputAction.CallbackContext context) 
    {
        Vector2 input = context.ReadValue<Vector2>();

        aimingInput.x = input.x;
        aimingInput.z = input.y;
    }
}
