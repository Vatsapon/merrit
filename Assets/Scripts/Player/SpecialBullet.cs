using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using static UnityEngine.ParticleSystem;

public class SpecialBullet : MonoBehaviour
{
    [Header("Special Bullet Settings")]
    public int damage; // Damage of bullet.
    public float bulletSpeed; // Speed of bullet.
    public float maxSpeed; // Maximum speed of bullet.
    public float scalingFactor;
    public Vector2 unitVector; // Direction of bullet.
    public float initSize; // Initial size of bullet.
    public float expoIncreaseSizeRate; // Exponential increase size rate.
    public float initSpeed; // Initial speed of bullet.
    public float expoDecreaseSpeedRate; // Exponential decrease speed rate.
    public bool shouldGoThrough; // Determine if bullet is penetration or not.
    public float maxSize; // Maximum size of bullet.
    public bool shouldNotIncrease; // Determine if bullet's size should be increase or not.
    public float minSpeed; // Minimum speed of bullet.
    public float maxDistance; // Maximum bullet's distance.

    private Vector2 initPos; // Initial position of bullet.
    private float currentFlyTime; // Current bullet's decay time.
    private GameObject hitParticle; // Particle when bullet hit.

    private Player player;
    private Rigidbody2D rigidBody;
    private Collider2D colliders;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidBody = GetComponent<Rigidbody2D>();
        colliders = GetComponent<Collider2D>();
    }

    private void Start()
    {
        player = Player.instance;

        currentFlyTime = 0f;
        initPos = player.transform.position;
    }

    private void Update()
    {
        ExpoDecaySpeed();
        ExpoIncreaseSize();

        currentFlyTime += Time.deltaTime;
        bulletSpeed = Mathf.Clamp(bulletSpeed, 0f, maxSpeed);

        float distance = Vector2.Distance(initPos, transform.position);

        // If bullet has reaches maximum distance, destroy itself.s
        if (distance > maxDistance)
        {
            Destroy(gameObject);
        }

        // If bullet's size has reaches its maximum size, clamp size to maximum value.
        if (spriteRenderer.transform.localScale.x > maxSize)
        {
            spriteRenderer.transform.localScale = Vector3.one * maxSize;
        }
    }

    // Function to set direction of bullet.
    public void SetDirection(Vector2 unitVector)
    {
        rigidBody.velocity = unitVector * bulletSpeed;
        this.unitVector = unitVector;
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

    // Function to set bullet's damage.
    public void SetDamage(int damage)
    {
        this.damage = damage;
    }

    // Function to set bullet's orientation.
    public void SetOrientation(bool isRight)
    {
        if (isRight) {
            spriteRenderer.flipX = true;
            colliders.offset = new Vector2(-colliders.offset.x, colliders.offset.y);
        }
    }

    // Function to set bullet's rotation.
    public void SetRotation(float angle)
    {
        transform.eulerAngles = Vector3.forward * angle * 180 / Mathf.PI;
    }

    // Function to update bullet's speed in exponential.
    public void ExpoDecaySpeed()
    {
        Func<float, float> expo = x => initSpeed * Mathf.Exp(- expoDecreaseSpeedRate * x);
        bulletSpeed = expo(currentFlyTime) + minSpeed;
        SetDirection(unitVector);
    }

    // Function to update bullet's size in exponential.
    public void ExpoIncreaseSize()
    {
        Func<float, float> expo = x => initSize * Mathf.Exp(expoIncreaseSizeRate * x);
        spriteRenderer.transform.localScale = Vector3.one * expo(currentFlyTime);
    }

    // Function to set, if bullet's penetration or not.
    public void SetShouldGoThrough(bool value)
    {
        shouldGoThrough = value;
    }

    // Function to set bullet's initial size.
    public void SetInitSize(float size)
    {
        initSize = size;
        spriteRenderer.transform.localScale = Vector3.one * size;
    }

    // Function to set bullet's initial speed.
    public void SetInitSpeed(float speed)
    {
        initSpeed = speed;
    }

    // Function to set bullet's speed exponential rate.
    public void SetExpoDecaySpeedRate(float rate)
    {
        expoDecreaseSpeedRate = rate;
    }
    
    // Function to set bullet's size exponential rate.
    public void SetExpoIncreaseSizeRate(float rate)
    {
        expoIncreaseSizeRate = rate;
    }

    // Function to set bullet's maximum size.
    public void SetMaxSize(float maxSize)
    {
        this.maxSize = maxSize;
    }

    // Function to set bullet's minimum speed.
    public void SetMinSpeed(float minSpeed)
    {
        this.minSpeed = minSpeed;
    }

    // Function to set bullet's maxmimum distance.
    public void SetMaxDistance(float maxDistance)
    {
        this.maxDistance = maxDistance;
    }

    // Function to set bullet hit particle.
    public void SetHitParticle(GameObject hitParticle)
    {
        this.hitParticle = hitParticle;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<Enemy>().TakeDamage(new Damage(0f, damage * spriteRenderer.color.a), gameObject); // damage decrease over time
        }

        // If bullet isn't penetration, destroy itself when hit a wall.
        if (!shouldGoThrough)
        {
            if (!other.CompareTag("Player") && !other.CompareTag("Bullet") && !other.CompareTag("EnemyProjectiles"))
            {
                // If it hit other than enemy, destroy itself.
                if (Player.instance.GetSpecialBuffDict().ContainsKey("Piercing Bowstrings") && other.tag != "Enemy")
                {
                    // Play Spiritual Bullet hit particle.
                    Instantiate(player.util.particles.spiritualBulletHitEnemy, transform.position, Quaternion.identity);

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
        }
    }
}
