using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

public class ThirdEnemy : Enemy
{
    [Header("Third Enemy Settings")]
    [SerializeField] private float flyTurnSpeed = 5; // Turn speed.

    [Header("Physical Attack Settings")]
    [SerializeField] private float followDelay = 0.5f; // Delay before follow after throwing projectile.
    [SerializeField] private float shootDelay = 0.5f; // Delay before shoot projectile.
    [SerializeField] private float projectileThrowForce = 5f; // Projectile force to throw up.
    [SerializeField] private float projectileAreaRange = 5f; // Projectile range.

    [Header("Spiritual Attack Settings")]
    public Damage explosionDamage; // Explosion damages.
    [SerializeField] private float dashSpeed = 5f; // Dash speed when chasing player as Spiritual form.
    [SerializeField] private float closeRange = 1f; // Range before start explode.
    [SerializeField] private float explosionRadius = 5f; // Explosion radius.

    [Header("Components")]
    [SerializeField] private GameObject projectilePrefab; // Projectile to throw.

    [Header("Debugging")]
    [SerializeField] private bool showDirection = false; // Determine to show direction or not.
    [SerializeField] private bool showExplosionRadius = false; // Determine to show Explosion radius or not.

    private Vector2 direction = Vector2.down; // Direction that enemy is moving toward to.
    private Vector2 targetOffset; // Position that offset from Attack area.
    private Vector2 targetPosition; // Position that enemy will move toward to.
    private float currentShootDelay; // Shoot delay timer.
    private float currentFollowDelay; // Follow delay timer.
    private bool exploding = false; // Determine if enemy is exploding.
    private bool shooting = false; // Determine if enemy is shooting.
    private bool isSpirit = false; // Check if player is in Spiritual form when approaches to Attack detection.

    public MMFeedbacks awakenSFX, preExplodeSFX, flyingSFX, shootingSFX, sleepingSFX, impactExplosionSFX;

    protected override void Start()
    {
        base.Start();

        targetPosition = (Vector2) transform.position + Vector2.down;
    }

    protected override void Update()
    {
        base.Update();

        DirectionUpdater();
        animator.SetBool("Spirit", isSpirit);
    }

    // Function that execute during Chase state.
    protected override void Chase()
    {
        MoveDirectionUpdater();
        base.DirectionUpdater();

        // If player is in Aggro area, go to Attack state.
        if (IsPlayerInAggro())
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Enemy3_Sleeping"))
            {
                animator.Play("Enemy3_Awaken");
            }
            
            rigidBody.velocity = Vector2.up * rigidBody.velocity.y;
        }
    }
    
    // Function to start attack after awaken.
    public void StartAttack()
    {
        currentAttackDelay = 0f;
        ChangeState(EnemyState.ATTACK);

        enemyState = EnemyState.ATTACK;
    }

    // Function that execute during Attack state.
    protected override void Attack()
    {
        if (!animator.GetBool("Spirit") && isSpirit)
        {
            animator.Play("Enemy3_Tracking");
        }

        // Separate Attack behavior between Physical and Spiritual form.
        if (isSpirit)
        {
            // Spiritual Attack.
            // If enemy is Exploding.
            if (exploding)
            {
                animator.Play("Enemy3_Explode");
                rigidBody.velocity = Vector2.zero;
                return;
            }

            targetPosition = player.transform.position;

            float distance = Vector2.Distance(transform.position, player.transform.position);

            // If close enough, start Exploding.
            if (distance <= closeRange)
            {
                exploding = true;
            }
            else
            {
                rigidBody.velocity = direction * dashSpeed;

                // Clamp speed between 0 and maxMoveSpeed.
                if (rigidBody.velocity.magnitude > dashSpeed)
                {
                    rigidBody.velocity = new Vector2(Mathf.Sign(rigidBody.velocity.x), Mathf.Cos(rigidBody.velocity.y)) * dashSpeed;
                }
            }
        }
        else
        {
            isSpirit = player.isSpirit;

            // If enemy is shooting, drop projectile.
            if (shooting && currentAttackDelay <= 0f)
            {
                if (currentShootDelay > 0f)
                {
                    currentShootDelay -= Time.fixedDeltaTime;
                }
                else
                {
                    animator.Play("Enemy3_Shooting");
                }

                return;
            }

            if (currentFollowDelay <= 0f)
            {
                // Physical Attack.
                targetPosition = GetTargetPosition();

                float distance = Vector2.Distance(transform.position, targetPosition);

                // If close enough, start Drop Projectile.
                if (distance <= 1f)
                {
                    rigidBody.velocity = Vector2.zero;
                    shooting = true;
                    return;
                }
                else
                {
                    rigidBody.velocity = direction * moveSpeed;

                    // Clamp speed between 0 and maxMoveSpeed.
                    if (rigidBody.velocity.magnitude > moveSpeed)
                    {
                        rigidBody.velocity = new Vector2(Mathf.Sign(rigidBody.velocity.x), Mathf.Cos(rigidBody.velocity.y)) * moveSpeed;
                    }
                }
            }
        }

        currentFollowDelay -= Time.fixedDeltaTime;
        currentFollowDelay = Mathf.Clamp(currentFollowDelay, 0f, followDelay);
    }

    // Function that execute when change states.
    protected override void ChangeState(EnemyState toState)
    {
        if (enemyState == EnemyState.PATROL && toState == EnemyState.CHASE)
        {
            isSpirit = player.isSpirit;
            
            // Check if player isn't in Spiritual form, set target position.
            if (!isSpirit)
            {
                targetOffset = GetRandomPosition();
                targetPosition = GetTargetPosition();
            }
            else
            {
                targetPosition = player.transform.position;
            }
        }
    }

    public override void TakeDamage(Damage damage, GameObject causeObject)
    {
        animator.Play("Enemy3_TakeDamage");

        base.TakeDamage(damage, causeObject);
    }

    // Function that execute when enemy shoot a projectile.
    public void Shoot()
    {
        currentShootDelay = shootDelay;
        currentAttackDelay = attackDelay;
        currentFollowDelay = followDelay;
        shooting = false;

        targetOffset = GetRandomPosition();
        targetPosition = GetTargetPosition();

        direction = targetPosition - (Vector2)transform.position;

        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        projectile.GetComponent<Rigidbody2D>().AddForce(Vector2.up * projectileThrowForce * 100f);
        projectile.GetComponent<ThirdEnemyProjectile>().projectileDamage = attackDamage;
        projectile.GetComponent<ThirdEnemyProjectile>().projectileAreaRange = projectileAreaRange;
    }

    // Function when enemy explode.
    public void Explode()
    {
        // SpriteDebugger.instance.CreateCircle(transform.position, explosionRadius, 1f);

        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, explosionRadius))
        {
            Player player;

            if (collider.TryGetComponent(out player))
            {
                player.TakeDamage(explosionDamage, gameObject);
            }
        }

        Instantiate(util.particles.projectileExplosion, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
    
    // Function to update direction.
    private new void DirectionUpdater()
    {
        Vector2 lookDirection = (targetPosition - (Vector2)transform.position).normalized;
        direction = Vector2.Lerp(direction.normalized, lookDirection, flyTurnSpeed * Time.deltaTime);
    }

    // Function to get random position in Attack detection.
    private Vector2 GetRandomPosition()
    {
        float randomX = Random.Range(-attackDetection.x, attackDetection.x);
        float randomY = Random.Range(-attackDetection.y, attackDetection.y);

        return new Vector2(randomX / 2f, randomY / 2f);
    }

    // Function to get target position in World space.
    private Vector2 GetTargetPosition()
    {
        Vector2 offset = (Vector2) player.transform.position + attackOffset;
        return offset + targetOffset;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (player)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(GetTargetPosition(), 1f);
        }

        if (showAttackDetection && player)
        {
            Vector2 aggroCenter = (Vector2)transform.position + aggroOffset;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(aggroCenter, aggroDetection);

            Vector2 attackCenter = (Vector2)player.transform.position + attackOffset;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(attackCenter, attackDetection);
        }

        if (showDirection)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction);
        }

        if (showExplosionRadius)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }

    public void PlayAwakenSFX()
    {
        awakenSFX.PlayFeedbacks();
    }
    public void PlayExplodeSFX()
    {
        preExplodeSFX.PlayFeedbacks();
    }
    public void PlayFlyingSFX()
    {
        flyingSFX.PlayFeedbacks();
    }
    public void PlayShootingSFX()
    {
        shootingSFX.PlayFeedbacks();
    }
    public void PlaySleepingSFX()
    {
        sleepingSFX.PlayFeedbacks();
    }

    public void PlayImpactExplosionSFX()
    {
        impactExplosionSFX.PlayFeedbacks();
    }
}
