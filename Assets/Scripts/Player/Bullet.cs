using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using static UnityEngine.ParticleSystem;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public int damage; // Damage of Bullet.
    public float bulletSpeed; // Speed of Bullet.

    private Rigidbody2D rigidBody;
    private SpriteRenderer spriteRenderer;

    private bool shouldMark; // Determine if bullet is able to marking enemy.
    private GameObject hitParticle; // Particle when bullet hit.

    [Header("SFX")]
    public MMFeedbacks bulletHitSFX;

    private Player player;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        player = Player.instance;
    }

    // Function to set bullet's direction.
    public void SetDirection(Vector2 unitVector)
    {
        rigidBody.velocity = unitVector * bulletSpeed;
    }

    // Function to set bullet's speed.
    public void SetBulletSpeed(float speed)
    {
        bulletSpeed = speed;
    }

    // Function to increase bullet's size.
    public void IncrBulletSize(float size)
    {
        spriteRenderer.transform.localScale += Vector3.one * size;
    }

    // Function to set bullet's damages.
    public void SetDamage(int damage)
    {
        this.damage = damage;
    }

    // Function to set bullet become markable or not.
    public void SetShouldMark(bool value)
    {
        shouldMark = value;
    }

    // Function to set bullet hit particle.
    public void SetHitParticle(GameObject hitParticle)
    {
        this.hitParticle = hitParticle;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Entity entity;

        if (other.TryGetComponent(out entity) && entity.CompareTag("Enemy"))
        {
            // Play Spiritual Bullet hit enemy particle.
            Instantiate(player.util.particles.spiritualBulletHitEnemy, transform.position, Quaternion.identity);

            // Damage affected by bullet's decay time.
            entity.TakeDamage(new Damage(0f, damage), gameObject);

            bulletHitSFX.PlayFeedbacks();

            // If bullet is markable, mark this enemy.
            if (shouldMark)
            {
                player.spiritualFormCombat.SetHasMark(true);
                player.spiritualFormCombat.SetMarkOn(other.gameObject);
            }

            Destroy(gameObject);
        }

        OutputInteractable interactable;

        if (other.TryGetComponent(out interactable))
        {
            interactable.SetActivate(true);
        }

        // Spawn bullet hit particle.
        GameObject particle = Instantiate(hitParticle, transform.position, Quaternion.identity);
        particle.transform.up = rigidBody.velocity.normalized;

        MainModule main = particle.GetComponent<ParticleSystem>().main;
        MinMaxCurve mainRotation = main.startRotation;
        mainRotation.constant -= particle.transform.eulerAngles.z * Mathf.PI / 180f;
        main.startRotation = mainRotation;

        for (int i = 0; i < particle.transform.childCount; i++)
        {
            MainModule second = particle.transform.GetChild(i).GetComponent<ParticleSystem>().main;
            second.startRotation = mainRotation;
        }

        Destroy(gameObject);
    }
}
