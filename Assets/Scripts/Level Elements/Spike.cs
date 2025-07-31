using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spike : MonoBehaviour
{
    private enum SpikeState
    {
        WAITING, SHOW, HIDE
    }

    [Header("Spike Settings")]
    [SerializeField] private Damage attackDamage; // Attack damages.
    [SerializeField] private Vector2 knockbackForce; // Knockback force.

    /*
    [Space(5)]
    [SerializeField] private bool hideOnStart = false; // Determine either to hide or show on start.
    [Space(5)]
    [SerializeField] private float showDelay = 1f; // Delay before show spike again.
    [SerializeField] private float showDuration = 1f; // Spike show duration.
    [SerializeField] private float showSpeed = 2f; // Spike show up speed.
    [SerializeField] private float hideSpeed = 1f; // Spike hide speed.
    [SerializeField] private float hideHeight = 1f; // Hide height. (Default = 1 unit)

    private SpikeState spikeState = SpikeState.WAITING; // Spike state.
    private Vector2 savedPosition = Vector2.zero; // Saved hide position.
    private float currentShowDelay; // Show delay timer.
    private float currentShowDuration; // Show duration timer.
    
    private Rigidbody2D rigidBody;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();

        /*
        savedPosition = (Vector2)transform.position + Vector2.down * hideHeight;
        currentShowDelay = showDelay;
        currentShowDuration = showDuration;

        if (!hideOnStart)
        {
            spikeState = SpikeState.SHOW;
        }
        else
        {
            transform.position = savedPosition;
        }
    }

    private void FixedUpdate()
    {
        switch (spikeState)
        {
            case SpikeState.WAITING:

            currentShowDelay -= Time.fixedDeltaTime;
            currentShowDelay = Mathf.Clamp(currentShowDelay, 0f, showDelay);

            if (currentShowDelay <= 0f)
            {
                spikeState = SpikeState.SHOW;
            }

            break;

            case SpikeState.SHOW:

            float offset = Mathf.Abs((savedPosition.y + hideHeight) - transform.position.y);

            if (offset <= 0.15f)
            {
                transform.position = savedPosition + (Vector2.up * hideHeight);
                rigidBody.velocity = Vector2.zero;

                currentShowDuration -= Time.fixedDeltaTime;
                currentShowDuration = Mathf.Clamp(currentShowDuration, 0f, showDuration);

                if (currentShowDuration <= 0f)
                {
                    spikeState = SpikeState.HIDE;
                }
            }
            else
            {
                rigidBody.velocity = Vector2.up * showSpeed;
            }

            break;

            case SpikeState.HIDE:

            float hideDistance = Mathf.Abs(transform.position.y - savedPosition.y);

            if (hideDistance <= 0.15f)
            {
                rigidBody.velocity = Vector2.zero;
                transform.position = savedPosition;

                currentShowDelay = showDelay;
                currentShowDuration = showDuration;

                spikeState = SpikeState.WAITING;
            }
            else
            {
                rigidBody.velocity = Vector2.down * hideSpeed;
            }

            break;
        }
    }
    */

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player;

        if (collision.TryGetComponent(out player))
        {
            player.TakeDamage(attackDamage, gameObject);
            player.rigidBody.velocity = Vector2.zero;

            if (knockbackForce != Vector2.zero)
            {
                player.Knockback(transform.position, knockbackForce);
            }
        }
    }
}
