using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using MoreMountains.Feedbacks;

public class Boulder : MonoBehaviour
{
    private enum BoulderState
    {
        DETECTING, SLAMMING, RESETTING
    }

    [Header("Settings")]
    [SerializeField] private float crumbleOffset = 3f; // Offset while crumbling.
    [SerializeField] private float crumbleTime = 0.1f; // Time to do crumble animation (Delay before slamming).
    [SerializeField] private float fallSpeed = 1f; // Speed when boulder slam down.
    [SerializeField] private float fallDistance = 2f; // Fall distance from original position.
    [SerializeField] private float recoverUpSpeed = 1f; // Speed when boulder recovering.
    [SerializeField] private float recoverCooldown = 1f; // Recover time after reset.

    [Space(10)]
    [SerializeField] private float detectionAreaOffset = 5f; // Detection offset from Boulder.
    [SerializeField] private float hitAreaOffset = 0.5f; // Hit area offset from Boulder.

    [Space(10)]
    public int attackDamage = 5; // Attack damage toward player.
    [SerializeField] private Vector2 knockbackForce = Vector2.one; // Knockback force that applied to player.

    [Header("References")]
    [SerializeField] private GameObject slamParticle; // Particle when slamming.

    [Header("Debugging")]
    [SerializeField] private bool showDetectionArea = false; // Show detection area.
    [SerializeField] private bool showFallPosition = false; // Show fall position.

    private BoulderState boulderState = BoulderState.DETECTING;
    private float currentCrumbleTime; // Crumble timer.
    private float currentRecoverCooldown; // Recover cooldown timer.
    private float currentCrumbleOffset; // current Crumble offset.
    private bool detected = false; // Determine that it found player or not.
    private Vector2 savedPosition; // Saved original position.
    private bool takeDamaged = false; // Determine if player had taken damages.

    private Rigidbody2D rigidBody; // Boulder's Rigidbody.
    private Collider2D colliders; // Boulder's Collider.
    private Player player;

    public MMFeedbacks movingSFX, impactSFX;
    bool hasPlayed;

    private void OnDisable()
    {
        /*

        rigidBody.velocity = Vector2.zero;
        transform.position = savedPosition;
        currentCrumbleTime = crumbleTime;
        currentRecoverCooldown = recoverCooldown;
        currentCrumbleOffset = crumbleOffset;

        detected = false;
        takeDamaged = false;

        boulderState = BoulderState.DETECTING;
        */
    }

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        colliders = GetComponent<Collider2D>();
        player = Player.instance;

        savedPosition = transform.position;

        currentCrumbleOffset = crumbleOffset;
        currentCrumbleTime = crumbleTime;
        currentRecoverCooldown = recoverCooldown;
        hasPlayed = false;
    }

    private void FixedUpdate()
    {
        switch (boulderState)
        {
            case BoulderState.DETECTING:

                rigidBody.isKinematic = true;

                // If not detecting, keep detect player.
                if (!detected)
                {
                    Detecting();
                }
                else
                {
                    detected = false;
                    movingSFX.PlayFeedbacks();
                    boulderState = BoulderState.SLAMMING;
                }

                break;

            case BoulderState.SLAMMING:
            
                // Crumbling the Boulder.
                if (currentCrumbleTime > 0f)
                {
                    rigidBody.isKinematic = true;
                    currentCrumbleTime -= Time.fixedDeltaTime;

                    // Do crumble animation.
                    float randomX = Random.Range(-currentCrumbleOffset, currentCrumbleOffset);
                    transform.position = new Vector2(savedPosition.x + randomX, transform.position.y);

                    currentCrumbleOffset = (crumbleOffset * currentCrumbleTime) / crumbleTime;

                    // If it finished crumble, reset position and timer.
                    if (currentCrumbleTime <= 0f)
                    {
                        transform.position = savedPosition;
                        currentCrumbleOffset = crumbleOffset;
                    }
                }
                else
                {
                    rigidBody.isKinematic = false;
                    transform.position = new Vector2(savedPosition.x, transform.position.y);

                    // Falling
                    rigidBody.velocity = Vector2.down * fallSpeed * 100f * Time.fixedDeltaTime;

                    Vector2 destination = savedPosition + Vector2.down * fallDistance;

                    // If it reaches fall distance, resetting back.
                    if (transform.position.y <= destination.y)
                    {
                        if (!hasPlayed) impactSFX.PlayFeedbacks();

                        transform.position = destination;

                        currentCrumbleTime = crumbleTime;
                        boulderState = BoulderState.RESETTING;
                        VirtualCamera.instance.CameraShake(CameraEventName.BOULDER_SMASH);

                        Vector2 position = (Vector2)transform.position + (Vector2.down * colliders.bounds.size.y / 2f);
                        Instantiate(slamParticle, position, Quaternion.identity);
                        return;
                    }

                    Vector2 center = (Vector2)transform.position + (Vector2.down * hitAreaOffset);
                    Vector2 position1 = center + new Vector2(colliders.bounds.size.x / 2f, hitAreaOffset);
                    Vector2 position2 = center - new Vector2(colliders.bounds.size.x / 2f, hitAreaOffset);

                    foreach (Collider2D collider in Physics2D.OverlapAreaAll(position1, position2))
                    {
                        // Check if it's not this object or it's not trigger collider.
                        if (collider.gameObject.Equals(gameObject) || collider.isTrigger)
                        {
                            continue;
                        }

                        // Check if it hit player, do damages to player with knockback force.
                        if (collider.gameObject.Equals(player.gameObject) && !takeDamaged)
                        {
                            takeDamaged = true;
                            DamageDelay();

                            Damage toInflict = new Damage(attackDamage, attackDamage);
                            player.TakeDamage(toInflict, gameObject);
                            player.Knockback(transform.position, knockbackForce);
                            continue;
                        }
                    }
                }

                break;

            case BoulderState.RESETTING:
            
                // If recovering, wait until time out and move up back to same position.
                if (currentRecoverCooldown > 0f)
                {
                    currentRecoverCooldown -= Time.deltaTime;
                }
                else
                {
                    rigidBody.velocity = Vector2.up * recoverUpSpeed * 100f * Time.fixedDeltaTime;

                    // If it's close enough, set position to savedPosition and back to detecting state.
                    if (Vector2.Distance(transform.position, savedPosition) <= 0.1f)
                    {
                        rigidBody.velocity = Vector2.zero;
                        transform.position = savedPosition;
                        currentRecoverCooldown = recoverCooldown;

                        boulderState = BoulderState.DETECTING;
                    }
                }

                break;
        }
    }

    // Function to detecting player in the detection area.
    private void Detecting()
    {
        try {
            // Check if not detecting yet, keep detecting.
            if (!detected)
            {
                Vector2 center = (Vector2)transform.position + (Vector2.down * detectionAreaOffset);
                Vector2 position1 = center + new Vector2(colliders.bounds.size.x / 2f, detectionAreaOffset);
                Vector2 position2 = center - new Vector2(colliders.bounds.size.x / 2f, detectionAreaOffset);

                foreach (Collider2D collider in Physics2D.OverlapAreaAll(position1, position2))
                {
                    // Check if it's not this object or it's not trigger collider.
                    if (collider.gameObject.Equals(gameObject) || collider.isTrigger)
                    {
                        continue;
                    }

                    // Check if it found player, detected.
                    if (collider.gameObject.Equals(player.gameObject))
                    {
                        detected = true;
                        break;
                    }
                }
            }
        } catch {
            // ADDED THI
        }
    }

    // Function to delay after player took damages.
    private async void DamageDelay()
    {
        float timer = 0.2f;
        
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            await Task.Yield();
        }

        takeDamaged = false;
    }

    private void OnDrawGizmos()
    {
        // Show Detection area in Editor.
        if (showDetectionArea)
        {
            if (!colliders)
            {
                colliders = GetComponent<Collider2D>();
            }

            // Show Detection Area.
            Vector2 offset = Vector2.down * detectionAreaOffset;
            Vector2 size = new Vector2(colliders.bounds.size.x, detectionAreaOffset * 2);

            Vector2 center = (Vector2)transform.position + offset;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, size);

            // Show Hit Area.
            offset = Vector2.down * hitAreaOffset;
            size = new Vector2(colliders.bounds.size.x, hitAreaOffset * 2);

            center = (Vector2)transform.position + offset;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, size);
        }

        if (showFallPosition)
        {
            if (!colliders)
            {
                colliders = GetComponent<Collider2D>();
            }

            Vector2 offset = Vector2.down * fallDistance;
            Vector2 center = (Vector2)transform.position + offset;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, colliders.bounds.size);
        }
    }

}
