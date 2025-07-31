using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTestFly : Enemy
{
    [Header("Fly Setting")]
    public float flyRange = 4f; // Range to fly around position.
    public float patrolDelay = 1f; // Delay to change patrol position.
    public float rotateSpeed = 4f; // Rotate speed toward patrol position.

    private Vector2 direction; // direction to move toward.
    private Vector2 patrolPosition; // Position to patrol.
    private Vector2 startPosition; // Start position.
    private float currentPatrolDelay; // Patrol delay timer.

    [Header("Debugging")]
    public bool showRadius = false; // Debug to show radius.
    public bool showDirection = false; // Debug to show direction.

    protected override void Start()
    {
        base.Start();

        startPosition = transform.position;
        currentPatrolDelay = patrolDelay;

        SetPatrolPosition();
        direction = (patrolPosition - (Vector2)transform.position).normalized;
    }

    protected override void Update()
    {
        base.Update();

        Movement();
        
        // Patrol delay
        if (currentPatrolDelay > 0f)
        {
            currentPatrolDelay -= Time.deltaTime;
        }
        else
        {
            SetPatrolPosition();
            currentPatrolDelay = patrolDelay;
        }
    }

    // Function to control enemy's movement.
    private void Movement()
    {
        Vector2 lookDirection = (patrolPosition - (Vector2) transform.position).normalized;
        direction = Vector2.Lerp(direction.normalized, lookDirection, rotateSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, patrolPosition) > 0.5f)
        {
            rigidBody.velocity = direction * moveSpeed;
        }
    }

    // Function to set patrol position.
    private void SetPatrolPosition()
    {
        Vector2 newPosition = FindNewPosition();
        Vector2 direction = newPosition - (Vector2)transform.position;

        RaycastHit2D[] hitList = Physics2D.RaycastAll(transform.position, direction, flyRange);

        Vector2 hitPoint = Vector2.zero;

        foreach (RaycastHit2D hit in hitList)
        {
            if (!hit.collider.gameObject.Equals(gameObject) && !hit.collider.isTrigger)
            {
                hitPoint = hit.point;
                break;
            }
        }

        // Check if it hits something, use hit point as new position.
        if (hitPoint != Vector2.zero)
        {
            patrolPosition = hitPoint;
        }
        else
        {
            patrolPosition = newPosition;
        }
    }

    // Function to find new patrol position.
    private Vector2 FindNewPosition()
    {
        Vector2 randomUnit = Random.insideUnitCircle;

        float randomRange = Random.Range(0f, flyRange);
        randomUnit *= randomRange;

        Vector2 newPosition = startPosition + randomUnit;

        return newPosition;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (showRadius)
        {
            if (!Application.isPlaying)
            {
                startPosition = transform.position;
            }

            Gizmos.DrawWireSphere(startPosition, flyRange);
        }

        if (showDirection)
        {
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + direction);
        }
    }
}
