using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTestBot : Enemy
{
    [Header("State System")]
    public bool stationary = true; // Determine if enemy bot is Stationary.
    public float detectPlayerRange = 5f;
    public float attackPlayerRange = 1f;

    private Vector2 defaultPosition; // Default position.

    protected override void Start()
    {
        base.Start();

        defaultPosition = transform.position;
    }

    protected override void Update()
    {
        base.Update();

        float dist = Vector2.Distance(player.transform.position, transform.position); //putting it out here incase we need this for something else
        StateChanger(dist);

        switch (enemyState)
        {
            case EnemyState.PATROL:
            Vector2 dir = (Vector3)defaultPosition - transform.position;
            float distanceToTarget = defaultPosition.x - transform.position.x;

            if (Mathf.Abs(distanceToTarget) > 0.5f)
            {
                rigidBody.velocity = (dir * Vector2.right).normalized * moveSpeed + (Vector2.up * rigidBody.velocity.y);
            }
            break;

            case EnemyState.CHASE:
            Vector2 dir2 = player.transform.position - transform.position;
            float distanceToTarget2 = player.transform.position.x - transform.position.x;

            if (Mathf.Abs(distanceToTarget2) > attackPlayerRange)
            {
                rigidBody.velocity = (dir2 * Vector2.right).normalized * moveSpeed + (Vector2.up * rigidBody.velocity.y);
            }
            break;

            case EnemyState.ATTACK:
            rigidBody.velocity = Vector2.zero;
            break;
        }
    }

    void StateChanger(float dist)
    {
        if (dist <= detectPlayerRange && dist > attackPlayerRange && !stationary)
        {
            enemyState = EnemyState.CHASE;
        }
        else
        {
            if (dist <= attackPlayerRange)
            {
                enemyState = EnemyState.ATTACK;
            }
            else
            {
                enemyState = EnemyState.PATROL;
            }
        }
    }
}
