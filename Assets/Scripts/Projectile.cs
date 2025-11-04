using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    const float SPEED = 150f;
    const float LIFETIME = 3f;

    Fighter owner;
    float damage = 0;
    float lifetimeRemaining = LIFETIME;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Fighter collidingFighter = collision.transform.gameObject.GetComponent<Fighter>();
        if(collidingFighter == owner)
        {
            return;
        }
        if (collidingFighter != null)
        {
            collidingFighter.takeDamage(damage);
            owner.reportDealtDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (collision.transform != null)
        {
            // hit obstacle
            Destroy(gameObject);
            return;
        }
    }

    public void initialise(Vector2 direction, Fighter newOwner, float newDamage)
    {
        owner = newOwner;
        //transform.LookAt((Vector2)transform.position + direction);
        GetComponent<Rigidbody2D>().AddForce(direction * SPEED);
        damage = newDamage;
    }

    private void Update()
    {
        //transform.position += (transform.up * SPEED * Time.deltaTime);
        lifetimeRemaining -= Time.deltaTime;
        if(lifetimeRemaining <= 0)
        {
            Destroy(gameObject);
        }
    }
}
