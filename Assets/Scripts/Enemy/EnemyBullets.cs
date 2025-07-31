using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullets : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed; // Speed of bullet.
    public float maxFlyTime = 5f; // Duration of bullet before it destroy itself.
    public int damage; // Damage of bullet.

    private bool hasBounced; // Determine if bullet got bounced or not.
    private Vector2 directionalVector; // Direction of bullet.
    private Vector2 unitVector; // Initial direction of bullet.

    private Rigidbody2D rigidBody;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        rigidBody.velocity = unitVector * speed;
        Destroy(gameObject, maxFlyTime);
    }

    // Function to set bullet's direction,
    public void SetDirection(Vector2 vec)
    {
        // If it hasn't been bounced yet, set direction to certain vector.
        if (!hasBounced)
        {
            directionalVector = vec;
            unitVector = directionalVector.normalized; 
        }
    }

    // Function to set bullet's speed.
    public void SetSpeed(float speed)
    {
        this.speed = speed;
    }

    // Function to get direction vector.
    public Vector2 GetDirentionalVector()
    {
        return directionalVector;
    }

    // Function to set bullet has been bounced.
    public void SetHasBounced()
    {
        hasBounced = true;
    }

    // Function to set bullet's damage.
    public void SetDamage(int damage)
    {
        this.damage = damage;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // If bullet has been bounced, enemy can took damages.
        if (other.CompareTag("Enemy") && hasBounced)
        {
            other.GetComponent<Enemy>().TakeDamage(new Damage(damage, 0f), gameObject);
            Destroy(gameObject);
        } 

        if (!other.CompareTag("EnemyProjectiles") && !other.CompareTag("Bullet") && !other.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }

}
