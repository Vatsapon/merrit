using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformEnemy : Enemy
{
    [Header("Patrol Behavior")]
    [SerializeField] private float walkRangeMin = 1f; // Minimum walk range.
    [SerializeField] private float walkRangeMax = 3f; // Maximum walk range.
    [SerializeField] private float stopDelay = 1f; // Stop delay before turn around.

    [Space(5)]
    [SerializeField] private bool showAttackAggro = false; // Determine to show detection debugging or not.
    [Space(5)]
    [SerializeField] protected Vector2 attackAggroDetection = Vector2.one; // Aggro detection during Attack state.
    [SerializeField] protected Vector2 attackAggroOffset = Vector2.zero; // Aggro offset during Attack state.

    [Header("Platform Detection")]
    [SerializeField] private bool showPlatformDetection = false; // Determine to show detection debugging or not.
    [Space(5)]
    [SerializeField] protected LayerMask platformLayer; // Platform Layer detection.
    [SerializeField] protected Vector2 detectionOffset = Vector2.one; // Detection offset away from enemy.

    protected Vector2 savedDefaultAggroOffset; // Saved Aggro Offset.
    protected Vector2 savedDefaultAggroDetection; // Saved Aggro Detection.

    private bool stopMove = false; // Determine if enemy is stop moving.
    private Vector2 defaultPosition; // Default position for enemy to move patrol around.
    private Vector2 targetPatrolPostion; // Target patrol position.
    private float currentStopDelay; // Stop Delay timer.

    protected override void Start()
    {
        base.Start();

        savedDefaultAggroOffset = aggroOffset;
        savedDefaultAggroDetection = aggroDetection;

        defaultPosition = transform.position;
        float randomX = Random.Range(walkRangeMin, walkRangeMax);
        bool randomRight = Random.value > 0.5f;

        // Set random direction.
        if (randomRight)
        {
            moveX = 1;
        }
        else
        {
            moveX = -1;
        }

        targetPatrolPostion = defaultPosition + Vector2.right * moveX * randomX;
    }

    protected override void Update()
    {
        base.Update();

        PlatformDetectionUpdater();

        if (enemyState == EnemyState.PATROL)
        {
            if (currentStopDelay > 0f)
            {
                currentStopDelay -= Time.deltaTime;
            }
            else
            {
                // If it stop moving right not, turn around and find new patrol position.
                if (stopMove)
                {
                    moveX *= -1;

                    float randomX = Random.Range(walkRangeMin, walkRangeMax);
                    targetPatrolPostion = defaultPosition + Vector2.right * moveX * randomX;
                    stopMove = false;
                }
            }
        }
    }

    // Function that execute during Patrol state.
    protected override void Patrol()
    {
        base.Patrol();

        if (!stopMove)
        {
            rigidBody.velocity = new Vector2(moveX * moveSpeed, rigidBody.velocity.y);

            // If enemy reach certain point, stop moving.
            float distance = Vector2.Distance(transform.position, targetPatrolPostion);

            if (distance <= 0.5f)
            {
                rigidBody.velocity = Vector2.up * rigidBody.velocity.y;
                currentStopDelay = stopDelay;
                stopMove = true;
            }
        }
    }

    // Function that execute during Chase state.
    protected override void Chase()
    {
        base.Chase();

        // If enemy reaches edge of platform, stop.
        if (!HasPlatformAhead() || HasWallAhead())
        {
            rigidBody.velocity = Vector2.up * rigidBody.velocity.y;
        }
        else
        {
            rigidBody.velocity = new Vector2(moveX * moveSpeed, rigidBody.velocity.y);
        }
    }

    // Function that execute when change states.
    protected override void ChangeState(EnemyState toState)
    {
        // CHASE -> PATROL
        if (enemyState == EnemyState.CHASE && toState == EnemyState.PATROL)
        {
            ResetPatrol(defaultPosition);
        }
    }

    // Function to detect platform ahead or wall in front of it.
    protected void PlatformDetectionUpdater()
    {
        // If there's wall ahead, switch direction.
        if (HasWallAhead() && !stopMove)
        {
            rigidBody.velocity = Vector2.up * rigidBody.velocity.y;
            currentStopDelay = stopDelay;
            stopMove = true;
            return;
        }

        // If there's no platform ahead, switch direction.
        if (!HasPlatformAhead() && !stopMove)
        {
            rigidBody.velocity = Vector2.up * rigidBody.velocity.y;
            currentStopDelay = stopDelay;
            stopMove = true;
        }
    }

    // Function to detect if there's a wall ahead.
    protected bool HasWallAhead()
    {
        Vector2 direction = Vector2.right * moveX;
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position, direction, detectionOffset.x, platformLayer);
        return wallHit.collider != null;
    }

    // Function to detect if enemy reaches edge of platform.
    protected bool HasPlatformAhead()
    {
        Vector2 offset = (Vector2)transform.position + (Vector2.right * moveX * detectionOffset.x) + (Vector2.down * detectionOffset.y);
        Collider2D[] platformCollider = Physics2D.OverlapCircleAll(offset, 0.5f, platformLayer);

        bool foundPlatform = false;

        foreach (Collider2D collider in platformCollider)
        {
            if (!collider.isTrigger)
            {
                foundPlatform = true;
                break;
            }
        }

        return foundPlatform;
    }

    // Function to reset patrol position (In case, enemy get into different platform)
    protected void ResetPatrol(Vector2 newDefaultPosition)
    {
        defaultPosition = newDefaultPosition;
        float randomX = Random.Range(walkRangeMin, walkRangeMax);
        targetPatrolPostion = defaultPosition + Vector2.right * moveX * randomX;

        currentStopDelay = 0f;
        stopMove = false;

        if (spriteRenderer.flipX)
        {
            moveX = -1;
        }
        else
        {
            moveX = 1;
        }
    }
    
    // Function to determine if enemy is moving.
    protected bool IsMoving()
    {
        return !stopMove;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (showPlatformDetection)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + (Vector2.right * moveX * detectionOffset));
            Gizmos.DrawLine(transform.position, (Vector2)transform.position - (Vector2.right * moveX * detectionOffset));

            Vector2 offsetRight = (Vector2)transform.position + (Vector2.right * moveX * detectionOffset.x) + (Vector2.down * detectionOffset.y);
            Vector2 offsetLeft = (Vector2)transform.position + (Vector2.right * -moveX * detectionOffset.x) + (Vector2.down * detectionOffset.y);
            Gizmos.DrawWireSphere(offsetRight, 0.5f);
            Gizmos.DrawWireSphere(offsetLeft, 0.5f);
        }

        if (showAttackAggro)
        {
            Vector2 aggroCenter = (Vector2)transform.position + attackAggroOffset;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(aggroCenter, attackAggroDetection);
        }
    }
}
