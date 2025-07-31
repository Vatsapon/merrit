using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SpiritualGemstone : DroppedItem
{
    [Header("Spiritual Gemstone Settings")]
    [SerializeField] private bool fakeGemstone = false; // Determine if it's the real one or fake one.
    [SerializeField] private float interactionRange = 1f; // Range to interact to open chest.
    [SerializeField] private float additionHealth = 5f; // Additional health that player gain when collected.

    [Header("Explosion Settings")]
    [SerializeField] private bool showExplosionRadius = false; // Determine to show Explosion radius or not.
    [SerializeField] private float explosionDelay = 3f; // Explosion delay.
    [SerializeField] private Damage explosionDamage; // Explosion damages.
    [SerializeField] private float explosionRadius = 1f; // Explosion radius.

    [Header("Components")]
    [SerializeField] private SpriteRenderer fakeSprite; // Fake sprite that shown when player is in Spiritual form.

    protected override void Start()
    {
        base.Start();

        collectable = false;
    }

    protected override void Update()
    {
        if (player != null) {
            base.Update();

        fakeSprite.enabled = fakeGemstone && player.isSpirit;

        float distance = Vector2.Distance(transform.position, player.transform.position);

        // If player is in interaction range, can collect.
        if (distance <= interactionRange)
        {
            // [F] - Collect Gemstone.
            if (Input.GetKeyDown(KeyCode.F))
            {
                // Check if it's the real or fake one.
                if (!fakeGemstone)
                {
                    collectable = true;
                }
                else
                {
                    Explosion();
                }
            }
        }
        }
    }

    private async void Explosion()
    {
        float timer = 0f;

        while (timer < explosionDelay)
        {
            timer += Time.deltaTime;
            await Task.Yield();
        }

        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, explosionRadius))
        {
            Player player;

            if (collider.TryGetComponent(out player))
            {
                player.TakeDamage(explosionDamage, gameObject);
                break;
            }
        }

        //SpriteDebugger.instance.CreateCircle(transform.position, explosionRadius, 1f);
        Destroy(gameObject);
    }

    protected override void OnCollect()
    {
        player.IncreaseStat("Health", additionHealth);
        SFXLibrary.instance.CollectingSpiritualGemstoneSFX.PlayFeedbacks();         
    }

    private void OnDrawGizmos()
    {
        if (showExplosionRadius)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
