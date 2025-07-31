using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class PoisonAtmosphere : MonoBehaviour
{
    [Header("Poison Atmosphere Settings")]
    [SerializeField] private Damage poisonDamage; // Poison damages.
    [SerializeField] private float poisonDelay; // Delay before taking poison damages.
    [Space(5)]
    [SerializeField] private bool showPoisonArea = false; // Determine to show Poison area or not.
    [Space(5)]
    public Vector2 poisonAreaSize = Vector2.one; // Poison area detection's size.

    [Header("References")]
    [SerializeField] private ParticleSystem particle;

    private BoxCollider2D colliders;
    private float currentPoisonDelay; // Poison delay timer.

    private void Start()
    {
        colliders = GetComponent<BoxCollider2D>();
        currentPoisonDelay = poisonDelay;
    }

    private void Update()
    {
        // Set particle size and emission rate.
        EmissionModule emission = particle.emission;
        emission.rateOverTime = poisonAreaSize.x * poisonAreaSize.y;
        ShapeModule shape = particle.shape;
        shape.scale = poisonAreaSize;

        colliders.size = poisonAreaSize;

        Vector2 position1 = (Vector2) transform.position + (poisonAreaSize / 2f);
        Vector2 position2 = (Vector2) transform.position - (poisonAreaSize / 2f);

        bool foundPlayer = false;

        foreach (Collider2D collider in Physics2D.OverlapAreaAll(position1, position2))
        {
            Player player;

            if (collider.TryGetComponent(out player))
            {
                // If player is in Spiritual form, don't take damages.
                if (player.isSpirit)
                {
                    break;
                }

                currentPoisonDelay -= Time.deltaTime;

                if (currentPoisonDelay <= 0f)
                {
                    player.TakeDamage(poisonDamage, gameObject);
//                    Debug.Log($"Poison: {poisonDamage.physicalDamage}");
                    currentPoisonDelay = poisonDelay;
                }

                foundPlayer = true;
                break;
            }
        }

        if (!foundPlayer)
        {
            currentPoisonDelay = poisonDelay;
        }

        currentPoisonDelay = Mathf.Clamp(currentPoisonDelay, 0f, poisonDelay);
    }

    private void OnDrawGizmos()
    {
        if (showPoisonArea)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, poisonAreaSize);
        }
    }

    public void SetPoisonAreaSize(Vector2 size) {
        poisonAreaSize = size;
    }

}
