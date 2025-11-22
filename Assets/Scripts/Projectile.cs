using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 250f;
    [SerializeField] float lifetime = 3f;
    [SerializeField] float staggerDuration = 0f;

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
        hit.takeDamage(damage, owner);
        Destroy(gameObject);
        return;
    }

    public void initialise(Vector2 direction, Fighter newOwner, float newDamage)
    {
        transform.position = newOwner.transform.position;
        // set direction for visuals

        // https://discussions.unity.com/t/lookat-2d-equivalent/88118
        transform.up = (Vector3)direction;

        owner = newOwner;
        GetComponent<Rigidbody2D>().AddForce(direction * speed);
        damage = newDamage;
        StartCoroutine(destroyInTime(lifetime));
    }

    IEnumerator destroyInTime(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(gameObject);
    }
}
