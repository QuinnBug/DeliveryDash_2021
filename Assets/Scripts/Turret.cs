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
    Rigidbody rb;

    Vector3 aimingInput;

    public float turnSpeed;

    private GameObject projectile;

    // Start is called before the first frame update
    void Start()
    {
        parent = transform.parent;
        rb = GetComponentInParent<Rigidbody>();
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
            projectile = Instantiate(projectilePrefab, transform.TransformPoint(projectileSpawnpoint), Quaternion.identity);

            Rigidbody proj_rb;
            if (projectile.TryGetComponent(out proj_rb))
            {
                Vector3 force = transform.TransformDirection(projectileForceDirection) * projectileForcePower;
                force.y = projectileForceDirection.y * projectileForcePower;
                proj_rb.AddForce(force);
            }
        }

        if (context.phase == InputActionPhase.Canceled)
        {
            //Destroy(projectile);
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
