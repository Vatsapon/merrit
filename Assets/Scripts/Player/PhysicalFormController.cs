using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MoreMountains.Feedbacks;
using static Ability;
using static Entity;
using static PhysicalFormCombat;
using static Player;

public enum GroundMaterial { Grass, Stone, Wood, None }


public class PhysicalFormController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float airLinearDrag = 1f; // Linear drag for air movement.
    [SerializeField] private float jumpHeight = 5f; // Jump height.
    [SerializeField] private float fallMultiplier = 5f; // Muliply gravity force while falling.
    [SerializeField] private float lowJumpFallMultiplier = 5f; // Muliply gravity force while in mid-air (Not falling).
    [SerializeField] private float hangTime = 0.1f; // Hang time after leaving ground.

    [Space(10)]
    [SerializeField] private AnimationCurve dashCurve; // Dash Speed depends on Animation Curve.
    [SerializeField] private float dashSpeed = 5f; // Dash speed when using Dash.
    [SerializeField] private float dashDuration = 1f; // Duration while dashing.
    [SerializeField] private float dashCooldown = 1f; // Dash Cooldown.

    [Header("Corner Correction")]
    [SerializeField] private float rayCastLength; // Raycast Length to check above.
    [SerializeField] private Vector2 rayCastOffset; // Raycast offset from player.
    [SerializeField] private float outerOffset; // Raycast offset for Outer corner.
    [SerializeField] private float innerOffset; // Raycast offset for Inner corner.

    [Space(10)]
    [SerializeField] private float groundHitboxOffset; // Ground Hitbox Offset from Player.
    [SerializeField] private Vector2 groundHitboxSize = Vector2.one; // Ground Hitbox size.
    [SerializeField] private float groundJumpBufferOffset; // Ground (for Jump Buffer) Hitbox Offset from Player.

    [Header("Components")]
    public Rigidbody2D rigidBody; // Player's Rigidbody.
    public BoxCollider2D colliders; // Physical form's Collider.
    public Animator animator; // Animator for Physical form.

    [Header("Debugging")]
    [SerializeField] private bool showGroundHitbox = false; // Either show Hitbox or not.
    [SerializeField] private bool showJumpBufferHitbox = false; // Either show Jump Buffer Hitbox or not.
    [SerializeField] private bool showCornerCorrection = false; // Either show Corner Correction or not.

    [Header("FloorMaterialDetectionAndSounds")]
    public GroundMaterial currentGround = GroundMaterial.None;
    public FloorSound[] floorSounds;
    public LayerMask whatIsGround;

    [Header("Feedbacks")]
    public MMFeedbacks splashingSFX;
    public MMFeedbacks fallSFX;
    public MMFeedbacks dropFromPlatformSFX;

    private float maxMoveSpeed; // Maximum player's move speed.
    private float modifiedSpeed; // Modified player's move speed.
    private float currentDashCooldown; // Dash cooldown timer.
    private float currentDashDuration; // Dash duration timer.
    private int dashDirection; // Save direction where player's dashing toward to.
    private float currentHangTime; // Hang time timer.
    private bool cornerCorrecton; // Check either in Corner Correction.
    private bool jumpBuffer = false; // Check if player does Jump Buffer.
    private bool landing = false; // Check if player just Landing.

    private Player player;
    private GameManager gameManager;
    private AbilityUpgradeManager abilityUpgradeManager;

    private Action OnEndDash; // Event when Dashing ended.

    private void Start()
    {
        player = instance;
        gameManager = GameManager.instance;
        abilityUpgradeManager = UpgradeManager.instance.abilityUpgradeManager;

        maxMoveSpeed = player.GetBaseStat("Physical Move Speed");
        currentHangTime = hangTime;

        OnEndDash += player.physicalFormCombat.EndDash;
    }

    private void Update()
    {
        if (player.entityState == EntityState.DEAD || player.HasEffect("Stun") || gameManager.IsUI() || CameraManager2.instance.isChoosingTeleport)
        {
            rigidBody.velocity = Vector2.up * rigidBody.velocity.y;
            animator.SetFloat("MoveX", 0f);
            return;
        }

        int x = (int) Input.GetAxisRaw("Horizontal");

        // Check either player is on ground, start Hang time.
        if (IsGrounded())
        {
            // Check if player just jump from the ground. Not Hang Time.
            if (currentHangTime != 0f)
            {
                currentHangTime = hangTime;
            }

            // Check if player has Jump Buffer.
            if (jumpBuffer)
            {
                jumpBuffer = false;
            }
        }
        else
        {
            currentHangTime -= Time.deltaTime;
        }

        // Prevent from changing to Walking animation, if being frozen.
        if (rigidBody.velocity.x != 0)
        {
            animator.SetFloat("MoveX", Mathf.Abs(x));
        }
        else
        {
            animator.SetFloat("MoveX", 0f);
        }
        
        animator.SetFloat("VelocityY", rigidBody.velocity.y);
        animator.SetBool("IsGround", IsGrounded());

        if (player.playerState == PlayerState.WALK && !DialogueManager.isInConversation && !gameManager.IsUI())
        {
            Jump();
            Dash();
        }
        else
        {
            // Prevent from move around. (Only when player isn't attack UP direction or has dashForce)
            if (player.playerState == PlayerState.ATTACK)
            {
                AttackArea attackArea = player.physicalFormCombat.GetAttack();

                if (attackArea != null)
                {
                    // If there's no Dash Force, Freeze.
                    if (attackArea.dashForce == 0)
                    {
                        rigidBody.velocity = Vector2.up * rigidBody.velocity.y;
                    }

                    // Make player be able to jump, if attacking UP direction.
                    if (attackArea.attackDirection == AttackDirection.UP)
                    {
                        Jump();
                    }

                    // Dash Attack while in Combo 2.
                    if (attackArea.combo == 2)
                    {
                        Dash();
                    }
                }
            }
            else
            {
                rigidBody.velocity = Vector2.up * rigidBody.velocity.y;
            }
        }

        if (currentDashCooldown > 0f)
        {
            currentDashCooldown -= Time.deltaTime;
        }

        //SFX System
        if (!IsGrounded())
        {
            if (rigidBody.velocity.y <= 0f && !fallSFX.IsPlaying)
            {
                fallSFX.PlayFeedbacks();
            }
        }
        else if (IsGrounded())
        {
            UpdateGroundMaterial();
            fallSFX.StopFeedbacks();
        }
    }

    private void FixedUpdate()
    {
        if (player.entityState == EntityState.DEAD || player.HasEffect("Stun") || CameraManager2.instance.isChoosingTeleport)
        {
            rigidBody.velocity = Vector2.up * rigidBody.velocity.y;
            return;
        }

        maxMoveSpeed = player.GetStat("Physical Move Speed");

        ApplyLinearDrag();
        FallMulitplier();

        if (player.playerState == PlayerState.WALK && !DialogueManager.isInConversation && !gameManager.IsUI())
        {
            Movement();
        }
        else
        {
            // Check if player is Attacking.
            if (player.playerState == PlayerState.ATTACK)
            {
                AttackArea attackArea = player.physicalFormCombat.GetAttack();

                // Make player be able to move while attacking UP direction.
                if (attackArea != null && attackArea.attackDirection == AttackDirection.UP)
                {
                    Movement();
                }
            }
        }

        // Check Corner Correction.
        Vector2 center = (Vector2) player.transform.position + rayCastOffset;
        Vector2 innerOffsetVector = Vector2.right * innerOffset;
        Vector2 outerOffsetVector = Vector2.right * outerOffset;

        Physics2D.queriesHitTriggers = false;

        bool leftOuterHit = Physics2D.Raycast(center - outerOffsetVector, Vector2.up, rayCastLength);
        bool leftInnerHit = Physics2D.Raycast(center - innerOffsetVector, Vector2.up, rayCastLength);
        bool rightOuterHit = Physics2D.Raycast(center + outerOffsetVector, Vector2.up, rayCastLength);
        bool rightInnerHit = Physics2D.Raycast(center + innerOffsetVector, Vector2.up, rayCastLength);

        Physics2D.queriesHitTriggers = true;

        cornerCorrecton = !IsGrounded() && (leftOuterHit && !leftInnerHit || rightOuterHit && !rightInnerHit);
        
        if (cornerCorrecton)
        {
            CornerCorrection();
        }
    }

    private void Movement() {
        
        // If player is being knockbacked, ignore all movements.
        if (player.isKnockbacking)
        {
            return;
        }

        float x = Input.GetAxis("Horizontal");

        // Prevent from walk both side.
        if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.D))
        {
            x = 0f;
        }

        // iF Dashing, use Dash direction.
        if (IsDashing())
        {
            x = dashDirection;
        }

        // Flip sprite when move toward opposite direction.
        if (x != 0f)
        {
            player.spriteRenderer.flipX = x < 0f;
        }

        float finalSpeed = maxMoveSpeed + modifiedSpeed;

        rigidBody.velocity = new Vector2(x * finalSpeed, rigidBody.velocity.y);

        // Clamp speed between 0 and maxMoveSpeed.
        if (Mathf.Abs(rigidBody.velocity.x) > finalSpeed)
        {
            rigidBody.velocity = new Vector2(Mathf.Sign(rigidBody.velocity.x) * finalSpeed, rigidBody.velocity.y);
        }

        // Check if player is dashing.
        if (IsDashing())
        {
            if (currentDashDuration > 0f)
            {
                float duration = currentDashDuration / dashDuration;
                float finalDashSpeed = dashSpeed * dashCurve.Evaluate(1f - duration);
                rigidBody.AddForce(Vector2.right * dashDirection * finalDashSpeed * 100f, ForceMode2D.Force);
                rigidBody.gravityScale = 0f;
                rigidBody.velocity = Vector2.right * rigidBody.velocity.x;

                currentDashDuration -= Time.fixedDeltaTime;
            }
            else
            {
                dashDirection = 0;
                animator.SetBool("Dashing", false);

                OnEndDash.Invoke();
            }
        }
    }

    // [Space] Jump only when detecting ground beneath or during hang time.
    private void Jump()
    {
        // If player is being knockbacked, ignore all movements.
        if (player.isKnockbacking)
        {
            return;
        }

        // If player is already in Jumping animation while still on ground, cancel the hang time.
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Anim_Physical_Jump") && IsGrounded())
        {
            currentHangTime = hangTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) && (currentHangTime > 0f || !jumpBuffer) && !IsDashing())
        {
            // Check if player is able to do Jump Buffer.
            if (!jumpBuffer && IsJumpBuffer())
            {
                animator.Play("Anim_Physical_Jump");
                jumpBuffer = true;

                rigidBody.velocity = new Vector2(rigidBody.velocity.x, -0.1f);
                rigidBody.AddForce(Vector2.up * jumpHeight, ForceMode2D.Impulse);
                return;
            }

            // Check if player can or can't do Hang Time Jump.
            if (currentHangTime <= 0f || jumpBuffer)
            {
                return;
            }

            animator.Play("Anim_Physical_Jump");
            currentHangTime = 0f;

            rigidBody.velocity = new Vector2(rigidBody.velocity.x, -0.1f);
            rigidBody.AddForce(Vector2.up * jumpHeight, ForceMode2D.Impulse);
        }
    }

    // [L.Shift] - Dash toward facing direction. (LEFT or RIGHT)
    private void Dash()
    {
        // If player is being knockbacked, ignore all movements.
        if (player.isKnockbacking)
        {
            return;
        }

        // Check if player has already unlock Dashing abilities Lv.4 (Cancel attack and start Dashing).
        if (!abilityUpgradeManager.GetAbility(AbilityType.DASH).abilityLevel[3].purchased && player.physicalFormCombat.attacking)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && currentDashCooldown <= 0f)
        {
            // Check if player is on ground, player ground dash particle.
            if (IsGrounded())
            {
                GameObject dashParticle = Instantiate(player.util.particles.playerDashTrail, transform.position, Quaternion.identity);
                splashingSFX.PlayFeedbacks();

                if (player.LookRight())
                {
                    Vector3 particleScale = dashParticle.transform.localScale;
                    dashParticle.transform.localScale = new Vector3(-particleScale.x, particleScale.y, particleScale.z);
                }
            }

            currentDashCooldown = dashCooldown;
            currentDashDuration = dashDuration;

            animator.Play("Anim_Physical_Dash");

            int facingX = 0;

            if (player.LookRight())
            {
                facingX = 1;
            }
            else
            {
                facingX = -1;
            }

            dashDirection = facingX;
            animator.SetBool("Dashing", true);
        }
    }

    public void PlayWalkingSFX()
    {
        PlayFloorMaterialSound(currentGround, "Walk");
    }


    // Function to apply linear drag.
    private void ApplyLinearDrag()
    {
        float x = Input.GetAxis("Horizontal");

        if (IsGrounded())
        {
            rigidBody.drag = 0f;
        }
        else
        {
            Vector2 center = (Vector2)player.transform.position + (Vector2.down * groundHitboxOffset);
            Vector2 position1 = center + new Vector2(colliders.bounds.size.x / 2f, groundHitboxOffset);
            Vector2 position2 = center - new Vector2(colliders.bounds.size.x / 2f, groundHitboxOffset);

            List<Collider2D> collider = Physics2D.OverlapAreaAll(position1, position2).ToList();

            if (collider.Where(col => col.CompareTag("Ground") || col.CompareTag("FragileGround")).ToList().Count > 0)
            {
                rigidBody.drag = 0f;
            }
            else
            {
                rigidBody.drag = airLinearDrag;
            }
        }
    }

    // Function to apply gravity force during jumping and falling.
    private void FallMulitplier()
    {
        float y = rigidBody.velocity.y;

        if (y < 0f)
        {
            Vector2 center = (Vector2)player.transform.position + (Vector2.down * groundHitboxOffset);
            Vector2 position1 = center + new Vector2(colliders.bounds.size.x / 2f, groundHitboxOffset);
            Vector2 position2 = center - new Vector2(colliders.bounds.size.x / 2f, groundHitboxOffset);

            List<Collider2D> collider = Physics2D.OverlapAreaAll(position1, position2).ToList();

            if (collider.Where(col => col.CompareTag("Ground") || col.CompareTag("FragileGround")).ToList().Count > 0)
            {
                rigidBody.gravityScale = 1f;
            }
            else
            {
                rigidBody.gravityScale = fallMultiplier;
            }
        }
        else
        {
            if (y > 0f && Input.GetKey(KeyCode.Space))
            {
                rigidBody.gravityScale = lowJumpFallMultiplier;
            }
            else
            {
                rigidBody.gravityScale = 1f;
            }
        }
    }
    
    // Function to check corner correction.
    private void CornerCorrection()
    {
        Physics2D.queriesHitTriggers = false;

        Vector2 center = (Vector2)player.transform.position + rayCastOffset;
        Vector2 innerOffsetVector = Vector2.right * innerOffset;
        Vector2 outerOffsetVector = Vector2.right * outerOffset;

        // Left corner - Push to the right.
        RaycastHit2D hit = Physics2D.Raycast(center - innerOffsetVector + Vector2.up * rayCastLength, Vector2.left, rayCastLength);

        if (hit.collider != null && hit.collider.CompareTag("Ground") && !hit.collider.GetComponent<Platform>())
        {
            Vector2 innerCorner = new Vector2(hit.point.x, center.y) + Vector2.up * rayCastLength;
            Vector2 outerCorner = center - outerOffsetVector + Vector2.up * rayCastLength;

            float offsetX = Vector2.Distance(innerCorner, outerCorner);
            player.transform.position = new Vector3(player.transform.position.x + offsetX, player.transform.position.y, player.transform.position.z);

            Physics2D.queriesHitTriggers = true;
            return;
        }

        // Right corner - Push to the left.
        hit = Physics2D.Raycast(center + innerOffsetVector + Vector2.up * rayCastLength, Vector2.right, rayCastLength);

        if (hit.collider != null && hit.collider.CompareTag("Ground") && !hit.collider.GetComponent<Platform>())
        {
            Vector2 innerCorner = new Vector2(hit.point.x, center.y) + Vector2.up * rayCastLength;
            Vector2 outerCorner = center + outerOffsetVector + Vector2.up * rayCastLength;

            float offsetX = Vector2.Distance(innerCorner, outerCorner);
            player.transform.position = new Vector3(player.transform.position.x - offsetX, player.transform.position.y, player. transform.position.z);

            Physics2D.queriesHitTriggers = true;
            return;
        }

        Physics2D.queriesHitTriggers = true;
    }

    // Function to check either player is on the ground.
    public bool IsGrounded()
    {
        Vector2 center = (Vector2) player.transform.position + (Vector2.down * groundHitboxOffset);
        Vector2 position1 = center + new Vector2(groundHitboxSize.x / 2f, groundHitboxSize.y / 2f);
        Vector2 position2 = center - new Vector2(groundHitboxSize.x / 2f, groundHitboxSize.y / 2f);

        List<Collider2D> collider = Physics2D.OverlapAreaAll(position1, position2, whatIsGround).ToList();

        if (collider.ToList().Count > 0)
        {
            if (!landing && rigidBody.velocity.y <= 0 && !Input.GetKey(KeyCode.S) && currentGround.Equals(GroundMaterial.None))
            {
                UpdateGroundMaterial();
                PlayFloorMaterialSound(currentGround, "Landing");
            }
            if (currentGround == GroundMaterial.Wood && Input.GetKeyDown(KeyCode.S))
            {
                dropFromPlatformSFX.PlayFeedbacks();
            }

            landing = true;

            // Check if player is falling or stand, not jumping up toward Platform.
            if (rigidBody.velocity.y <= 0f || player.physicalFormCombat.GetIsPerformingTwistedDive())
            {
                return true;
            }
        }
        else
        {
            currentGround = GroundMaterial.None;
        }

        if (collider.Where(col => col.CompareTag("Ground") || col.CompareTag("FragileGround")).ToList().Count > 0 && Input.GetKeyDown(KeyCode.S))
        {
            dropFromPlatformSFX.PlayFeedbacks();
        }

        landing = false;
        return false;
    }
        
    void PlayFloorMaterialSound(GroundMaterial groundMaterial, string soundType)
    {
        //if inthe air, dont play sounds
        if (groundMaterial == GroundMaterial.None) return;

        foreach (var item in floorSounds)
        {
            if (item.material == groundMaterial)
            {
                if (soundType == "Landing")
                {
                    if (!item.Landing.IsPlaying)
                        item.Landing.PlayFeedbacks();
                }
                else
                {
                    item.Wallk.PlayFeedbacks();
                }

                break;
            }
        }
    }

    // Function to check either player is on Jump Buffer range.
    public bool IsJumpBuffer()
    {
        Vector2 center = (Vector2)player.transform.position + (Vector2.down * groundJumpBufferOffset);
        Vector2 position1 = center + new Vector2(colliders.bounds.size.x / 2f, groundJumpBufferOffset);
        Vector2 position2 = center - new Vector2(colliders.bounds.size.x / 2f, groundJumpBufferOffset);

        List<Collider2D> collider = Physics2D.OverlapAreaAll(position1, position2).ToList();

        if (collider.ToList().Where(col => col.CompareTag("Ground") || col.CompareTag("FragileGround")).ToList().Count > 0)
        {
            return rigidBody.velocity.y < 0f;
        }

        return false;
    }

    // Function to check if player is dashing.
    public bool IsDashing()
    {
        return animator.GetBool("Dashing");
    }

    // Function to return current dash duration (Used for Dash Attack in PhysicalFormCombat.cs)
    public float GetCurrentDashDuration()
    {
        return currentDashDuration;
    }
    
    private void OnTriggerStay2D(Collider2D collision)
    {
        StickyVine stickyVine;

        // If player touching Sticky Vine, make player walk slower.
        if (collision.TryGetComponent(out stickyVine))
        {
            modifiedSpeed = -((maxMoveSpeed * stickyVine.GetSlowPercentage()) / 100f);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<StickyVine>() != null)
        {
            modifiedSpeed = 0f;
        }
    }

    void UpdateGroundMaterial()
    {
        Vector2 center = (Vector2)player.transform.position + (Vector2.down * groundHitboxOffset);
        Vector2 position1 = center + new Vector2(groundHitboxSize.x / 2f, groundHitboxSize.y / 2f);
        Vector2 position2 = center - new Vector2(groundHitboxSize.x / 2f, groundHitboxSize.y / 2f);

        List<Collider2D> collider = Physics2D.OverlapAreaAll(position1, position2).ToList();

        if (collider.Where(col => col.name.Contains("One Way Platform") || col.name.Contains("OWP")).ToList().Count > 0)
        {
            currentGround = GroundMaterial.Wood;
        }
        else if (collider.Where(col => col.name.Contains("Fragile Platform") || col.name.Contains("Moving Platform")).ToList().Count > 0)
        {
            currentGround = GroundMaterial.Stone;
        }
        else
        {
            currentGround = GroundMaterial.Grass;
        }
    }

    // Debugging
    private void OnDrawGizmos()
    {
        if (showGroundHitbox)
        {
            Gizmos.color = Color.blue;

            Vector2 center = (Vector2) transform.position + (Vector2.down * groundHitboxOffset);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center, groundHitboxSize);
        }

        if (showJumpBufferHitbox)
        {
            Gizmos.color = Color.blue;

            Vector2 center = (Vector2)transform.position + (Vector2.down * groundJumpBufferOffset);
            Vector2 size = new Vector2(colliders.bounds.size.x, groundJumpBufferOffset * 2f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, size);
        }

        if (showCornerCorrection)
        {
            Vector2 center = (Vector2)transform.position + rayCastOffset;
            Vector2 innerOffsetVector = Vector2.right * innerOffset;
            Vector2 outerOffsetVector = Vector2.right * outerOffset;

            Gizmos.color = Color.white;
            Gizmos.DrawLine(center + outerOffsetVector, center + outerOffsetVector + Vector2.up * rayCastLength);
            Gizmos.DrawLine(center - outerOffsetVector, center - outerOffsetVector + Vector2.up * rayCastLength);

            Gizmos.DrawLine(center + innerOffsetVector, center + innerOffsetVector + Vector2.up * rayCastLength);
            Gizmos.DrawLine(center - innerOffsetVector, center - innerOffsetVector + Vector2.up * rayCastLength);

            Vector2 left = center - innerOffsetVector + Vector2.up * rayCastLength;
            Gizmos.DrawLine(left, left + Vector2.left * rayCastLength);

            Vector2 right = center + innerOffsetVector + Vector2.up * rayCastLength;
            Gizmos.DrawLine(right, right + Vector2.right * rayCastLength);
        }
    }
}

[System.Serializable]
public class FloorSound
{
    public GroundMaterial material;
    public MMFeedbacks Landing;
    public MMFeedbacks Wallk;
}