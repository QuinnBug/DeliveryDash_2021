using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Turret : MonoBehaviour
{
    Transform parent;

    Vector3 aimingInput;

    // Start is called before the first frame update
    void Start()
    {
        parent = transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Rotation() 
    {

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
