using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

public class GorgoEgg : MonoBehaviour
{
    [Header("Gorgo's Egg Settings")]
    [SerializeField] private bool showAimRadius = false; // Determine to show Aim radius or not.
    [Space(5)]
    [SerializeField] private float aimRadius = 2f; // Aim radius.
    [SerializeField] private float aimDuration = 2f; // Aim duration before locked the aim.
    [SerializeField] private float aimDelay = 1f; // Aim delay before start dashing.
    [SerializeField] private float dashSpeed = 5f; // Dash speed.
    [Space(10)]
    [SerializeField] private Damage attackDamage; // Attack damages.
    [SerializeField] private float stunDuration = 2f; // Stun duration when it hits player.

    private bool dashing = false; // Determine if it's dashing.
    private float currentAimDuration; // Aim duration timer.
    private float currentAimDelay; // Aim delay timer.
    private Vector3 spawnPosition;

    private Player player;
    private Rigidbody2D rigidBody;

    public MMFeedbacks aimSFX, shootSFX;
    bool shootHasPlayed = false;

    private void Start()
    {
        player = Player.instance;

        rigidBody = GetComponent<Rigidbody2D>();

        currentAimDuration = aimDuration;
        currentAimDelay = aimDelay;
        spawnPosition = transform.position;
//        Debug.Log(spawnPosition);
    }

    private void FixedUpdate()
    {
        // Check if it's aiming or dashing.
        if (!dashing)
        {
            // If player is within aim radius while in Spiritual form, start aiming.
            if (IsPlayerInRadius() && player.isSpirit)
            {
                if (!aimSFX.IsPlaying)
                    aimSFX.PlayFeedbacks();

                Vector2 direction = player.transform.position - transform.position;
                transform.up = direction;

                currentAimDuration -= Time.fixedDeltaTime;

                // If it aim long enough, start dashing.
                if (currentAimDuration <= 0f)
                {
                    dashing = true;
                    return;
                }
            }
            else
            {
                currentAimDuration = aimDuration;
                if (aimSFX.IsPlaying)
                    aimSFX.StopFeedbacks();
            }
        }
        else
        {
            currentAimDelay -= Time.fixedDeltaTime;

            // If delay reaches 0, dashing toward direction.
            if (currentAimDelay <= 0f)
            {
                rigidBody.velocity = transform.up.normalized * dashSpeed;

                if (!shootHasPlayed)
                {
                    shootSFX.PlayFeedbacks();
                    shootHasPlayed = true;
                }

                return;
            }
        }

        rigidBody.velocity = Vector2.zero;
    }

    // Function to determine if player is within Aim radius.
    private bool IsPlayerInRadius()
    {
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, aimRadius))
        {
            Player player;

            if (collider.TryGetComponent(out player))
            {
                Vector2 direction = player.transform.position - transform.position;
                float distance = Vector2.Distance(transform.position, player.transform.position);
                RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, LayerMask.GetMask("Ground"));

                // Check if there's a wall blocked enemy's sight, return.
                if (hit.collider != null)
                {
                    return false;
                }

                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        if (showAimRadius)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, aimRadius);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Player player;

        if (collision.gameObject.TryGetComponent(out player) && player.isSpirit)
        {
            player.TakeDamage(attackDamage, gameObject);
            player.AddEffect("Stun", stunDuration);
            Destroy(gameObject);
        }

        Entity entity;

        if (collision.gameObject.TryGetComponent(out entity))
        {
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), entity.GetComponent<Collider2D>());
            return;
        }

        Destroy(gameObject);
    }

    public Vector3 GetSpawnPos() {
        return spawnPosition;
    }

}
