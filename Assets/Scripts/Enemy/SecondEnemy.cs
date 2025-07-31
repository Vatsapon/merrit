using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

public class SecondEnemy : PlatformEnemy
{
    [Header("Second Enemy Settings")]
    [SerializeField] private bool showFleeRange = false; // Determine to show Flee range or not.
    [Space(5)]
    [SerializeField] private float fleeRange = 5f; // Range to flee away.
    [SerializeField] private float teleportDistance = 5f; // Distance to teleport away.
    [SerializeField] private float teleportBackwardDistance = 5f; // Distance to teleport away in backward.
    [SerializeField] private float teleportCooldown = 2f; // Teleport cooldown.
    [SerializeField] private float colliderDelay = 1f; // Delay before move collision back to enemy.

    [Header("Projectile Settings")]
    [SerializeField] private bool showProjectileSpawnOffset = false; // Determine to show projectile spawn offset or not.
    [Space(5)]
    [SerializeField] private Vector2 projectileSpawnOffset; // Projectile spawn offset.
    [SerializeField] private float projectileSpeed = 2f; // Projectile speed.
    [SerializeField] private float projectileDecay = 3f; // Projectile decay time.
    [SerializeField] private float projectileTurnSpeed = 3f; // Projectile turn speed.

    [Header("Components")]
    [SerializeField] private SpriteRenderer teleportSprite; // Teleportation fade sprite.
    [SerializeField] private Animator teleportAnimator; // Teleportation Animator.
    [SerializeField] private GameObject projectilePrefab; // Projectile prefab.

    private bool colliderTeleport = false; // Determine if collider is showing.
    private float currentTeleportCooldown; // Teleport cooldown timer.
    private float currentColliderDelay; // Collider delay timer.
    private Vector2 savedColliderPosition; // Saved collider position.

    [Header("Feedbacks")]
    [SerializeField] private MMFeedbacks teleportSFX;


    protected override void Patrol()
    {
        animator.SetBool("Moving", IsMoving());

        base.Patrol();
    }

    protected override void Attack()
    {
        animator.SetBool("Moving", false);

        MoveDirectionUpdater();
        DirectionUpdater();

        teleportSprite.flipX = spriteRenderer.flipX;
        teleportSprite.enabled = colliderTeleport;

        // Make sure enemy won't bounce up.
        if (rigidBody.velocity.y > 0f)
        {
            rigidBody.velocity = Vector2.zero;
        }

        rigidBody.velocity = Vector2.up * rigidBody.velocity.y;

        aggroDetection = attackAggroDetection;
        aggroOffset = attackAggroOffset;

        currentTeleportCooldown -= Time.fixedDeltaTime;
        currentTeleportCooldown = Mathf.Clamp(currentTeleportCooldown, 0f, teleportCooldown);

        currentColliderDelay -= Time.fixedDeltaTime;
        currentColliderDelay = Mathf.Clamp(currentColliderDelay, 0f, colliderDelay);

        
        // If player is out of Attack detection, back to Chase state.
        if (!IsPlayerInAttackRange())
        {
            currentAttackCooldown = attackCooldown;
            currentTeleportCooldown = teleportCooldown;

            spriteRenderer.gameObject.transform.localPosition = Vector2.zero;

            aggroDetection = savedDefaultAggroDetection;
            aggroOffset = savedDefaultAggroOffset;

            // Detecting new ground position since enemy might fall to different platform level.
            RaycastHit2D groundHit = Physics2D.Raycast(transform.position, Vector2.down, 100f, platformLayer);

            if (groundHit.collider != null)
            {
                Vector2 groundOffset = Vector2.up * GetComponent<Collider2D>().bounds.size / 2f;
                ResetPatrol(groundHit.point + groundOffset);
            }
            else
            {
                ResetPatrol(transform.position);
            }

            enemyState = EnemyState.CHASE;
            return;
        }

        // If player just teleport, show collider.
        if (colliderTeleport)
        {
            CollisionUpdater();
        }
        
        // If it's not teleporting. keep shooting projectile.
        if (!colliderTeleport)
        {
            // Teleporting (Flee away)
            if (currentTeleportCooldown <= 0f)
            {
                foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, fleeRange))
                {
                    if (collider.gameObject.Equals(player.gameObject))
                    {
                        if (savedColliderPosition != Vector2.zero)
                        {
                            transform.position = savedColliderPosition;
                            spriteRenderer.gameObject.transform.localPosition = Vector2.zero;
                        }

                        Teleport();
                        return;
                    }
                }
            }

            // Shooting Projectile.
            if (currentAttackDelay <= 0f)
            {
                animator.Play("Enemy2_Attack");
            }
        }
    }

    protected override void MoveDirectionUpdater()
    {
        Vector2 position = teleportSprite.transform.position;

        // Check if enemy is teleporting, make it focus on teleport sprite.
        if (colliderTeleport)
        {
            position = spriteRenderer.transform.position;
        }

        float distance = Vector2.Distance(player.transform.position, position);

        // If it's close enough, return to not make sprite flipping around player.
        if (distance < 0.5f)
        {
            return;
        }

        if (player.transform.position.x < position.x)
        {
            moveX = -1;
        }
        else
        {
            moveX = 1;
        }
    }

    public override void TakeDamage(Damage damage, GameObject causeObject)
    {
        teleportSprite.enabled = false;
        animator.Play("Enemy2_TakeDamage");

        base.TakeDamage(damage, causeObject);
    }

    // Function to handle Teleportation.
    public void Teleport()
    {
        teleportSFX.PlayFeedbacks();

        currentTeleportCooldown = teleportCooldown;
        currentColliderDelay = colliderDelay;

        savedColliderPosition = transform.position;

        Vector2 offset = Vector2.zero;

        // Offset Teleport.
        if (transform.position.x < player.transform.position.x)
        {
            offset = Vector2.left * teleportDistance;
        }
        else
        {
            offset = Vector2.right * teleportDistance;
        }

        bool foundWall = false;

        // Check if there's a wall at teleport position.
        foreach (Collider2D collider in Physics2D.OverlapCircleAll((Vector2) transform.position + offset, 0.25f, platformLayer))
        {
            if (!collider.isTrigger)
            {
                foundWall = true;
                break;
            }
        }

        // If there's no wall, teleport.
        if (!foundWall)
        {
            animator.Play("Enemy2_Teleport");

            RaycastHit2D groundHit = Physics2D.Raycast((Vector2)transform.position + offset, Vector2.down, 100f, platformLayer);

            if (groundHit.collider != null)
            {
                float y = groundHit.point.y + GetComponent<Collider2D>().bounds.size.y / 2f;
                savedColliderPosition = new Vector2(transform.position.x + offset.x, y);
            }
            else
            {
                savedColliderPosition = (Vector2)transform.position + offset;
            }
            
            currentColliderDelay = colliderDelay;
            colliderTeleport = true;

            ignoreHitRecover = false;
            return;
        }

        // If there's wall, teleport backward instead.
        offset *= -Vector2.right;
        offset = offset.normalized * teleportBackwardDistance;

        foundWall = false;

        // Check if there's a wall at teleport position.
        foreach (Collider2D collider in Physics2D.OverlapCircleAll((Vector2)transform.position + offset, 0.25f, platformLayer))
        {
            if (!collider.isTrigger)
            {
                foundWall = true;
                break;
            }
        }

        // If there's no wall, teleport. or else, don't teleport.
        if (!foundWall)
        {
            animator.Play("Enemy2_Teleport");

            RaycastHit2D groundHit = Physics2D.Raycast((Vector2) transform.position + offset, Vector2.down, 100f, platformLayer);

            if (groundHit.collider != null)
            {
                float y = groundHit.point.y + GetComponent<Collider2D>().bounds.size.y / 2f;
                savedColliderPosition = new Vector2(transform.position.x + offset.x, y);
            }
            else
            {
                savedColliderPosition = (Vector2)transform.position + offset;
            }

            currentColliderDelay = colliderDelay;
            colliderTeleport = true;
        }
    }

    // Function to shoot projectile.
    public void Shoot()
    {
        Vector2 spawnPosition = (Vector2)transform.position + projectileSpawnOffset;

        if (spriteRenderer.flipX)
        {
            spawnPosition = (Vector2)transform.position + new Vector2(-projectileSpawnOffset.x, projectileSpawnOffset.y);
        }

        Vector2 direction = player.transform.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        GameObject projectileObject = Instantiate(projectilePrefab, spawnPosition, rotation);
        SecondEnemyProjectile projectile = projectileObject.GetComponent<SecondEnemyProjectile>();

        projectile.projectileDamage = attackDamage;
        projectile.projectileSpeed = projectileSpeed;
        projectile.projectileDecay = projectileDecay;
        projectile.projectileTurnSpeed = projectileTurnSpeed;

        currentAttackDelay = attackDelay;
    }

    // Function to handle Collider position.
    private void CollisionUpdater()
    {
        ignoreHitRecover = currentColliderDelay > 0f;

        if (currentColliderDelay > 0f)
        {
            GameObject spriteObject = spriteRenderer.gameObject;
            float offsetX = savedColliderPosition.x - transform.position.x;
            float offsetY = savedColliderPosition.y - transform.position.y;
            spriteObject.transform.localPosition = new Vector2(offsetX, offsetY);
        }
        else
        {
            colliderTeleport = false;
            transform.position = savedColliderPosition;
            spriteRenderer.gameObject.transform.localPosition = Vector2.zero;

            savedColliderPosition = Vector2.zero;
        }
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (showFleeRange)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, fleeRange);
        }

        if (showProjectileSpawnOffset)
        {
            Vector2 spawnPosition = (Vector2)transform.position + projectileSpawnOffset;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPosition, 0.25f);

            spawnPosition = (Vector2)transform.position + new Vector2(-projectileSpawnOffset.x, projectileSpawnOffset.y);
            Gizmos.DrawWireSphere(spawnPosition, 0.25f);
        }
    }
}
