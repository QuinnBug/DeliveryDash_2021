using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Turret : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Vector3 projectileSpawnpoint;
    public Vector3 projectileForceDirection;
    public float projectileForcePower;

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
        Vector3 targetPos = transform.position + parent.InverseTransformDirection(aimingInput);
        Vector3 diff = targetPos - transform.position;
        diff.Normalize();
        float rot_y = Mathf.Atan2(diff.x, diff.z) * Mathf.Rad2Deg;
        Quaternion desiredRot = Quaternion.Euler(0f, rot_y, 0);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, desiredRot, turnSpeed * Time.deltaTime);
    }

    public void Fire(InputAction.CallbackContext context) 
    {
        if (context.phase == InputActionPhase.Started) 
        {
            GameObject proj = Instantiate(projectilePrefab, transform.TransformPoint(projectileSpawnpoint), Quaternion.identity);

            Rigidbody proj_rb;
            if (proj.TryGetComponent(out proj_rb))
            {
                proj_rb.AddForce(transform.TransformDirection(projectileForceDirection) * projectileForcePower);
            }
            Destroy(proj, 10);
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.grey;
        Gizmos.DrawSphere(transform.TransformPoint(projectileSpawnpoint), 0.1f);
    }
}
