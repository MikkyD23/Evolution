using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    const float SPEED = 250f;
    const float LIFETIME = 3f;

    Fighter owner;
    float damage = 0;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.layer == Fighter.FIGHTER_LAYER)
        {
            hitFighter(collision.transform.gameObject.GetComponent<Fighter>());
        }
        else if (collision.transform != null)
        {
            // hit obstacle (TODO colliding with other bullets)
            Destroy(gameObject);
            return;
        }
    }

    void hitFighter(Fighter hit)
    {
        if (hit == owner)
        {
            return;
        }
        hit.takeDamage(damage);
        owner.reportDealtDamage(damage);
        Destroy(gameObject);
        return;
    }

    public void initialise(Vector2 direction, Fighter newOwner, float newDamage)
    {
        owner = newOwner;
        GetComponent<Rigidbody2D>().AddForce(direction * SPEED);
        damage = newDamage;
        StartCoroutine(destroyInTime(LIFETIME));
    }

    IEnumerator destroyInTime(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(gameObject);
    }
}
