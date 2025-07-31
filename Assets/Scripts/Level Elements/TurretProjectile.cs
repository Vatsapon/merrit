using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretProjectile : Entity
{
    [Header("Projectile Settings")]
    public Damage projectileDamage; // Projectile damages.
    public float projectileTurnSpeed = 5f; // Projectile turn speed.
    public float projectileSpeed = 3f; // Projectile speed.

    private Player player;

    protected override void Start()
    {
        base.Start();

        rigidBody = GetComponent<Rigidbody2D>();
        player = Player.instance;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        Vector2 direction = player.transform.position - transform.position;
        float turnAmount = Vector3.Cross(direction, transform.up).z;

        rigidBody.angularVelocity = -turnAmount * projectileTurnSpeed;
        rigidBody.velocity = transform.up * projectileSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Player player;

        if (collision.gameObject.TryGetComponent(out player))
        {
            player.TakeDamage(projectileDamage, gameObject);
            Destroy(gameObject);
        }

        Entity entity;

        if (collision.gameObject.TryGetComponent(out entity))
        {
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), entity.GetComponent<Collider2D>());
            return;
        }

        Destroy(gameObject);
    }
}
