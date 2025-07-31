using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdEnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float colliderDetectionSize = 1f; // Collider detection to check if projectile is inside a wall.
    public Damage projectileDamage; // Projectile damages.
    public float projectileAreaRange = 5f; // Projectile range.
    [SerializeField] private float destroyDelay = 5f; // Destroy delay after projectile spawned.

    [Header("References")]
    [SerializeField] private GameObject explosionParticle; // Explosion particle.

    [Header("Debugging")]
    [SerializeField] private bool showColliderDetection = false; // Determine to show Collider detection or not.
    [SerializeField] private bool showAreaRange = false; // Determine to show Area attack range or not.

    private Rigidbody2D rigidBody;
    private Collider2D colliders;

    private Player player;

    private void Start()
    {
        player = Player.instance;

        rigidBody = GetComponent<Rigidbody2D>();
        colliders = GetComponent<Collider2D>();

        colliders.isTrigger = IsInCollider() || rigidBody.velocity.y > 0f;

        Destroy(gameObject, destroyDelay);
    }

    private void Update()
    {
        // If projectile is inside collision, keep checking until went outside a wall and turn back to collision mode.
        if (rigidBody.velocity.y > 0f)
        {
            colliders.isTrigger = true;
        }
        else
        {
            colliders.isTrigger = IsInCollider();
        }
    }

    // Function to check if projectile is inside collider.
    private bool IsInCollider()
    {
        bool inCollision = false;

        // If the projectile is above player, make it unable to detect.
        if (transform.position.y > player.transform.position.y && transform.position.y - player.transform.position.y > 1f)
        {
            return true;
        }

        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, colliderDetectionSize))
        {
            if (collider.gameObject.Equals(player.gameObject))
            {
                inCollision = false;
                break;
            }

            if (!collider.gameObject.Equals(gameObject) && !collider.isTrigger)
            {
                inCollision = true;
                break;
            }
        }

        return inCollision;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // SpriteDebugger.instance.CreateCircle(transform.position, projectileAreaRange, 1f);

        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, projectileAreaRange))
        {
            Player player;

            if (collider.TryGetComponent(out player))
            {
                player.TakeDamage(projectileDamage, gameObject);
            }
        }

        Instantiate(explosionParticle, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (showColliderDetection)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, colliderDetectionSize);
        }

        if (showAreaRange)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, projectileAreaRange);
        }
    }
}
