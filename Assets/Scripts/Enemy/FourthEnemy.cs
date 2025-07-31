using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Item;
using MoreMountains.Feedbacks;

public class FourthEnemy : PlatformEnemy
{
    private enum FourthEnemyStage
    {
        NORMAL, MADMAN
    }

    [Header("Fourth Enemy Settings")]
    [SerializeField] private float shieldHealth = 100f; // Shield health.
    [SerializeField] private float rushDelay = 1f; // Rush delay.
    [SerializeField] private float recoverCooldown = 2f; // Recover cooldown after rushing.
    [SerializeField] private float rushSpeed = 5f; // Rush speed.

    [Header("Madman Stage Settings")]
    [SerializeField] private float increaseDamagePercent = 50f; // Increase damages by X%.
    [Space(5)]
    [SerializeField] private float runSpeed = 5f; // Run speed.
    [SerializeField] private float stompDelay = 1f; // Delay before start stomping.
    [SerializeField] private float stompCooldown = 2f; // Cooldown after stomping.
    [Space(5)]
    [SerializeField] private bool showStompRange = false; // Determine to show Stomp range or not.
    [SerializeField] private Vector2 stompRange = Vector2.one; // Stomp range.
    [SerializeField] private Vector2 stompOffset = Vector2.zero; // Stomp offset.
    [SerializeField] private float stompDistance = 1f; // Stomp closest distance.

    private FourthEnemyStage enemyStage = FourthEnemyStage.NORMAL; // Stage of this enemy.
    private bool rushing = false; // Determine if enemy is rushing.
    private bool stomping = false; // Determine if enemy is stomping.
    private bool playerTookDamage = false; // Determine if player is already took damage.
    private float currentDelay; // Delay timer.
    private float currentCooldown; // Recover cooldown timer.
    private float currentShieldHealth; // Current shield health.

    public MMFeedbacks healthyShieldSFX;
    public MMFeedbacks damagedShieldSFX;
    public MMFeedbacks veryDamagedShieldSFX;
    private bool finishedTransform;

    protected override void Start()
    {
        base.Start();

        currentShieldHealth = shieldHealth;
        currentDelay = rushDelay;
        currentCooldown = recoverCooldown;
        finishedTransform = false;
    }

    protected override void Update()
    {
        base.Update();

        animator.SetBool("Moving", IsMoving());
    }

    protected override void Attack()
    {
        // If there's recover cooldown, make it stand still.
        if (currentCooldown > 0f)
        {
            rigidBody.velocity = Vector2.up * rigidBody.velocity.y;

            currentCooldown -= Time.fixedDeltaTime;
            return;
        }

        switch (enemyStage)
        {
            case FourthEnemyStage.NORMAL:

                if (!rushing && !animator.GetCurrentAnimatorStateInfo(0).IsName("Enemy4_Charging"))
                {
                    MoveDirectionUpdater();
                    DirectionUpdater();
                }

                // If player is out of Attack detection, back to Chase state.
                if (!rushing && !IsPlayerInAttackRange() && currentCooldown <= 0f)
                {
                    currentAttackCooldown = attackCooldown;
                    currentDelay = rushDelay;

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

                    animator.Play("Enemy4_Idle");
                    enemyState = EnemyState.CHASE;
                    return;
                }

                if (currentDelay > 0f)
                {
                    animator.Play("Enemy4_Charging");
                    currentDelay -= Time.fixedDeltaTime;
                }
                else
                {
                    animator.Play("Enemy4_Charge");

                    rushing = true;

                    // Check if there's still a platform ahead and clear, keep rushing.
                    if (HasPlatformAhead() && !HasWallAhead() && !HasWallAheadCollider())
                    {
                        rigidBody.velocity = Vector2.right * moveX * rushSpeed;

                        // Check if player hasn't took damage yet, took damage during rush.
                        if (!playerTookDamage)
                        {
                            foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, 0.25f))
                            {
                                Player player;

                                if (collider.TryGetComponent(out player))
                                {
                                    player.TakeDamage(new Damage(attackDamage.physicalDamage, attackDamage.magicDamage), gameObject);
                                    playerTookDamage = true;

                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        rigidBody.velocity = Vector2.up * rigidBody.velocity.y;
                        currentDelay = rushDelay;
                        currentCooldown = recoverCooldown;

                        playerTookDamage = false;
                        rushing = false;

                        animator.Play("Enemy4_Idle");
                    }
                }

                break;

            case FourthEnemyStage.MADMAN:

                if (!finishedTransform) return;

                aggroDetection = attackAggroDetection;
                aggroOffset = attackAggroOffset;

                if (!stomping)
                {
                    MoveDirectionUpdater();
                    DirectionUpdater();
                }

                // If player is out of Attack detection, back to Chase state.
                if (!stomping && !IsPlayerInAttackRange() && currentCooldown <= 0f)
                {
                    currentAttackCooldown = attackCooldown;
                    currentDelay = stompDelay;

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

                    animator.Play("Enemy4_Madman_Walking");
                    enemyState = EnemyState.CHASE;
                    return;
                }
            
                if (!stomping)
                {
                    rigidBody.velocity = new Vector2(moveX * runSpeed, rigidBody.velocity.y);

                    float distance = Mathf.Abs(transform.position.x - player.transform.position.x);

                    if (distance < stompDistance)
                    {
                        rigidBody.velocity = Vector2.zero;
                        stomping = true;

                        animator.Play("Enemy4_Madman_Smashing");
                    }
                }

                break;
        }
    }

    // Function when enemy took damages.
    public override void TakeDamage(Damage damage, GameObject causeObject)
    {
        // If enemy look toward player, nulltify damages.
        if (enemyStage == FourthEnemyStage.NORMAL)
        {
            // If enemy got hit by Spiritual bullet while having Shield, cancel.
            if (causeObject.GetComponent<Bullet>() != null)
            {
                Instantiate(util.particles.protectionShield, transform.position, Quaternion.identity);
                return;
            }

            bool hitShield = false;

            if (moveX > 0f && transform.position.x - 0.95f < player.transform.position.x)
            {
                hitShield = true;
            }

            if (moveX < 0f && transform.position.x + 0.95f > player.transform.position.x)
            {
                hitShield = true;
            }

            if (hitShield)
            {
                PlayShieldHitSFX();

                rigidBody.velocity = Vector2.right * rigidBody.velocity.x;
                currentShieldHealth -= damage.physicalDamage;
                
                // If shield broke, turn to Madman stage.
                if (currentShieldHealth <= 0f)
                {
                    animator.Play("Enemy4_Transform");

                    // Reset Health.
                    SetStat("Health", GetBaseStat("Health"));

                    // Increase Damages by 50%.
                    float extraPhysicalDamage = (increaseDamagePercent * attackDamage.physicalDamage) / 100f;
                    IncreaseStatUnclamp("Extra Physical Damage", extraPhysicalDamage);

                    float extraMagicDamage = (increaseDamagePercent * attackDamage.magicDamage) / 100f;
                    IncreaseStatUnclamp("Extra Magic Damage", extraMagicDamage);

                    enemyStage = FourthEnemyStage.MADMAN;
                }
                else
                {
                    animator.Play("Enemy4_TakeDamage");
                }

                return;
            }

            animator.Play("Enemy4_TakeDamage");
        }
        else
        {
            if (!stomping && finishedTransform)
            {
                animator.Play("Enemy4_Madman_TakeDamage");
            }
        }

        base.TakeDamage(damage, causeObject);
    }

    // Function to determine if there's a wall in front of collider.
    private bool HasWallAheadCollider()
    {
        Collider2D collider = GetComponent<Collider2D>();

        Vector2 direction = Vector2.right * moveX;
        Vector2 center = (Vector2)transform.position + collider.offset + Vector2.up * Mathf.Abs(collider.bounds.size.y / 2f);
        RaycastHit2D wallHit = Physics2D.Raycast(center, direction, detectionOffset.x, platformLayer);
        return wallHit.collider != null;
    }

    // Function to execute Stomp during MadMan stage.
    public void Stomp()
    {
        Vector2 offset = (Vector2)transform.position + stompOffset;
        Vector2 position1 = offset + (stompRange / 2f);
        Vector2 position2 = offset - (stompRange / 2f);

        foreach (Collider2D collider in Physics2D.OverlapAreaAll(position1, position2))
        {
            Player player;

            if (collider.TryGetComponent(out player))
            {
                Damage damage = attackDamage;
                damage.physicalDamage += GetStat("Extra Physical Damage");
                damage.magicDamage += GetStat("Extra Magic Damage");

                player.TakeDamage(damage, gameObject);
                break;
            }
        }

        Instantiate(util.particles.stompEffect, offset, Quaternion.identity);
        // SpriteDebugger.instance.CreateRectangle(offset, stompRange, 1f);

        currentDelay = stompDelay;
        currentCooldown = stompCooldown;
    }

    // Function to stop Stomp action.
    public void ResetStomp()
    {
        stomping = false;
    }

    public void PlayShieldHitSFX()
    {
        if (shieldHealth == currentShieldHealth)
        {
            healthyShieldSFX.PlayFeedbacks();
        }
        else if (currentShieldHealth > 0.3f * shieldHealth)
        {
            damagedShieldSFX.PlayFeedbacks();
        }
        else
        {
            veryDamagedShieldSFX.PlayFeedbacks();
        }
    }

    public void EndOfTransform()
    {
        finishedTransform = true;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (showStompRange)
        {
            Vector2 offset = (Vector2)transform.position + stompOffset;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(offset, stompRange);
        }
    }
}
