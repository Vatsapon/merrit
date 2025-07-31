using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondEnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public Damage projectileDamage; // Projectile damages.
    public float projectileSpeed = 2f; // Projectile speed.
    public float projectileDecay = 3f; // Projectile decay time.
    public float projectileTurnSpeed = 3f; // Projectile turn speed.
    [SerializeField] private float minSize = 0.25f; // Minimum size before destroy itself.

    private float currentDecayTime; // Decay timer.

    private Rigidbody2D rigidBody; // Projectile's Rigidbody.
    private Player player;

    private void Start()
    {
        player = Player.instance;

        rigidBody = GetComponent<Rigidbody2D>();
        currentDecayTime = transform.localScale.x;
    }

    private void FixedUpdate()
    {
        transform.localScale = Vector3.one * currentDecayTime;
        currentDecayTime -= Time.fixedDeltaTime * 0.1f * projectileDecay;

        // If it reaches minimum size, destroy.
        if (currentDecayTime <= minSize)
        {
            Destroy(gameObject);
        }

        Vector2 direction = player.transform.position - transform.position;
        float turnAmount = Vector3.Cross(direction, transform.up).z;

        rigidBody.angularVelocity = -turnAmount * projectileTurnSpeed;
        rigidBody.velocity = transform.up * projectileSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player;

        if (collision.TryGetComponent(out player))
        {
            player.TakeDamage(projectileDamage, gameObject);
        }

        Destroy(gameObject);
    }
}
