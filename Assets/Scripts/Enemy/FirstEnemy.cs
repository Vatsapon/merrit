using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;

public class FirstEnemy : PlatformEnemy
{
    [Header("First Enemy Settings")]
    [SerializeField] private float flySpeed = 5f; // Speed during flying up.
    [SerializeField] private float dashSpeed = 5f; // Speed during dash toward player.
    [SerializeField] private float dashDuration = 0.75f; // Dash duration.
    [SerializeField] private float dashDelay = 0.75f; // Cooldown before start dashing.
    [SerializeField] private float recoverCooldown = 1f; // Recover cooldown after dash.

    private bool dashing = false; // Determine if enemy is dashing.
    private bool playerTookDamage = false; // Check if player has already took damages from Dashing.
    private int savedDirection; // Saved direction to dash toward to.

    private float currentDashDuration; // Dash duration timer.
    private float currentDashDelay; // Dash delay timer.
    private float currentRecoverCooldown; // Recoevr cooldown timer.

    [Header("SFXs")]
    public MMFeedbacks wingFlappingSFX;
    //public MMFeedbacks attackingSFX;
    public MMFeedbacks chargingUpSFX;
    [Space(5)]
    //public AudioSource wingFlappingSource;
    public AudioSource attackingSource;
    public AudioSource rushingSource;

    protected override void Start()
    {
        base.Start();

        currentDashDuration = dashDuration;
        currentDashDelay = dashDelay;
    }

    protected override void Patrol()
    {
        animator.SetBool("Moving", IsMoving());

        base.Patrol();
    }

    // Function to handle Attack.
    protected override void Attack()
    {
        // If there's recover cooldown, make it stand still.
        if (currentRecoverCooldown > 0f)
        {
            rigidBody.velocity = Vector2.up * rigidBody.velocity.y;

            currentRecoverCooldown -= Time.fixedDeltaTime;
            return;
        }

        // While dashing, enemy won't change direction.
        if (!dashing)
        {
            MoveDirectionUpdater();
            DirectionUpdater();
        }
        else
        {
            animator.SetBool("Moving", false);
        }

        aggroDetection = attackAggroDetection;
        aggroOffset = attackAggroOffset;

        // If player is out of Attack detection, back to Chase state.
        if (!dashing && !IsPlayerInAttackRange() && currentRecoverCooldown <= 0f)
        {
            currentAttackCooldown = attackCooldown;
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

        if (currentAttackDelay <= 0f)
        {
            // If not Dashing, Flying up.
            if (!dashing)
            {
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Enemy1_Walking") || animator.GetCurrentAnimatorStateInfo(0).IsName("Enemy1_Idle"))
                {
                    if (transform.position.y < player.transform.position.y)
                    {
                        animator.Play("Enemy1_Preflying");
                    }
                    else
                    {
                        animator.Play("Enemy1_Precharging");
                    }
                }

                if (player.transform.position.y > transform.position.y)
                {
                    rigidBody.velocity = Vector2.up * flySpeed;
                }

                // If enemy is flying close enough. Start Dashing.
                if (transform.position.y - player.transform.position.y >= 0f)
                {
                    transform.position = new Vector2(transform.position.x, player.transform.position.y + 0.1f);
                    rigidBody.velocity = Vector2.zero;

                    dashing = true;
                    savedDirection = moveX;
                    playerTookDamage = false;

                    currentDashDelay = dashDelay;
                    currentDashDuration = dashDuration;
                }
            }
            else
            {
                Dashing();
                
            }
        }
    }

    // Function that handle Dashing.
    private void Dashing()
    {
        if (currentDashDelay > 0f)
        {
            currentDashDelay -= Time.fixedDeltaTime;
            rigidBody.constraints = RigidbodyConstraints2D.FreezeAll;

            if (!chargingUpSFX.IsPlaying)
            {
                chargingUpSFX.PlayFeedbacks();
            }
            return;
        }
        else
        {
            rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (!attackingSource.isPlaying)
                attackingSource.Play();

            if (!rushingSource.isPlaying)
                rushingSource.Play();
            /*
            if (!attackingSFX.IsPlaying)
            {
                attackingSFX.PlayFeedbacks();
            }
            */
        }

        if (currentDashDuration > 0f)
        {
            // Player took damage only once.
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

            animator.Play("Enemy1_Charge");

            rigidBody.gravityScale = 0f;
            rigidBody.velocity = Vector2.right * savedDirection * dashSpeed;

            currentDashDuration -= Time.fixedDeltaTime;
        }
        else
        {
            animator.Play("Enemy1_Walking");

            rigidBody.velocity = Vector2.zero;
            rigidBody.gravityScale = 1f;
            currentAttackDelay = attackDelay;
            currentRecoverCooldown = recoverCooldown;

            dashing = false;
        }
    }

    protected override void ChangeState(EnemyState toState)
    {
        if (toState == EnemyState.PATROL)
        {
            animator.Play("Enemy1_Walking");
        }

        base.ChangeState(toState);
    }

    public override void TakeDamage(Damage damage, GameObject causeObject)
    {
        // Check if enemy is currently dashing, return.
        if (dashing)
        {
            return;
        }

        animator.Play("Enemy1_TakeDamage");

        base.TakeDamage(damage, causeObject);
    }

    public override void Knockback(Vector2 basePosition, Vector2 knockbackForce)
    {
        // Check if enemy is currently dashing, return.
        if (dashing)
        {
            return;
        }

        base.Knockback(basePosition, knockbackForce);
    }

    public void PlayWingFlappingSFX()
    {
        wingFlappingSFX.PlayFeedbacks();
    }
}
