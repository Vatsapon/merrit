using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using MoreMountains.Feedbacks;
using static Entity;
using static Player;
using static Ability;
using static UnityEngine.ParticleSystem;

public class PhysicalFormCombat : MonoBehaviour
{
    public enum AttackDirection
    {
        UP, LEFT, RIGHT
    }

    [Header("Attack Setting")]
    [SerializeField] private float dashAttackDelay = 0.3f; // Attack delay after Dash Attack.
    [SerializeField] private float attackDuration = 0.1f; // Attack duration while moving. (Make player stop only split second, if not doing combo attack)
    [SerializeField] private float attackComboDuration = 1f; // Attack duration between combo (Replace Sprite animation for now)
    [SerializeField] private float attackDashHangtime = 0.1f; // Attack Dash force hang time to stay float. (Float for X seconds)
    [SerializeField] private float dashAttackBefore = 0.2f; // Delay dash attack while Dashing. (Can do Dash Attack between X to 0 second)
    [SerializeField] private float dashAttackAfter = 0.2f; // Duration where player still can perform dash attack after Dashed.
    [SerializeField] private float comboDuration = 0.5f; // Combo chain duration.
    [SerializeField] private List<AttackArea> attackAreaList = new List<AttackArea>(); // List of attack areas.

    [Header("Twisted Dive")]
    [SerializeField] private GameObject twistedDivePrefab;
    public float twistedDiveCooldown;
    [SerializeField] private float hithoxTravelSpeed;
    [SerializeField] private float airTime; // Air time duration while using Twisted Dive.
    [SerializeField] private float twistedDiveHitboxRadius; // Radius of Twisted Dive's attack.
    [SerializeField] private float speed;
    [SerializeField] private float diveStaggerDuration;
    [SerializeField] private float diveKnockbackForce;
    [SerializeField] private int twistedDiveDamage;
    [SerializeField] private float minHeight;
    [SerializeField] private Vector2 twistedDiveColliderSize;
    [SerializeField] private Vector2 diveColliderOffset;
    [SerializeField] private LayerMask groundLayer;

    [Header("Circle Twirl")]
    [SerializeField] private Vector2 circleTwirlHitbox;
    [SerializeField] private int circleTwirlDamage;
    [SerializeField] private int hitTimes;
    [SerializeField] private float circleTwirlFrequency; // sec/time
    public float circleTwirlCooldown;
    [SerializeField] private float knockbackDistance;
    [ColorUsage(true, true)]
    [SerializeField] private Color circleTwirlColor = Color.white;

    [Header("References")]
    [SerializeField] private SpriteRenderer attackEffect; // Attack VFX.

    private AttackDirection attackDirection; // Attack direction depends movement direction.
    private int comboAmount; // Amount of combo.
    private int previousCombo; // Saved data of previous combo.
    private float currentComboDuration; // Combo chain timer.
    private float currentAttackDelay; // Attack delay timer.
    private float currentAttackDuration; // Attack duration timer.
    private float currentAttackComboDuration; // Attack combo duration timer.
    private bool dashAttack = false; // Determine if player will do Dash Attack after finish Dashing.
    [HideInInspector] public bool attacking = false; // Determine if player is attacking or not.
    private bool isPerformingTwistedDive; // Determine if player is performing Twisted Dive.
    private float twistedDiveCooldownCounter = 0f; // Counter while performing Twisted Dive.
    private float circleTwirlCooldownCounter = 0f; // Counter while performing Circle Twirl.

    private Player player;
    private GameManager gameManager;
    private AbilityUpgradeManager abilityUpgradeManager;

    [Header("Feedbacks")]
    public MMFeedbacks attackHitSFX;
    public MMFeedbacks twistedDiveFB;
    public MMFeedbacks circleTwirlFB;
    public MMFeedbacks basicAttackHitsGroundSFX;

    private void Start()
    {
        player = instance;
        gameManager = GameManager.instance;
        abilityUpgradeManager = UpgradeManager.instance.abilityUpgradeManager;

        currentComboDuration = comboDuration;

        for (int i = 0; i < attackAreaList.Count; i++) {
            attackAreaList[i].attackDamage += player.GetBaseStat("Strength");
        }
    }

    private void Update()
    {
        if (player.entityState == EntityState.DEAD || player.HasEffect("Stun") || player.isKnockbacking)
        {
            return;
        }

        attackEffect.flipX = !player.LookRight();
        player.physicalFormController.animator.SetInteger("ComboAttack", comboAmount);

        // Check either player is DEAD or in Spiritual form or not or opening any UI.
        if (player.entityState == EntityState.DEAD || player.isSpirit || gameManager.IsUI())
        {
            return;
        }

        TimerUpdater();

        // Check if player is using Twisted Dive Ability.
        if (player.playerState == PlayerState.ABILITY)
        {
            if (isPerformingTwistedDive)
            {
                TwistedDiveHelper();
            }

            return;
        }

        // Ability - Twisted Dive.
        if (abilityUpgradeManager.GetAbility(AbilityType.TWISTED_DIVE).unlocked)
        {
            if (Input.GetKeyDown(KeyCode.S) && !player.physicalFormController.IsGrounded() && HasAtLeastMinHeight() && !player.physicalFormController.IsDashing() && twistedDiveCooldownCounter <= 0f)
            {
                StartCoroutine(TwistedDive());

                // Play Twisted Dive Charging particle.
                Instantiate(player.util.particles.twistedDiveCharging, transform.position, Quaternion.identity);

                // playerSFX.PlaySFX(playerSFX.gpCharging);
                return;
            }
        }

        // Ability - Circle Twirl.
        if (abilityUpgradeManager.GetAbility(AbilityType.CIRCLE_TWIRL).unlocked)
        {
            if (Input.GetKeyDown(KeyCode.E) && circleTwirlCooldownCounter <= 0f)
            {
                player.physicalFormController.animator.Play("Anim_Physical_CircleTwirl");
                attackEffect.material.color = circleTwirlColor;

                circleTwirlFB.PlayFeedbacks();

                StartCoroutine(CircleTwirl());
                circleTwirlCooldownCounter = circleTwirlCooldown;
                return;
            }
        }

        AttackInput();
    }
    
    // Function to reset attack timer (Duration, Delay, etc.)
    private void ResetAttackTimer()
    {
        AttackArea attackArea = GetAttack();

        if (attackArea == null)
        {
            return;
        }

        currentAttackDuration = attackDuration;
        currentAttackComboDuration = attackComboDuration;
        currentAttackDelay = attackArea.attackDelay;
    }
    
    // Function to check Attack input.
    public void AttackInput()
    {
        if (gameManager.IsUI())
        {
            return;
        }

        // [Left-Mouse Button] - Normal Attack
        if (Input.GetMouseButtonDown(0) && currentAttackDelay <= 0)
        {
            DirectionUpdater();

            if (comboAmount >= 3)
            {
                return;
            }

            if (attackDirection == AttackDirection.LEFT || attackDirection == AttackDirection.RIGHT)
            {
                // Check if player attack while Dashing.
                if (player.physicalFormController.IsDashing())
                {
                    // Check if player has already unlock Dashing abilities Lv.2 (Ability to perform Dash Attack).
                    if (!abilityUpgradeManager.GetAbility(AbilityType.DASH).abilityLevel[1].purchased)
                    {
                        return;
                    }

                    // Check delay to make sure player can do Dash Attack.
                    float currentDuration = player.physicalFormController.GetCurrentDashDuration();

                    if (currentDuration > 0f && dashAttackBefore > currentDuration)
                    {
                        dashAttack = true;
                    }

                    return;
                }
                else
                {
                    // Play Normal Attack Animation.
                    if (comboAmount == 0)
                    {
                        player.physicalFormController.animator.Play("Anim_Physical_AttackCombo" + (comboAmount + 1));
                    }
                    else
                    {
                        // Check if player is attack on mid-air, force to change animation to Attack instead of Falling.
                        if (!player.physicalFormController.IsGrounded())
                        {
                            player.physicalFormController.animator.Play("Anim_Physical_AttackCombo" + (comboAmount + 1));
                        }
                    }
                }

                DirectionUpdater();
                attacking = true;

                previousCombo = comboAmount;
                comboAmount++;
                currentComboDuration = comboDuration;

                ResetAttackTimer();
            }
            else
            {
                // Prevent player from attack UP while Dashing.
                if (!player.physicalFormController.IsDashing())
                {
                    DirectionUpdater();
                    attacking = true;

                    player.physicalFormController.animator.Play("Anim_Physical_AttackUp");
                    previousCombo = 0;
                    comboAmount = 0;

                    ResetAttackTimer();
                }
            }
        }
    }

    // Function when player start attack.
    public async void Attack()
    {
        AttackArea attackArea = GetAttack();

        if (attackArea == null)
        {
            attacking = false;
            return;
        }

        player.playerState = PlayerState.ATTACK;

        float x = 1f;

        if (attackDirection == AttackDirection.LEFT)
        {
            x *= -1f;
        }

        // Flip sprite toward attack direction.
        if (attackDirection == AttackDirection.LEFT || attackDirection == AttackDirection.RIGHT)
        {
            player.spriteRenderer.flipX = attackDirection == AttackDirection.LEFT;
        }

        //attackEffect.material.SetColor("_Color", attackArea.attackEffectColor);

        // Dash toward direction when attack.
        if (attackArea.dashForce != 0)
        {
            switch (attackDirection)
            {
                case AttackDirection.UP:

                // Prevent player from dash UP, if player stand on ground.
                float y = player.rigidBody.velocity.y;

                if (!player.physicalFormController.IsGrounded())
                {
                    y = 1f;
                }

                player.rigidBody.velocity = (Vector2.up * y * attackArea.dashForce) + (Vector2.right * player.rigidBody.velocity.x);
                break;

                case AttackDirection.LEFT:
                if (player.physicalFormController.IsGrounded())
                {
                    player.rigidBody.MovePosition(GetAttackDashEdge(Vector2.left, attackArea.dashForce));
                }
                else
                {
                    player.rigidBody.MovePosition((Vector2)transform.position + Vector2.left * attackArea.dashForce);
                }

                // If player is on mid-air, do hang time.
                if (!player.physicalFormController.IsGrounded())
                {
                    AttackHangTime();
                }
                break;

                case AttackDirection.RIGHT:
                if (player.physicalFormController.IsGrounded())
                {
                    player.rigidBody.MovePosition(GetAttackDashEdge(Vector2.right, attackArea.dashForce));
                }
                else
                {
                    player.rigidBody.MovePosition((Vector2)transform.position + Vector2.right * attackArea.dashForce);
                }

                // If player is on mid-air, do hang time.
                if (!player.physicalFormController.IsGrounded())
                {
                    AttackHangTime();
                }
                break;
            }

            float timer = 0.05f;

            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                await Task.Yield();
            }
        }

        Vector2 center = (Vector2)transform.position + attackArea.colliderOffset;
        Vector2 position1 = center + new Vector2(attackArea.colliderSize.x / 2f, attackArea.colliderSize.y / 2f);
        Vector2 position2 = center - new Vector2(attackArea.colliderSize.x / 2f, attackArea.colliderSize.y / 2f);

        Collider2D[] area = Physics2D.OverlapAreaAll(position1, position2);

        int enemyCount = 0;

        // Find enemy in attack area and do damages.
        foreach (Collider2D collider in area)
        {
            Entity entity;

            if (collider.TryGetComponent(out entity) && entity.CompareTag("Enemy"))
            {
                enemyCount += 1;

                // Play Attack Particle.
                GameObject selectedParticle = player.util.particles.physicalAttackHit1;

                switch (comboAmount)
                {
                    case 2:
                    selectedParticle = player.util.particles.physicalAttackHit2;
                    break;

                    case 3:
                    selectedParticle = player.util.particles.physicalAttackHit3;
                    break;

                    case 4:
                    selectedParticle = player.util.particles.physicalDashAttack;
                    break;
                }

                if (attackDirection == AttackDirection.UP)
                {
                    selectedParticle = player.util.particles.physicalAttackHitUp;
                }

                Vector2 particlePosition = (Vector2)player.transform.position + attackArea.particleOffset;

                // If player is too close to enemy, spawn particle at enemy's position instead.
                if (Vector2.Distance(transform.position, entity.transform.position) < 1f)
                {
                    particlePosition = player.transform.position;
                }

                GameObject attackParticle = Instantiate(selectedParticle, particlePosition, Quaternion.identity);
                Vector3 particleScale = attackParticle.transform.localScale;
                attackParticle.transform.localScale = new Vector3(particleScale.x * x, particleScale.y, particleScale.z);

                float attackDamage = attackArea.attackDamage;

                // Check if player has already unlock Dashing abilities Lv.3 (Dash Attack after 2nd combo attack deal 200% damages).
                if (abilityUpgradeManager.GetAbility(AbilityType.DASH).abilityLevel[2].purchased && previousCombo == 2)
                {
                    attackDamage *= 2f;
                }

                entity.TakeDamage(new Damage(attackDamage + player.soulFlyDamage, 0f), player.gameObject);

                GameObject selectedBloodParticle = Util.instance.particles.bloodSplashEnemy1;
                Vector2 particleOffset = Vector2.right * x;

                if (attackDirection == AttackDirection.UP)
                {
                    selectedBloodParticle = Util.instance.particles.bloodSplashEnemyUp;
                    particleOffset = Vector2.zero;
                }
                else
                {
                    switch (comboAmount)
                    {
                        case 2:
                        selectedBloodParticle = Util.instance.particles.bloodSplashEnemy2;
                        break;

                        case 3:
                        selectedBloodParticle = Util.instance.particles.bloodSplashEnemy3;
                        VirtualCamera.instance.CameraShake(CameraEventName.ATTACK3RD);
                        break;
                    }
                }

                // Play Blood Particle at enemy's position.
                GameObject bloodParticle = Instantiate(selectedBloodParticle, (Vector2) entity.transform.position + particleOffset, Quaternion.identity);
                Vector3 particleSize = bloodParticle.transform.localScale;
                bloodParticle.transform.localScale = new Vector3(particleSize.x * x, particleSize.y, particleSize.z);

                // Check if there's knockback force.
                if (attackArea.knockbackForce != Vector2.zero)
                {
                    entity.Knockback(transform.position, attackArea.knockbackForce);
                }
            }

            Prop prop;

            if (collider.TryGetComponent(out prop))
            {
                prop.TakeHit();
            }

            OutputInteractable interactable;

            if (collider.TryGetComponent(out interactable))
            {
                interactable.SetActivate(true);
            }

            if (collider.CompareTag("Ground"))
            {
                basicAttackHitsGroundSFX.PlayFeedbacks();
            }
        }

        if (enemyCount > 0)
        {
            attackHitSFX.PlayFeedbacks();
        }

        // If hit something, play Hit particle on surface.
        if (GetHitPosition(attackArea) != Vector2.zero)
        {
            Instantiate(player.util.particles.environmentHit, GetHitPosition(attackArea), Quaternion.identity);
        }
        
        attacking = false;
    }

    // Function to make player float after an attack with Dash force.
    private async void AttackHangTime()
    {
        float timer = attackDashHangtime;

        while (timer > 0f)
        {
            player.rigidBody.velocity = Vector2.right * player.rigidBody.velocity.x;

            timer -= Time.deltaTime;
            await Task.Yield();
        }
    }

    // Function to perform Dash attack.
    private void DashAttack()
    {
        DirectionUpdater();

        player.physicalFormController.animator.Play("Anim_Physical_DashAttack");
        currentComboDuration = player.physicalFormController.animator.GetCurrentAnimatorStateInfo(0).length + 0.1f;

        attacking = true;
        dashAttack = false;
        previousCombo = comboAmount;
        comboAmount = 4;

        ResetAttackTimer();

        currentAttackDelay = dashAttackDelay;
    }

    // Function to get edge position after an attack with Dash force.
    private Vector2 GetAttackDashEdge(Vector2 direction, float dashForce)
    {
        // Increase distance by 0.5 unit until reaches dashForce value.
        for (float i = 0f; i < dashForce; i += 0.5f)
        {
            Vector2 position = (Vector2) transform.position + direction * (i + 0.5f);

            bool ground = false;

            foreach (RaycastHit2D hit in Physics2D.RaycastAll(position, Vector2.down, 0.65f))
            {
                if (!hit.collider)
                {
                    continue;
                }

                if (hit.collider.CompareTag("Ground") || hit.collider.CompareTag("FragileGround"))
                {
                    ground = true;
                    break;
                }
            }

            // If there's no ground at that position, return the previous position.
            if (!ground)
            {
                return (Vector2)transform.position + direction * i;
            }
        }

        return (Vector2)transform.position + direction * dashForce;
    }

    // Function to get hit position when attack.
    private Vector2 GetHitPosition(AttackArea attackArea)
    {
        Vector2 direction = Vector2.zero;
        float distance = 0f;

        switch (attackArea.attackDirection)
        {
            case AttackDirection.LEFT:
            direction = Vector2.left;
            distance = Mathf.Abs(attackArea.colliderOffset.x) + Mathf.Abs(attackArea.colliderSize.x / 2f);
            break;

            case AttackDirection.RIGHT:
            direction = Vector2.right;
            distance = Mathf.Abs(attackArea.colliderOffset.x) + Mathf.Abs(attackArea.colliderSize.x / 2f);
            break;

            case AttackDirection.UP:
            direction = Vector2.up;
            distance = Mathf.Abs(attackArea.colliderOffset.y) + Mathf.Abs(attackArea.colliderSize.y / 2f);
            break;
        }

        RaycastHit2D hit;
        
        hit = Physics2D.Raycast(transform.position, direction, distance, LayerMask.GetMask("Ground"));

        if (hit.collider != null)
        {
            return hit.point;
        }

        return Vector2.zero;
    }

    // Function to perform Twisted Dive Ability.
    private IEnumerator TwistedDive()
    {
        player.physicalFormController.animator.Play("Anim_Physical_TwistedDive_Float");

        player.playerState = PlayerState.ABILITY;
        player.rigidBody.constraints = RigidbodyConstraints2D.FreezeAll;

        yield return new WaitForSeconds(airTime);

        player.physicalFormController.animator.Play("Anim_Physical_TwistedDive_Fall");

        player.rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        player.rigidBody.velocity = Vector2.down * speed;
        isPerformingTwistedDive = true;
    }

    // Function that execute while Twisted Dive is performing.
    private void TwistedDiveHelper()
    {
        // Check if the Twisted Dive action is still going.
        if (isPerformingTwistedDive)
        {

            player.rigidBody.velocity = Vector2.down * speed;
            Vector2 point = (Vector2) player.transform.position + (Vector2.down * player.transform.localScale.y / 2f);
            Vector2 size = new Vector2(player.transform.localScale.x / 2f, 2f);

            List<Collider2D> collider = Physics2D.OverlapBoxAll(point, size, 0).ToList();

            // If player is doing Twisted Dive and touching Fragile ground, destroy it.
            foreach (Collider2D col in collider)
            {
                if (col.CompareTag("FragileGround"))
                {

                    Instantiate(player.util.particles.fragilePlatformBroke, col.transform.position, Quaternion.identity);
                    Destroy(col.gameObject);
                    return;
                }

                Prop prop;

                if (col.TryGetComponent(out prop))
                {
                    prop.TakeHit();
                }

                OutputInteractable interactable;

                if (col.TryGetComponent(out interactable))
                {
                    interactable.SetActivate(true);
                }
            }
        }

        // Check if the Twisted Dive action is finished.
        if (isPerformingTwistedDive && player.physicalFormController.IsGrounded())
        {
            player.physicalFormController.animator.Play("Anim_Physical_TwistedDive_Land");
            twistedDiveFB.PlayFeedbacks();

            twistedDiveCooldownCounter = twistedDiveCooldown;
            isPerformingTwistedDive = false;

            float additionalRange = (twistedDiveHitboxRadius * abilityUpgradeManager.GetAbility(AbilityType.TWISTED_DIVE).GetTotalModifyValue(AbilityModifyType.RANGE)) / 100f;
            float additionalDamage = (twistedDiveDamage * abilityUpgradeManager.GetAbility(AbilityType.TWISTED_DIVE).GetTotalModifyValue(AbilityModifyType.DAMAGE)) / 100f;

            // Play Twisted Dive impact particle.
            Instantiate(player.util.particles.twistedDiveInnerImpact, transform.position, Quaternion.identity);
            GameObject twistedDiveOuter = Instantiate(player.util.particles.twistedDiveOuterImpact, transform.position, Quaternion.identity);
            MainModule novaParticle = twistedDiveOuter.transform.Find("Nova").GetComponent<ParticleSystem>().main;
            novaParticle.startSize = (twistedDiveHitboxRadius + additionalRange) * 2f;

            TwistedDiveCollider(twistedDiveDamage + additionalDamage + player.soulFlyDamage, twistedDiveHitboxRadius + additionalRange, diveKnockbackForce, Vector2.left);
            TwistedDiveCollider(twistedDiveDamage + additionalDamage + player.soulFlyDamage, twistedDiveHitboxRadius + additionalRange, diveKnockbackForce, Vector2.right);

            /*

            Vector2 colliderPosition = (Vector2) player.transform.position + diveColliderOffset;
            Collider2D[] collided = Physics2D.OverlapCircleAll(colliderPosition, twistedDiveHitboxRadius + additionalRange);

            foreach (Collider2D col in collided)
            {
                Entity entity;

                if (col.TryGetComponent(out entity) && col.CompareTag("Enemy"))
                {
                    entity.TakeDamage(new Damage(twistedDiveDamage + additionalDamage + player.soulFlyDamage, 0f), player.gameObject);
                    entity.Knockback(player.transform.position, Vector2.one * diveKnockbackForce);
                }

                Prop prop;

                if (col.TryGetComponent(out prop))
                {
                    prop.TakeHit();
                }

                Interactable interactable;

                if (col.TryGetComponent(out interactable))
                {
                    interactable.SetActivate(true);
                }
            }

            */

            VirtualCamera.instance.CameraShake(CameraEventName.TWISTED_DIVE);

            StartCoroutine(TwistedDiveHelper2());
        }
    }

    private void TwistedDiveCollider(float damage, float radius, float force, Vector2 dir)
    {
        GameObject twistedDiveCollider = Instantiate(twistedDivePrefab, player.transform.position, Quaternion.identity);
        TwistedDiveBullet component = twistedDiveCollider.GetComponent<TwistedDiveBullet>();
        component.SetDistance(radius);
        component.SetColliderSize(twistedDiveColliderSize);
        component.SetDamage(damage);
        component.SetDirection(dir);
        component.SetPlayerTransform(player.transform.position);
        component.SetKnockback(force);
        component.SetSpeed(hithoxTravelSpeed);
    }

    // Function to finish Twisted Dive Ability.
    private IEnumerator TwistedDiveHelper2()
    {
        yield return new WaitForSeconds(diveStaggerDuration);
        player.playerState = PlayerState.WALK;
    }

    // Function to perform Circle Twirl Ability.
    private IEnumerator CircleTwirl()
    {
        // Create Circle Twirl particle.
        GameObject circleTwirlGroundParticle = Instantiate(player.util.particles.circleTwirlGroundArea, transform.position, Quaternion.identity);
        MainModule waveParticle = circleTwirlGroundParticle.transform.Find("Waves").GetComponent<ParticleSystem>().main;
        waveParticle.startSize = circleTwirlHitbox.x * 2.5f;

        player.playerState = PlayerState.ABILITY;
        player.rigidBody.velocity = Vector2.zero;
        player.rigidBody.constraints = RigidbodyConstraints2D.FreezeAll;

        int additionalHitTime = (int) abilityUpgradeManager.GetAbility(AbilityType.CIRCLE_TWIRL).GetTotalModifyValue(AbilityModifyType.AMOUNT);
        float additionalDamage = (circleTwirlDamage * abilityUpgradeManager.GetAbility(AbilityType.CIRCLE_TWIRL).GetTotalModifyValue(AbilityModifyType.DAMAGE)) / 100f;
        
        for (int i = 0; i < hitTimes + additionalHitTime; i++)
        {
            Collider2D[] collided = Physics2D.OverlapBoxAll(player.transform.position, circleTwirlHitbox, 0f);

            foreach (Collider2D col in collided)
            {
                Entity entity;

                if (col.TryGetComponent(out entity) && col.CompareTag("Enemy"))
                {
                    entity.TakeDamage(new Damage(circleTwirlDamage + additionalDamage + player.soulFlyDamage, 0f), player.gameObject);
                    entity.Knockback(player.transform.position, Vector2.one * knockbackDistance);
                }

                Prop prop;

                if (col.TryGetComponent(out prop))
                {
                    prop.TakeHit();
                }

                OutputInteractable interactable;

                if (col.TryGetComponent(out interactable))
                {
                    interactable.SetActivate(true);
                }
            }

            // SpriteDebugger.instance.CreateRectangle(player.transform.position, circleTwirlHitbox, 1f);

            yield return new WaitForSeconds(circleTwirlFrequency);
        }

        // Stop the particle loop.
        for (int i = 0; i < circleTwirlGroundParticle.transform.childCount; i++)
        {
            MainModule circleTwirlGroundMain = circleTwirlGroundParticle.transform.GetChild(i).GetComponent<ParticleSystem>().main;
            circleTwirlGroundMain.loop = false;
        }

        if (player.physicalFormController.IsGrounded())
        {
            player.physicalFormController.animator.Play("Movement");
        }
        else
        {
            player.physicalFormController.animator.Play("Anim_Physical_Falling");
        }

        player.playerState = PlayerState.WALK;

        player.rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        player.rigidBody.AddForce(Vector2.down * player.rigidBody.gravityScale);

    }

    // Function to update timer of each variable.
    private void TimerUpdater()
    {
        if (currentAttackDuration > 0)
        {
            currentAttackDuration -= Time.deltaTime;
        }
        else
        {
            if (comboAmount > 0)
            {
                currentAttackComboDuration = 0f;
            }
        }

        // Check if there's attack combo duration.
        if (currentAttackComboDuration > 0)
        {
            currentAttackComboDuration -= Time.deltaTime;
        }
        else
        {
            if (player.playerState != PlayerState.ABILITY)
            {
                player.playerState = PlayerState.WALK;
            }
        }

        // Check if there's attack delay.
        if (currentAttackDelay > 0)
        {
            currentAttackDelay -= Time.deltaTime;
        }

        // Check if there's combo chain.
        if (comboAmount > 0)
        {
            if (currentComboDuration > 0)
            {
                currentComboDuration -= Time.deltaTime;
            }
            else
            {
                // Reset combo.
                comboAmount = 0;
                currentComboDuration = comboDuration;
            }
        }

        twistedDiveCooldownCounter -= Time.deltaTime;
        twistedDiveCooldownCounter = Mathf.Clamp(twistedDiveCooldownCounter, 0f, twistedDiveCooldown);

        circleTwirlCooldownCounter -= Time.deltaTime;
        circleTwirlCooldownCounter = Mathf.Clamp(circleTwirlCooldownCounter, 0f, circleTwirlCooldown);
    }

    public bool GetIsPerformingTwistedDive() {
        return isPerformingTwistedDive;
    }

    // Function to update attack direction.
    private void DirectionUpdater()
    {
        if (player.playerState == PlayerState.ATTACK)
        {
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        if (x < 0f)
        {
            attackDirection = AttackDirection.LEFT;
        }

        if (x > 0f)
        {
            attackDirection = AttackDirection.RIGHT;
        }

        // If player doesn't move, attack toward facing direction (LEFT or RIGHT).
        if (x == 0f)
        {
            if (player.LookRight())
            {
                attackDirection = AttackDirection.RIGHT;
            }
            else
            {
                attackDirection = AttackDirection.LEFT;
            }
        }

        if (y > 0f)
        {
            attackDirection = AttackDirection.UP;
        }
    }

    // Event that execute when player stop Dashing.
    public async void EndDash()
    {
        if (dashAttack)
        {
            DashAttack();
        }
        else
        {
            // Check if player has already unlock Dashing abilities Lv.2 (Ability to perform Dash Attack).
            if (!abilityUpgradeManager.GetAbility(AbilityType.DASH).abilityLevel[1].purchased)
            {
                return;
            }

            float timer = dashAttackAfter;

            while (timer > 0f)
            {
                // If player attack after dash while in dashAttackAfter timer, player can still perfrom Dash attack.
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    DashAttack();
                    break;
                }

                timer -= Time.deltaTime;
                await Task.Yield();
            }
        }
    }

    // Function to check min height.
    private bool HasAtLeastMinHeight()
    {
        RaycastHit2D hit = Physics2D.Raycast(player.transform.position, Vector2.down, minHeight, groundLayer);

        Vector2 center = (Vector2) player.transform.position + diveColliderOffset - (Vector2.up * minHeight / 2f);
        //SpriteDebugger.instance.CreateRectangle(center, new Vector2(1f, minHeight), 1f);

        return hit.collider == null;
    }

    // Function to set Attack Direction.
    public void SetAttackDirection(AttackDirection direction)
    {
        attackDirection = direction;
    }

    // Function to find specific attack.
    public AttackArea GetAttack()
    {
        foreach (AttackArea attackArea in attackAreaList)
        {
            if (attackArea.attackDirection == attackDirection && attackArea.combo == comboAmount)
            {
                return attackArea;
            }
        }

        return null;
    }

    // Function to get combo amount.
    public int GetCombo()
    {
        return comboAmount;
    }

    // Function to set combo amount;
    public void SetCombo(int value)
    {
        comboAmount = value;
    }

    // Function to get combo cooldown.
    public float GetComboCooldown()
    {
        return currentComboDuration;
    }

    // Function to get Twisted Dive cooldown.
    public float GetTwistedDiveCooldown()
    {
        return twistedDiveCooldownCounter;
    }

    // Function to get Twisted Dive cooldown.
    public float GetCircleTwirlCooldown()
    {
        return circleTwirlCooldownCounter;
    }

    // Function to set combo cooldown.
    public void SetComboCooldown(float comboCooldown)
    {
        currentComboDuration = comboCooldown;
    }

    public AttackDirection GetAttackDirection() {
        return attackDirection;
    }

    [Serializable]
    public class AttackArea
    {
        public string attackName;
        public AttackDirection attackDirection;
        public int combo;
        public float attackDelay = 0.2f; // Attack delay between each attack.
        public float attackDamage; // Attack damage.
        public float dashForce; // Dash force toward direction.
        public Vector2 knockbackForce = Vector2.zero; // Knockback force toward enemy.
        [Space(5)]
        public Vector2 colliderOffset = Vector2.zero; // Attack area offset.
        public Vector2 colliderSize = Vector2.one; // Attack area size.
        [Space(5)]
        public Vector2 particleOffset = Vector2.zero; // Offset position to spawn attack particle.
        [Space(5)]
        [ColorUsage(true, true)] public Color attackEffectColor = Color.white; // Attack Effect color.
        [Space(5)]
        public bool showAttackArea = false; // Determine to show Attack area or not.
    }

    private void OnDrawGizmos()
    {
        foreach (AttackArea attackArea in attackAreaList)
        {
            if (attackArea.showAttackArea)
            {
                Vector2 center = (Vector2)transform.position + attackArea.colliderOffset;
                Vector2 particleCenter = (Vector2)transform.position + attackArea.particleOffset;

                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(center, attackArea.colliderSize);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(particleCenter, 0.5f);
            }
        }
    }
}
