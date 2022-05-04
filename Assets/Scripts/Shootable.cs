using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shootable : MonoBehaviour
{
    public float health;
    public int armor;

    public virtual void Update()
    {
        armor = Mathf.Clamp(armor, -200, 100);

        if (health <= 0)
        {
            Die();
        }
    }

    public virtual void Die() 
    {
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        DamageProjectile proj;
        if (collision.gameObject.TryGetComponent(out proj)) 
        {
            if (armor != 0)
            {
                health -= proj.damage * (1 - (armor / 100.0f));
            }
            else 
            {
                health -= proj.damage;
            }
            Destroy(proj.gameObject);
        }
    }
}
