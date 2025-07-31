using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using static Entity;
using static Player;
using static Ability;
using static UnityEngine.ParticleSystem;

public class SpiritualFormCombat : MonoBehaviour
{
    [Header("Auto Attack Combo")]
    [SerializeField] private float smallBulletSpeed; // Speed of Bullet.
    [SerializeField] private float smallBulletDamage; // Damage of Bullet.
    [SerializeField] private float betweenComboTime; // Delay between each combo.
    [SerializeField] private float resetComboTime; // Duration of combo before reset.
    [SerializeField] private float specialBulletSpeed; // Speed of Special Bullet.
    [SerializeField] private float specialBulletDamage; // Damage of Special Bullet.
    [SerializeField] private float specialBulletInitSize; // Initial size of Special Bullet.
    [SerializeField] private float specialBulletInitSpeed; // Initial speed of Special Bullet.
    [SerializeField] private float expoDecayRate; // Exponential rate to Decay.
    [SerializeField] private float expoIncreaseRate; // Exponential rate to Increase.
    [SerializeField] private float maxSize; // Maximum size of Special Bullet.
    [SerializeField] private float minSpeed; // Minimum speed of Special Bullet.
    [SerializeField] private float maxDistance; // Maximum distance of Special Bullet.
    
    [Header("Teleportation")]
    [SerializeField] private float markRange = 3f; // Range that player can teleport to when do Attack.
    [SerializeField] private float markOffset = 1f; // Offset of mark from enemy's position.

    [Header("Spiritual Resonance")]
    [SerializeField] private float spiritualResonanceHitbox; // Radius of Spiritual Resonance's explosion.
    [SerializeField] private float spiritualResonanceDuration; // Spiritual Resonance Charge duration.
    [SerializeField] private float spiritualResonanceDamage; // Spiritual Resonance damages.
    [SerializeField] private float spiritualResonanceShakeRange = 1f; // Spiritual Resonance shake distance.

    [Header("Spirit's Calling")]
    [SerializeField] private float dashSpeed; // Dash speed.
    [SerializeField] private float dashDuration; // Dash duration.
    [SerializeField] private float barrierSize; // Size of Barrier.
    [SerializeField] private float dashDamage; // Dash damages.
    public float dashCooldown; // Dash cooldown.

    [Header("Dash + Resonance Combo")]
    [SerializeField] private float comboTimeThreshold;

    [Header("References")]
    [SerializeField] private GameObject bulletPrefab; // Spiritual Bullet prefab.
    [SerializeField] private GameObject specialBulletPrefab; // Special Spiritual Bullet prefab.
    [SerializeField] private GameObject markPrefab; // Mark prefab;

    private int comboCount; // Amount of combo.
    private float sinceLastAttack; // Current Combo in-between duration.
    private float cooldownCounter; // Cooldown counter.
    private float currentChargeDuration; // Current charge duration.
    private bool isHoldingSpiritualResonance; // Determine if player is using Spiritual Resonance.
    private float currentDashCooldown; // Current Dash cooldown.
    private float currentDashDuration; // Current Dash duration.
    private bool isDashing; // Determine if player is dashing.
    private Vector2 dashDirectionalVector; // Dash direction.
    private GameObject currentSpiritualResonanceParticle; // Spiritual Resonance particle.
    private float currentJustOutOfDashCounter; // Just dash counter.
    private bool justOutOfDash; // Determine if player just dashed.
    private bool wasDashing; // Determine if player was dashing.
    private float soulFlyDamage = 0; // Amount of Soul Fly's Latern's damage.
    private bool hasMark; // Determine if there's mark on any enemy.
    private GameObject markOn; // Current mark Object.
    private Vector2 savedPosition; // Saved position while doing Spiritual Resonance.

    public Player player;
    private AbilityUpgradeManager abilityUpgradeManager;

    [HideInInspector] public GameObject currentMark; // Current Mark prefab.

    [Header("SFXs")]
    public MMFeedbacks shootSpiritualBulletSFX;
    public MMFeedbacks spiritCallingSFX;
    public MMFeedbacks spiritCallingHitEnemySFX;
    public MMFeedbacks spiritCallingDeflectingSFX;
    public MMFeedbacks preResonanceExplosionSFX;
    private void Start()
    {
        player = instance;
        abilityUpgradeManager = UpgradeManager.instance.abilityUpgradeManager;

        spiritualResonanceDamage += player.GetStat("Magic Power");
        dashDamage += player.GetStat("Magic Power");

        currentJustOutOfDashCounter = 1;
    }

    private void Update()
    {
        if (player == null) player = Player.instance;
        if (player.entityState == EntityState.DEAD || player.HasEffect("Stun"))
        {
            // Cancel Spiritual Resonance.
            if (isHoldingSpiritualResonance)
            {
                // If the particle is playing, destroy it.
                if (currentSpiritualResonanceParticle)
                {
                    MainModule chargingParticle = currentSpiritualResonanceParticle.GetComponent<ParticleSystem>().main;
                    chargingParticle.loop = false;

                    for (int i = 0; i < currentSpiritualResonanceParticle.transform.childCount; i++)
                    {
                        MainModule otherParticle = currentSpiritualResonanceParticle.transform.GetChild(i).GetComponent<ParticleSystem>().main;
                        otherParticle.loop = false;
                    }
                }

                isHoldingSpiritualResonance = false;
                player.playerState = PlayerState.WALK;
                currentChargeDuration = 0f;
            }

            return;
        }

        AutoAttack();
        SpiritualResonance();

        // Ability - Spirit Calling.
        // Check if player has already unlock Spiritual's Calling abilities Lv.1 (Perform this ability).
        if (abilityUpgradeManager.GetAbility(AbilityType.SPIRIT_CALLING).unlocked)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) && currentDashCooldown <= 0 && !isDashing)
            {
                spiritCallingSFX.Initialization();
                spiritCallingSFX.PlayFeedbacks();

                float inputHorizontal = Input.GetAxisRaw("Horizontal");
                float inputVertical = Input.GetAxisRaw("Vertical");

                Vector2 unitVector = new Vector2(inputHorizontal, inputVertical).normalized;

                if (Vector2.SqrMagnitude(unitVector) == 0)
                {
                    return;
                }

                // Play Spirit Calling's shield particle.
                GameObject spiritCallingShield = Instantiate(player.util.particles.spiritCallingShield, transform.position, Quaternion.identity);
                spiritCallingShield.transform.SetParent(player.transform);

                MainModule shieldParticle = spiritCallingShield.GetComponent<ParticleSystem>().main;
                shieldParticle.startSize = barrierSize * 2f;
                shieldParticle.startLifetime = dashDuration;

                MainModule sparklesParticle = spiritCallingShield.GetComponent<ParticleSystem>().main;
                sparklesParticle.startLifetime = dashCooldown;

                ShapeModule sparklesShapeParticle = spiritCallingShield.GetComponentInChildren<ParticleSystem>().shape;
                sparklesShapeParticle.radius = barrierSize;

                player.playerState = PlayerState.ABILITY;
                isDashing = true;
                dashDirectionalVector = unitVector; // Set direction of the dash.
                currentDashDuration = dashDuration; // Set temp cooldown.
                wasDashing = true;

                print("CHARGE");
                player.spiritualFormController.animator.Play("Anim_Spiritual_SpiritCalling_PreCharge");

                // currentSpiritCallBarrier = SpriteDebugger.instance.CreateCircle(player.transform.position, barrierSize, dashCooldown);
            }
            else
            {
                currentDashDuration -= Time.deltaTime;
            }
        }

    }

    private void FixedUpdate()
    {
        if (player.entityState == EntityState.DEAD)
        {
            player.rigidBody.velocity = Vector2.zero;
            return;
        }

        if (player.HasEffect("Stun"))
        {
            player.rigidBody.velocity = Vector2.zero;
            return;
        }

        SpiritCalling();

        if (currentJustOutOfDashCounter >= comboTimeThreshold)
        {
            justOutOfDash = false;
        } 

        currentJustOutOfDashCounter += Time.deltaTime;
        currentJustOutOfDashCounter = Mathf.Clamp(currentJustOutOfDashCounter, 0, comboTimeThreshold);
    }

    // Function of Auto Attack.
    public void AutoAttack()
    {
        // [Right Mouse Button] - Shoot Spiritual Bullet.
        if (Input.GetMouseButtonDown(1) && cooldownCounter <= 0f && player.playerState == PlayerState.WALK)
        {
            shootSpiritualBulletSFX.Initialization();
            shootSpiritualBulletSFX.PlayFeedbacks();

            // Play Spiritual Attack particle.
            GameObject spiritualAttack = Instantiate(player.util.particles.spiritualAttack, transform.position, Quaternion.identity);
            spiritualAttack.transform.SetParent(player.transform);

            Vector3 mouseClickPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 directionalVector = (mouseClickPosition - transform.position); // Find the trajectory vector.
            Vector2 unitVector = directionalVector / Mathf.Sqrt(Mathf.Pow(directionalVector.x, 2f) + Mathf.Pow(directionalVector.y, 2f)); // get unit vec

            bool shouldFlip = mouseClickPosition.x > player.transform.position.x;
            player.spriteRenderer.flipX = mouseClickPosition.x <= player.transform.position.x;

            // If player is combo time reaches, reset combo back to 0.
            if (sinceLastAttack >= resetComboTime) {
                comboCount = 0;
                sinceLastAttack = 0;
            }

            UtilParticle particle = player.util.particles;

            // Check if player has already unlock Spiritual Combo abilities Lv.1 (Ability to perform Combo).
            if (abilityUpgradeManager == null) abilityUpgradeManager = UpgradeManager.instance.abilityUpgradeManager;
            if (!abilityUpgradeManager.GetAbility(AbilityType.SPIRITUAL_COMBO).unlocked)
            {
                Shoot(unitVector, false, particle.spiritualBullet1, particle.spiritualBullet1Hit);
                sinceLastAttack = 0;
                player.spiritualFormController.animator.Play("Anim_Spiritual_Attack_1_2");
            }
            else
            {
                // Shoot Spiritual bullet depends on combo sequences.
                switch (comboCount)
                {
                    case 0:
                    comboCount++;
                    Shoot(unitVector, false, particle.spiritualBullet1, particle.spiritualBullet1Hit);
                    sinceLastAttack = 0f;
                    player.spiritualFormController.animator.Play("Anim_Spiritual_Attack_1_2");
                    break;

                    case 1:
                    comboCount++;

                    // Check if player has already unlock Spiritual Combo abilities Lv.3 (Ability to mark enemy).
                    Shoot(unitVector, abilityUpgradeManager.GetAbility(AbilityType.SPIRITUAL_COMBO).abilityLevel[2].purchased, particle.spiritualBullet2, particle.spiritualBullet2Hit);

                    // Check if player has already unlock Spiritual Combo abilities Lv.2 (Ability to shoot projectile backward).
                    if (abilityUpgradeManager.GetAbility(AbilityType.SPIRITUAL_COMBO).abilityLevel[1].purchased)
                    {
                        Shoot(-1f * unitVector, false, particle.spiritualBullet2, particle.spiritualBullet2Hit);
                    }

                    sinceLastAttack = 0f;
                    player.spiritualFormController.animator.Play("Anim_Spiritual_Attack_1_2");
                    break;

                    case 2:
                    // Check if player has already unlock Spiritual Combo abilities Lv.4 (Ability to shoot projectile as penetration).
                    bool shouldGoThrough = abilityUpgradeManager.GetAbility(AbilityType.SPIRITUAL_COMBO).abilityLevel[3].purchased;

                    comboCount = 0;
                    ShootSpecial(unitVector, specialBulletInitSize, specialBulletInitSpeed, shouldFlip, true, shouldGoThrough, particle.spiritualBullet3, particle.spiritualBullet3Hit);

                    // Check if player has already unlock Spiritual Combo abilities Lv.2 (Ability to shoot projectile backward).
                    if (abilityUpgradeManager.GetAbility(AbilityType.SPIRITUAL_COMBO).abilityLevel[1].purchased)
                    {
                        ShootSpecial(-1f * unitVector, specialBulletInitSize / 2f, specialBulletInitSpeed, !shouldFlip, false, shouldGoThrough, particle.spiritualBullet3Back, particle.spiritualBullet3BackHit);
                    }

                    sinceLastAttack = 0f;
                    player.spiritualFormController.animator.Play("Anim_Spiritual_Attack_3");
                    break;

                    default:
                    break;
                }
            }

            cooldownCounter = betweenComboTime;
        }

        cooldownCounter -= Time.deltaTime;
        sinceLastAttack += Time.deltaTime;
        sinceLastAttack = Mathf.Clamp(sinceLastAttack, 0f, resetComboTime);
    }

    // Function to shoot a spiritual bullet.
    public void Shoot(Vector2 unitVector, bool shouldMark, GameObject bulletParticle, GameObject bulletHitParticle)
    {
        GameObject projectile = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet bullet = projectile.GetComponent<Bullet>();

        bullet.SetHitParticle(bulletHitParticle);
        bullet.SetShouldMark(shouldMark);
        bullet.SetBulletSpeed(smallBulletSpeed);
        bullet.SetDamage((int)smallBulletDamage);
        bullet.SetDirection(unitVector);

        // Spawn bullet particle.
        GameObject particle = Instantiate(bulletParticle, projectile.transform);
        particle.transform.localPosition = Vector3.zero;
        particle.transform.up = unitVector;

        MainModule main = particle.GetComponent<ParticleSystem>().main;
        MinMaxCurve mainRotation = main.startRotation;
        mainRotation.constant -= particle.transform.eulerAngles.z * Mathf.PI / 180f;
        main.startRotation = mainRotation;

        for (int i = 0; i < particle.transform.childCount; i++)
        {
            MainModule second = particle.transform.GetChild(i).GetComponent<ParticleSystem>().main;
            second.startRotation = mainRotation;
        }
    }

    // Function to shoot a special spiritual bullet.
    public void ShootSpecial(Vector2 unitVector, float size, float speed, bool shouldFlip, bool isFirst, bool goThrough, GameObject bulletParticle, GameObject bulletHitParticle)
    {
        GameObject projectile = Instantiate(specialBulletPrefab, transform.position, new Quaternion());

        if (projectile != null) {
            SpecialBullet specialBullet = projectile.GetComponent<SpecialBullet>();

            specialBullet.SetBulletSpeed(specialBulletSpeed);
            specialBullet.SetDamage((int)specialBulletDamage);
            specialBullet.SetDirection(unitVector);
            specialBullet.SetInitSize(size);
            specialBullet.SetInitSpeed(speed);
            specialBullet.SetOrientation(shouldFlip);
            specialBullet.SetRotation(Mathf.Atan(unitVector.y / unitVector.x));
            specialBullet.SetMaxSize(maxSize);
            specialBullet.SetMaxDistance(maxDistance);

            if (isFirst) {
                specialBullet.SetShouldGoThrough(goThrough);
                specialBullet.SetExpoDecaySpeedRate(expoDecayRate);
                specialBullet.SetExpoIncreaseSizeRate(expoIncreaseRate);
                specialBullet.SetMinSpeed(minSpeed);
            } else {
                specialBullet.SetRotation(Mathf.Atan(unitVector.y / unitVector.x));
                specialBullet.SetExpoDecaySpeedRate(0f);
                specialBullet.SetExpoIncreaseSizeRate(0f);
            }
        }

        // Spawn bullet particle.
        GameObject particle = Instantiate(bulletParticle, projectile.transform);
        particle.transform.localPosition = Vector3.zero;
        particle.transform.up = unitVector;

        MainModule main = particle.GetComponent<ParticleSystem>().main;
        MinMaxCurve mainRotation = main.startRotation;
        mainRotation.constant -= particle.transform.eulerAngles.z * Mathf.PI / 180f;
        main.startRotation = mainRotation;

        for (int i = 0; i < particle.transform.childCount; i++)
        {
            MainModule second = particle.transform.GetChild(i).GetComponent<ParticleSystem>().main;
            MinMaxCurve secondSize = second.startSize;
            secondSize = projectile.transform.localScale.x * 30f;
            second.startSize = secondSize;
            second.startRotation = mainRotation;
        }
    }

    // Function to perform Spiritual Resonance.
    public void SpiritualResonance()
    {
        // Check if player has already unlock Spiritual's Resonance abilities Lv.1 (Perform this ability).
        if (!abilityUpgradeManager.GetAbility(AbilityType.SPIRITUAL_RESONANCE).unlocked)
        {
            return;
        }

        float additionalRange = (spiritualResonanceHitbox * abilityUpgradeManager.GetAbility(AbilityType.SPIRITUAL_RESONANCE).GetTotalModifyValue(AbilityModifyType.RANGE)) / 100f;
        float additionalDamage = (spiritualResonanceDamage * abilityUpgradeManager.GetAbility(AbilityType.SPIRITUAL_RESONANCE).GetTotalModifyValue(AbilityModifyType.DAMAGE)) / 100f;

        // Check if player has already unlock Spirit's Call abilities Lv.4 (Use Spiritual's Resonance after Spirit's Call without charging).
        if (abilityUpgradeManager.GetAbility(AbilityType.SPIRIT_CALLING).abilityLevel[3].purchased)
        {
            if (Input.GetKey(KeyCode.E) && justOutOfDash)
            {
                // If the charging particle is playing, destroy it.
                if (currentSpiritualResonanceParticle)
                {
                    MainModule chargingParticle = currentSpiritualResonanceParticle.GetComponent<ParticleSystem>().main;
                    chargingParticle.loop = false;

                    for (int i = 0; i < currentSpiritualResonanceParticle.transform.childCount; i++)
                    {
                        MainModule otherParticle = currentSpiritualResonanceParticle.transform.GetChild(i).GetComponent<ParticleSystem>().main;
                        otherParticle.loop = false;
                    }
                }

                player.spiritualFormController.animator.Play("Anim_Spiritual_Resonance_Explosion");

                // Play Spiritual Resonance explosion particle.
                Instantiate(player.util.particles.spiritualResonanceInnerExplosion, transform.position, Quaternion.identity);
                GameObject explosion = Instantiate(player.util.particles.spiritualResonanceOuterExplosion, transform.position, Quaternion.identity);
                MainModule explosionParticle = explosion.GetComponent<ParticleSystem>().main;
                explosionParticle.startSize = (spiritualResonanceHitbox + additionalRange) * 5.2f;

                additionalDamage = (spiritualResonanceDamage * abilityUpgradeManager.GetAbility(AbilityType.SPIRIT_CALLING).GetTotalModifyValue(AbilityModifyType.DAMAGE)) / 100f;

                Collider2D[] collided = Physics2D.OverlapCircleAll(player.transform.position, spiritualResonanceHitbox + additionalRange);

                foreach (Collider2D col in collided)
                {
                    // Deal damages to enemy.
                    Entity entity;

                    if (col.TryGetComponent(out entity) && col.CompareTag("Enemy"))
                    {
                        entity.TakeDamage(new Damage(0f, spiritualResonanceDamage + additionalDamage + (int)soulFlyDamage), gameObject);
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

                player.playerState = PlayerState.WALK;

                currentChargeDuration = 0f;
                currentJustOutOfDashCounter = 1;

                isHoldingSpiritualResonance = false;
                justOutOfDash = false;

                player.SetStat("Mana", 0f); // Set Mana to 0.
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            VirtualCamera.instance.SRRumblingSFX.PlayFeedbacks();
        }

        if (Input.GetKey(KeyCode.E))
        {
            if (!isHoldingSpiritualResonance)
            {
                // If the particle is already playing, destroy the old one.
                if (currentSpiritualResonanceParticle)
                {
                    MainModule chargingParticle = currentSpiritualResonanceParticle.GetComponent<ParticleSystem>().main;
                    chargingParticle.loop = false;

                    for (int i = 0; i < currentSpiritualResonanceParticle.transform.childCount; i++)
                    {
                        MainModule otherParticle = currentSpiritualResonanceParticle.transform.GetChild(i).GetComponent<ParticleSystem>().main;
                        otherParticle.loop = false;
                    }
                }

                // Play Spiritual Resonance Charging particle.
                currentSpiritualResonanceParticle = Instantiate(player.util.particles.spiritualResonanceCharging, transform.position, Quaternion.identity);

                savedPosition = player.transform.position;
                player.spiritualFormController.animator.Play("Anim_Spiritual_Resonance_Precharge");
            }

            // Make player shake while charging.
            float randomDistance = Random.Range(-spiritualResonanceShakeRange, spiritualResonanceShakeRange);
            Vector2 offset = Random.insideUnitCircle * randomDistance;
            player.transform.position = savedPosition + offset;

            player.playerState = PlayerState.ABILITY;
            currentChargeDuration += Time.deltaTime;
            isHoldingSpiritualResonance = true;
            VirtualCamera.instance.CameraShake(CameraEventName.SPIRIT_RESONANCE);
        }
        else
        {
            if (savedPosition != Vector2.zero)
            {
                player.transform.position = savedPosition;
                savedPosition = Vector2.zero;
            }

            // If the player release before explosion, reset value.
            if (isHoldingSpiritualResonance)
            {
                player.spiritualFormController.animator.Play("Movement");

                // If the particle is playing, destroy it.
                if (currentSpiritualResonanceParticle)
                {
                    MainModule chargingParticle = currentSpiritualResonanceParticle.GetComponent<ParticleSystem>().main;
                    chargingParticle.loop = false;

                    for (int i = 0; i < currentSpiritualResonanceParticle.transform.childCount; i++)
                    {
                        MainModule otherParticle = currentSpiritualResonanceParticle.transform.GetChild(i).GetComponent<ParticleSystem>().main;
                        otherParticle.loop = false;
                    }
                }

                isHoldingSpiritualResonance = false; 
                player.playerState = PlayerState.WALK;
                currentChargeDuration = 0f;
            }
        }

        float subtractionalChargeTime = (spiritualResonanceDuration * abilityUpgradeManager.GetAbility(AbilityType.SPIRITUAL_RESONANCE).GetTotalModifyValue(AbilityModifyType.TIME)) / 100f;

        if (currentChargeDuration >= spiritualResonanceDuration + subtractionalChargeTime)
        {
            // If the charging particle is playing, destroy it.
            if (currentSpiritualResonanceParticle)
            {
                MainModule chargingParticle = currentSpiritualResonanceParticle.GetComponent<ParticleSystem>().main;
                chargingParticle.loop = false;

                for (int i = 0; i < currentSpiritualResonanceParticle.transform.childCount; i++)
                {
                    MainModule otherParticle = currentSpiritualResonanceParticle.transform.GetChild(i).GetComponent<ParticleSystem>().main;
                    otherParticle.loop = false;
                }
            }

            player.SetStat("Mana", 0f);
            player.spiritualFormController.animator.Play("Anim_Spiritual_Resonance_Explosion");

            // Play Spiritual Resonance explosion particle.
            Instantiate(player.util.particles.spiritualResonanceInnerExplosion, transform.position, Quaternion.identity);
            GameObject explosion = Instantiate(player.util.particles.spiritualResonanceOuterExplosion, transform.position, Quaternion.identity);
            MainModule explosionParticle = explosion.GetComponent<ParticleSystem>().main;
            explosionParticle.startSize = (spiritualResonanceHitbox + additionalRange) * 5.2f;

            Collider2D[] collided = Physics2D.OverlapCircleAll(player.transform.position, spiritualResonanceHitbox + additionalRange);
            float finalDamage = spiritualResonanceDamage + additionalDamage + (int)soulFlyDamage;

            // Check if player has already unlock Spiritual's Resonance abilities Lv.4 (Damage enemies in screen sight and damage depends on range).
            if (abilityUpgradeManager.GetAbility(AbilityType.SPIRITUAL_RESONANCE).abilityLevel[3].purchased)
            {
                Vector2 position1 = Camera.main.ScreenToWorldPoint(new Vector2(Camera.main.pixelWidth, Camera.main.pixelHeight));
                Vector2 position2 = Camera.main.ScreenToWorldPoint(-new Vector2(Camera.main.pixelWidth, Camera.main.pixelHeight));

                collided = Physics2D.OverlapAreaAll(position1, position2);

                foreach (Collider2D col in collided)
                {
                    // Calculate damages depends on distance.
                    Vector2 width = Camera.main.ScreenToWorldPoint(Vector2.right * Screen.width);

                    float distancePlayer = Vector2.Distance(transform.position, col.transform.position);
                    float distanceMax = Vector2.Distance(transform.position, width);

                    float percentage = (distancePlayer * 100f) / distanceMax;
                    float finalEnemyDamage = finalDamage - ((percentage * finalDamage) / 100f);

                    // Deal damages to enemy.
                    Entity entity;

                    if (col.TryGetComponent(out entity) && col.CompareTag("Enemy"))
                    {
                        entity.TakeDamage(new Damage(0f, finalEnemyDamage), gameObject);
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
            else
            {
                foreach (Collider2D col in collided)
                {
                    // Deal damages to enemy.
                    Entity entity;

                    if (col.TryGetComponent(out entity) && col.CompareTag("Enemy"))
                    {
                        entity.TakeDamage(new Damage(0f, finalDamage), gameObject);
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

                SpriteDebugger.instance.CreateCircle(player.transform.position, spiritualResonanceHitbox + additionalRange, 1f);
            }

            player.playerState = PlayerState.WALK;

            currentChargeDuration = 0f;
            player.SetStat("Mana", 0f);
            isHoldingSpiritualResonance = false;
        }

        currentChargeDuration = Mathf.Clamp(currentChargeDuration, 0f, spiritualResonanceDuration);
    }

    public bool GetIsHoldingSpiritualResonance()
    {
        return isHoldingSpiritualResonance;
    }

    // Function to perform Spirit Calling.
    public void SpiritCalling()
    {
        if (isDashing)
        {
            player.rigidBody.AddForce(dashDirectionalVector * dashSpeed * 100f, ForceMode2D.Force);
            Collider2D[] collided = Physics2D.OverlapCircleAll(player.transform.position, barrierSize);

            foreach (Collider2D col in collided)
            {
                // Deal damages to enemy.
                Entity entity;

                if (col.TryGetComponent(out entity) && col.CompareTag("Enemy"))
                {
                    entity.TakeDamage(new Damage(0f, dashDamage + (int)soulFlyDamage), gameObject);
                    spiritCallingHitEnemySFX.PlayFeedbacks();
                }

                EnemyBullets enemyBullet;

                if (col.TryGetComponent(out enemyBullet))
                {
                    float additionalDeflectDamage = (enemyBullet.damage * abilityUpgradeManager.GetAbility(AbilityType.SPIRIT_CALLING).GetTotalModifyValue(AbilityModifyType.DAMAGE)) / 100f;

                    Vector2 bulletVector = enemyBullet.GetDirentionalVector(); // Get the directional vector of the bullet

                    enemyBullet.SetDirection(Vector2.one - bulletVector); // Negate the vector
                    enemyBullet.SetDamage((int)player.GetStat("Strength") + (int)additionalDeflectDamage);
                    enemyBullet.SetHasBounced(); // Make enemy bullet bounces away.

                    if (spiritCallingDeflectingSFX != null)
                        spiritCallingDeflectingSFX.PlayFeedbacks();
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
            
            wasDashing = true;
        }

        if (currentDashDuration <= 0f && wasDashing)
        {
            player.spiritualFormController.animator.Play("Movement");

            player.playerState = PlayerState.WALK;

            isDashing = false;
            justOutOfDash = true;
            wasDashing = false;

            float additionalShieldTime = (dashCooldown * abilityUpgradeManager.GetAbility(AbilityType.SPIRIT_CALLING).GetTotalModifyValue(AbilityModifyType.TIME)) / 100f;

            currentJustOutOfDashCounter = 0;
            currentDashCooldown = dashCooldown + additionalShieldTime;
        }
        else
        {
            currentDashCooldown -= Time.deltaTime;
            wasDashing = false;
        }

        currentDashDuration = Mathf.Clamp(currentDashDuration, 0f, dashDuration);
        currentDashCooldown = Mathf.Clamp(currentDashCooldown, 0f, dashCooldown);
    }

    // Function to update Mark.
    public void MarkUpdater()
    {
        // Check if there's marked enemy.
        if (markOn)
        {
            float distance = Vector2.Distance(player.transform.position, markOn.transform.position);

            // If marked enemy is far away, remove mark.
            if (distance > markRange)
            {
                markOn = null;
                hasMark = false;

                if (currentMark)
                {
                    Destroy(currentMark);
                }
            }
            else
            {
                // If mark object doesn't exist, create new one and move along with enemy.
                if (!currentMark)
                {
                    currentMark = Instantiate(markPrefab, markOn.transform.position, Quaternion.identity);
                }
                else
                {
                    Vector2 offset = (Vector2) markOn.transform.position + (Vector2.up * markOffset);
                    currentMark.transform.position = offset;
                }
            }
        }
        else
        {
            hasMark = false;

            if (currentMark)
            {
                Destroy(currentMark);
            }
        }
    }

    public bool GetIsDashing()
    {
        return isDashing;
    }

    public float GetCurrentDashCooldown()
    {
        return currentDashCooldown;
    }

    // Function to get combo amount.
    public int GetCombo()
    {
        return comboCount;
    }

    // Function to set combo amount;
    public void SetCombo(int value)
    {
        comboCount = value;
    }

    // Function to get combo cooldown.
    public float GetComboCooldown()
    {
        return sinceLastAttack;
    }

    // Function to set combo cooldown.
    public void SetComboCooldown(float comboCooldown)
    {
        sinceLastAttack = comboCooldown;
    }

    public void SetHasMark(bool to)
    {
        hasMark = to;
    }

    public void SetMarkOn(GameObject enemy)
    {
        markOn = enemy;
    }

    public bool GethasMark()
    {
        return hasMark;
    }

    public GameObject GetMarkOn()
    {
        return markOn;
    }

    public int GetComboCount()
    {
        return comboCount;
    }

    public void SetComboCount(int to)
    {
        comboCount = to;
    }

    public void ResetSavedPosition() {
        savedPosition = new Vector2(0,0);
    }

    public void PlayPreExplosionSound()
    {
        preResonanceExplosionSFX.PlayFeedbacks();
    }
}

