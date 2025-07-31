using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : Entity
{
    [Header("Turret Settings")]
    [SerializeField] private bool showDetectionRadius = false; // Determine to show Detection radius or not.
    [Space(10)]
    [SerializeField] private float detectionRadius = 5f; // Detection radius.
    [SerializeField] private float shootDelay = 1f; // Delay for shooting projectile.

    [Space(10)]
    [SerializeField] private Damage projectileDamage; // Projectile damages.
    [SerializeField] private float projectileTurnSpeed = 20f; // Projectile Turn speed.
    [SerializeField] private float projectileSpeed = 3f; // Projectile speed.
    [SerializeField] private GameObject projectilePrefab; // Projectile prefab.

    private float currentShootDelay; // Shoot delay timer.

    protected override void Start()
    {
        base.Start();

        currentShootDelay = shootDelay;
    }

    protected override void Update()
    {
        base.Update();

        bool playerInRadius = false;

        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, detectionRadius))
        {
            Player player;

            if (collider.TryGetComponent(out player))
            {
                // If out of delay, start shooting projectile.
                if (currentShootDelay <= 0f)
                {
                    Vector2 direction = player.transform.position - transform.position;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                    Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

                    GameObject projectileObject = Instantiate(projectilePrefab, transform.position, rotation);
                    TurretProjectile projectile = projectileObject.GetComponent<TurretProjectile>();
                    projectile.projectileDamage = projectileDamage;
                    projectile.projectileSpeed = projectileSpeed;
                    projectile.projectileTurnSpeed = projectileTurnSpeed;

                    currentShootDelay = shootDelay;
                }

                playerInRadius = true;
                break;
            }
        }

        // If player is out of radius, reset Shoot delay.
        if (!playerInRadius)
        {
            currentShootDelay = shootDelay;
        }

        currentShootDelay -= Time.deltaTime;
        currentShootDelay = Mathf.Clamp(currentShootDelay, 0f, shootDelay);
    }

    protected override void Die()
    {
        // Check if entity is already dead, don't execute further.
        if (entityState == EntityState.DEAD)
        {
            return;
        }

        entityState = EntityState.DEAD;
        Destroy(gameObject);
    }
    private void OnDrawGizmos()
    {
        if (showDetectionRadius)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
