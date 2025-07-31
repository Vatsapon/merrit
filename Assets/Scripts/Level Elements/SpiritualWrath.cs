using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritualWrath : MonoBehaviour
{
    [Header("Spiritual Wrath Settings")]
    [SerializeField] private float triggerRadius = 1f; // Trigger radius to explode.
    [SerializeField] private float explosionDelay = 1f; // Delay before start exploding.
    [SerializeField] private float explosionRadius = 1f; // Explosion radius after player walk too close.

    [Header("Debugging")]
    [SerializeField] private bool showTriggerRadius = false; // Determine to show Trigger radius or not.
    [SerializeField] private bool showExplosionRadius = false; // Determine to show Explosion radius or not.

    private bool exploding = false; // Determine if trap is exploding.
    private float currentExplosionDelay; // Explosion delay timer.

    private Player player;

    private void Start()
    {
        player = Player.instance;

        currentExplosionDelay = explosionDelay;
    }

    private void Update()
    {
        // If not exploding, keep checking distance.
        if (!exploding)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);

            if (distance <= triggerRadius)
            {
                exploding = true;
            }
        }
        else
        {
            currentExplosionDelay -= Time.deltaTime;

            // If reaches 0, Explode within explosionRadius.
            if (currentExplosionDelay <= 0f)
            {
                foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, explosionRadius))
                {
                    Player player;

                    if (collider.TryGetComponent(out player))
                    {
                        player.SetStat("Mana", 0f);
                    }
                }

                SpriteDebugger.instance.CreateCircle(transform.position, explosionRadius, 1f);
                Destroy(gameObject);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (showTriggerRadius)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }

        if (showExplosionRadius)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
