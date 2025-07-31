using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickyVine : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float dissolveCooldown = 1f; // Dissolve cooldown before destroy itself.
    [SerializeField] private int attackDamage = 5; // Damage toward player.
    [Range(0, 100)]
    [SerializeField] private float slowSpeedPercentage = 70f; // Speed to slow player in percentage.

    [Header("References")]
    [SerializeField] private ObjectLoader objectLoader;

    private bool dissolve = false; // Determine to dissolve or not.
    private float currentDissolveCooldown; // Dissolve cooldown timer.

    private SpriteRenderer spriteRenderer;
    private float savedAlpha; // Saved alpha for dissolving.
    private Vector3 spawnPos;

    private void Start()
    {
        spriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        savedAlpha = spriteRenderer.color.a;

        currentDissolveCooldown = dissolveCooldown;
        objectLoader = ObjectLoader.instance;
        spawnPos = transform.position;
    }

    private void Update()
    {
        // If not dissolve yet, do damage when player close by
        if (dissolve)
        {
            // Make sprite fade away (will replace with sprite animation instead)
            float alpha = (savedAlpha * currentDissolveCooldown) / dissolveCooldown;
            Color color = spriteRenderer.color;

            spriteRenderer.color = new Color(color.r, color.g, color.b, alpha);

            if (currentDissolveCooldown > 0f)
            {
                currentDissolveCooldown -= Time.deltaTime;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    // Function to fetch Slow percentage.
    public float GetSlowPercentage()
    {
        return slowSpeedPercentage;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        Player player;

        if (collision.TryGetComponent(out player) && !dissolve)
        {
            if (player.IsInvincible())
            {
                return;
            }

            Damage toInflict = new Damage(attackDamage, attackDamage);
            player.TakeDamage(toInflict, gameObject);
            dissolve = true;
        }
    }

    public Vector3 GetSpawnPos() {
        return spawnPos;
    }

}
