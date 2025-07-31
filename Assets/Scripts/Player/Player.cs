using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using MoreMountains.Feedbacks;
using static Ability;
using static Item;
using System.Linq;
public class Player : Entity
{
    public static Player instance;

    public enum PlayerState
    {
        WALK, ATTACK, ABILITY
    }

    [Header("Player Settings")]
    public PlayerState playerState = PlayerState.WALK;

    [Space(10)]
    public float transformCooldown = 1f; // Cooldown to switching form.
    public float invincibleDuration = 1f; // Invincible duration after taken damages.

    [Header("References")]
    public CapsuleCollider2D colliders; // Player's Collider.
    public PhysicalFormController physicalFormController;
    public PhysicalFormCombat physicalFormCombat;
    public SpiritualFormController spiritualFormController;
    public SpiritualFormCombat spiritualFormCombat;

    public bool isSpirit = false; // Determine which form player is.

    [HideInInspector] public bool isKnockbacking = false; // Determine if player is knockbacking or not.
    private float currentManaRegeneration; // Current timer for Spirit power's regeneration.
    private float currentManaConsumption; // Current timer for Spirit power's consumption.
    private float currentTransformCooldown; // Transform cooldown timer.
    private float currentInvincibleDuration; // Current invincible duration timer.
    private bool outOfMana = false; // Determine if player used up all manas.
    [HideInInspector] public float soulFlyDamage = 0; // Amount of Soul Fly's Latern's damage.
    private float timer; // Timer for time elapsed.

    private Dictionary<string, Item> itemBuffsDict = new Dictionary<string, Item>(); // Hashmap that contains Item Buff effects.

    private Inventory inventory;
    private AbilityUpgradeManager abilityUpgradeManager;

    [Header("Feedbacks")]
    public MMFeedbacks transformToSFX;
    public MMFeedbacks transformBackSFX;
    public MMFeedbacks takeDamageSFX;
    public MMFeedbacks diesSFX;
    [Space(5)]
    public MMFeedbacks shootSpiritualBulletSFX;
    public MMFeedbacks outOfManaSFX;
    public MMFeedbacks teleportSFX;

    protected override void Awake()
    {
        base.Awake();

        instance = this;
    }

    protected override void Start()
    {
        base.Start();

        gameManager = GameManager.instance;
        inventory = Inventory.instance;
        abilityUpgradeManager = UpgradeManager.instance.abilityUpgradeManager;

        currentManaConsumption = 1f;
        currentManaRegeneration = 1f;

        spriteRenderer = physicalFormController.GetComponent<SpriteRenderer>();
        FormUpdater(false);

        SetStat("Soul Bar", 0f);

        OnEffectWornOut += spiritualFormController.OnEffectWornOut;
    }

    protected override void Update()
    {
        base.Update();

        // If player is DEAD, return.
        if (entityState == EntityState.DEAD || HasEffect("Stun"))
        {
            rigidBody.velocity = Vector2.zero;
            return;
        }

        timer += Time.deltaTime;
        SetStatUnclamp("Clear Time", (int)timer);

        // If player is stunning, freeze player's movement.
        if (HasEffect("Stun"))
        {
            rigidBody.velocity = Vector2.up * rigidBody.velocity.y;
            return;
        }

        StuckChecker();
        SpiritPowerUpdater();

        // If player has Soulfly's Lantern item, update Soul Fly's damage.
        if (itemBuffsDict.ContainsKey("Soulfly's Lantern"))
        {
            SoulFlyUpdater();
        }

        spiritualFormCombat.MarkUpdater();

        // Check if player can't maintain in Spiritual form, Transform back to Physical form.
        if (HasEffect("Spiritual Blocker") && isSpirit)
        {
            FormUpdater(false);
        }
        
        // Check if player doesn't have enough Mana to maintain in Spiritual form, Transform back to Physical form.
        if ((GetStat("Mana") <= 0f) && isSpirit)
        {
            outOfMana = true;
            FormUpdater(false);
        }

        // [Right-Mouse Button] - Switch to Spiritual Form.
        if (Input.GetMouseButtonDown(1) && !isSpirit && currentTransformCooldown <= 0f && !HasEffect("Spiritual Blocker") && playerState != PlayerState.ABILITY)
        {
            if (outOfMana && GetStat("Mana") < 100f)
            {
                // Play Out of Mana feedback.
                if (outOfManaSFX)
                {
                    outOfManaSFX.PlayFeedbacks();
                }

                return;
            }

            FormUpdater(!isSpirit);
            spiritualFormCombat.AutoAttack();
            spiritualFormCombat.ResetSavedPosition();
        }

        // [Left-Mouse Button] - Switch to Physical Form.
        if (Input.GetMouseButtonDown(0) && isSpirit && currentTransformCooldown <= 0f)
        {
            FormUpdater(!isSpirit);

            // Check if there's marked enemy, teleport to that location (if close enough within teleportRange)
            if (spiritualFormCombat.GethasMark() && spiritualFormCombat.GetMarkOn() != null)
            {
                GameObject enemy = spiritualFormCombat.GetMarkOn();

                // Set direction that player will facing,
                if (enemy.transform.position.x < transform.position.x)
                {
                    transform.position = (Vector2)enemy.transform.position + (Vector2.right * 1.3f);
                    physicalFormCombat.SetAttackDirection(PhysicalFormCombat.AttackDirection.LEFT);
                }
                else
                {
                    transform.position = (Vector2)enemy.transform.position + (Vector2.left * 1.3f);
                    physicalFormCombat.SetAttackDirection(PhysicalFormCombat.AttackDirection.RIGHT);
                }

                spriteRenderer.flipX = enemy.transform.position.x < transform.position.x;

                spiritualFormCombat.SetHasMark(false);
                spiritualFormCombat.SetMarkOn(null);

                teleportSFX.PlayFeedbacks();
            }

            physicalFormCombat.AttackInput();
            spiritualFormCombat.SetComboCount(0);

        }

        /*
        // DEBUGGING - [M] - Refill Mana + Gain Gold and Soul coins.
        if (Input.GetKey(KeyCode.M))
        {
            IncreaseStat("Mana", 5f);
            IncreaseStat("Gold Coin", 10f);
            IncreaseStat("Soul Coin", 10f);
        }
        */

        currentTransformCooldown -= Time.deltaTime;
        currentTransformCooldown = Mathf.Clamp(currentTransformCooldown, 0f, transformCooldown);

        currentInvincibleDuration -= Time.deltaTime;
        currentInvincibleDuration = Mathf.Clamp(currentInvincibleDuration, 0f, invincibleDuration);
    }

    // Function to update after transform to other form.
    public void FormUpdater(bool spirit)
    {
        // Play Transformation particle.
        if (util.particles.playerTransformation)
        {
            GameObject transformationParticle = Instantiate(util.particles.playerTransformation, transform.position, Quaternion.identity);
            transformationParticle.transform.SetParent(transform);
            transformationParticle.transform.localPosition = Vector3.zero;
        }

        if (isSpirit != spirit)
        {
            try {
                if (spirit)
                {
                    transformToSFX.Initialization();
                    transformToSFX.PlayFeedbacks();
                }
                else
                {
                    transformBackSFX.Initialization();
                    transformBackSFX.PlayFeedbacks();
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            // Ancient Lotus - Buff after transform.
            if (itemBuffsDict.ContainsKey("Ancient Lotus"))
            {
                StartCoroutine(AncientLotus(itemBuffsDict["Ancient Lotus"]));
            }
        }

        isSpirit = spirit;
        currentTransformCooldown = transformCooldown;

        physicalFormController.gameObject.SetActive(!isSpirit);
        spiritualFormController.gameObject.SetActive(isSpirit);

        if (isSpirit)
        {
            Texture2D texture = Util.instance.prefabs.aimCursor;
            Cursor.SetCursor(texture, new Vector2(texture.width / 2f, texture.height / 2f), CursorMode.Auto);

            spiritualFormCombat.SetCombo(physicalFormCombat.GetCombo());
            spiritualFormCombat.SetComboCooldown(physicalFormCombat.GetComboCooldown());
            colliders.size = spiritualFormController.colliders.size;

            rigidBody.drag = 0f;
            rigidBody.gravityScale = 0f;

            spriteRenderer = spiritualFormController.gameObject.GetComponent<SpriteRenderer>();
            spiritualFormController.animator.Play("Anim_Player_Transform");
        }
        else
        {
            Cursor.SetCursor(Util.instance.prefabs.normalCursor, Vector2.zero, CursorMode.Auto);

            physicalFormCombat.SetCombo(spiritualFormCombat.GetCombo());
            physicalFormCombat.SetComboCooldown(spiritualFormCombat.GetComboCooldown());
            colliders.size = physicalFormController.colliders.size;

            spriteRenderer = physicalFormController.gameObject.GetComponent<SpriteRenderer>();
            physicalFormController.animator.Play("Anim_Player_Transform");
        }

        savedColor = Color.white;
    }

    // Function to update Spirit Power Meter.
    private void SpiritPowerUpdater()
    {
        // If player is in Spiritual form, keep decreasing Mana.
        if (isSpirit)
        {
            if (GetStat("Mana") > 0f && !spiritualFormCombat.GetIsHoldingSpiritualResonance())
            {
                if (currentManaConsumption > 0f)
                {
                    currentManaConsumption -= Time.deltaTime;
                }
                else
                {
                    DecreaseStat("Mana", GetStat("Mana Consumption"));
                    currentManaConsumption = 1f;
                }
            }
        }
        else
        {
            // If not, regenerating Mana back.
            if (GetStat("Mana") < GetBaseStat("Mana"))
            {
                if (currentManaRegeneration > 0f)
                {
                    currentManaRegeneration -= Time.deltaTime;
                }
                else
                {
                    IncreaseStat("Mana", GetStat("Mana Regeneration"));
                    currentManaRegeneration = 1f;

                    // If it reaches maximum, remove Out of Mana restriction.
                    if (GetStat("Mana") >= GetBaseStat("Mana"))
                    {
                        outOfMana = false;
                    }
                }
            }
        }
    }

    // Function to check if player is stucking inside or between wall, find nearest space to teleport out.
    private void StuckChecker()
    {
        bool stuck = false;

        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, 0.27f))
        {
            GameObject objects = collider.gameObject;

            // If it's not player, and it's Ground layer.
            if (!objects.Equals(gameObject) && LayerMask.LayerToName(objects.layer).Equals("Ground") && !objects.GetComponent<PlatformEffector2D>())
            {
                stuck = true;
                break;
            }
        }

        // If player is stucking, find nearest space and teleport player.
        if (stuck)
        {
            Vector2 nearestPosition = FindNearestSpace(5f, 1f);
            transform.position = nearestPosition;
        }
    }

    // Function to find nearest space from player's position.
    private Vector2 FindNearestSpace(float searchRadius, float searchSize)
    {
        Vector2 nearestSpace = transform.position;
        Vector2[] directionList = new Vector2[8];

        directionList[0] = Vector2.up; // TOP
        directionList[1] = new Vector2(-1f, 1f); // TOP-LEFT
        directionList[2] = Vector2.left; // LEFT
        directionList[3] = -Vector2.one; // BOTTOM-LEFT
        directionList[4] = Vector2.down; // BOTTOM
        directionList[5] = new Vector2(1f, -1f); // BOTTOM-RIGHT
        directionList[6] = Vector2.right; // RIGHT
        directionList[7] = Vector2.one; // TOP-RIGHT

        // Interation until reach searchRadius.
        for (int i = 0; i < searchRadius; i++)
        {
            // Interation through all directions.
            for (int dir = 0; dir < directionList.Length; dir++)
            {
                // Get position with direction and distance.
                Vector2 position = (Vector2)transform.position + (directionList[dir] * (i * 0.5f));

                bool empty = true;

                // Check if that position is empty or near nothing.
                foreach (Collider2D collider in Physics2D.OverlapCircleAll(position, searchSize))
                {
                    if (collider.gameObject.Equals(gameObject) || !collider.isTrigger)
                    {
                        empty = false;
                    }
                }

                // If that position is clear with no collision nearby, return that position.
                if (empty)
                {
                    return position;
                }
            }
        }

        return nearestSpace;
    }

    // Function to Apply Buff.
    public void ApplyBuff()
    {

        
        inventory.slotList.ForEach(slot => {

            if (!slot.hasApplied) {
                Item item = slot.item;
                slot.hasApplied = true;

                switch (item.itemName)
                {
                    case "Cloth Armor":
                    IncreaseBaseStat("Health", item.GetTotalModifyValue());
                    break;

                    case "Rusty Sword":
                    IncreaseBaseStat("Strength", item.GetTotalModifyValue());
                    break;

                    case "Old Boots":

                    float oldBootsPhysical = (GetBaseStat("Physical Move Speed") * item.GetTotalModifyValue()) / 100f;
                    IncreaseBaseStat("Physical Move Speed", oldBootsPhysical);

                    float oldBootsSpiritual = (GetBaseStat("Spiritual Move Speed") * (item.GetTotalModifyValue() / 2f)) / 100f;
                    IncreaseBaseStat("Spiritual Move Speed", oldBootsSpiritual);
                    break;

                    case "Tattered Cloak":
                    IncreaseBaseStat("Agility", item.GetTotalModifyValue());
                    break;

                    case "Broken Staff":
                    IncreaseBaseStat("Magic Power", item.GetTotalModifyValue());
                    break;

                    case "Wooden Shield":
                    IncreaseBaseStat("Physical Armor", item.GetTotalModifyValue());
                    break;

                    case "Armguard":
                    IncreaseBaseStat("Magic Armor", item.GetTotalModifyValue());
                    break;

                    case "Karmic Thorns":
                    itemBuffsDict["Karmic Thorns"] = item;
                    break;

                    case "Peculiar Sponge":
                    itemBuffsDict["Peculiar Sponge"] = item;
                    break;

                    case "Mana Fragment":
                    IncreaseBaseStat("Mana", item.GetTotalModifyValue());
                    break;

                    case "Mage's Loafer":
                    float mageLoafer = (GetBaseStat("Mana Regeneration") * item.GetTotalModifyValue()) / 100f;
                    IncreaseBaseStat("Mana Regeneration", mageLoafer);
                    break;

                    case "Berserker's Seal":
                    IncreaseBaseStat("Strength", item.GetTotalModifyValue(PassiveType.STRENGTH));
                    DecreaseBaseStat("Physical Armor", item.GetTotalModifyValue(PassiveType.PHYSICAL_ARMOR));
                    break;

                    case "Rabid Mage's Seal":
                    IncreaseBaseStat("Magic Power", item.GetTotalModifyValue(PassiveType.MAGIC_POWER));
                    DecreaseBaseStat("Magic Armor", item.GetTotalModifyValue(PassiveType.MAGIC_ARMOR));
                    break;

                    case "Rogue's Guidebook":
                    itemBuffsDict["Rogue's Guidebook"] = item;
                    break;

                    case "Bat's Tongue":
                    itemBuffsDict["Bat's Tongue"] = item;
                    break;

                    case "Reinforced Armor":
                    IncreaseBaseStat("Health", item.GetTotalModifyValue());
                    break;

                    case "Steel Sword":
                    IncreaseBaseStat("Strength", item.GetTotalModifyValue());
                    break;

                    case "Leather Boots":
                    float leatherBootsPhysical = (GetBaseStat("Physical Move Speed") * item.GetTotalModifyValue()) / 100f;
                    IncreaseBaseStat("Physical Move Speed", leatherBootsPhysical);

                    float leatherBootsSpiritual = (GetBaseStat("Spiritual Move Speed") * item.GetTotalModifyValue()) / 100f;
                    IncreaseBaseStat("Spiritual Move Speed", leatherBootsSpiritual);
                    break;

                    case "Silkworm Cloak":
                    float silkwormCloak = (GetBaseStat("Agility") * item.GetTotalModifyValue()) / 100f;
                    IncreaseBaseStat("Agility", silkwormCloak);
                    break;

                    case "Magic Staff":
                    IncreaseBaseStat("Magic Power", item.GetTotalModifyValue());
                    break;

                    case "Iron Shield":
                    IncreaseBaseStat("Physical Armor", item.GetTotalModifyValue());
                    break;

                    case "Reinforced Armguard":
                    IncreaseBaseStat("Magic Armor", item.GetTotalModifyValue());
                    break;

                    case "Mana Stone":
                    IncreaseBaseStat("Mana", item.GetTotalModifyValue());
                    break;

                    case "High Mage's Hat":
                    float highMageHat = (GetBaseStat("Mana Regeneration") * item.GetTotalModifyValue()) / 100f;
                    IncreaseBaseStat("Mana Regeneration", highMageHat);
                    break;

                    case "Rogue's Legacy":
                    itemBuffsDict["Rogue's Legacy"] = item;
                    break;

                    case "Bombardier Gauntlet":
                    itemBuffsDict["Bombardier Gauntlet"] = item;
                    break;

                    case "Black Wasp's Head":
                    itemBuffsDict["Black Wasp's Head"] = item;
                    break;

                    case "Fellsworth's Requiem":
                    itemBuffsDict["Fellsworth's Requiem"] = item;
                    break;

                    case "Queen Bee's Scale":
                    itemBuffsDict["Queen Bee's Scale"] = item;
                    break;

                    case "Elegy of the Deceased":
                    itemBuffsDict["Elegy of the Deceased"] = item;
                    break;

                    case "Serpentine Skin":
                    itemBuffsDict["Serpentine Skin"] = item;
                    break;

                    case "Unbounded Silk":
                    itemBuffsDict["Unbounded Silk"] = item; // Not implemented yet.
                    break;

                    case "Ancient Lotus":
                    itemBuffsDict["Ancient Lotus"] = item;
                    break;

                    case "Ancestral Will":
                    float ancestralWillPhysical = (GetBaseStat("Strength") * item.GetTotalModifyValue()) / 100f;
                    IncreaseBaseStat("Strength", ancestralWillPhysical);

                    float ancestralWillSpiritual = (GetBaseStat("Magic Power") * item.GetTotalModifyValue()) / 100f;
                    IncreaseBaseStat("Magic Power", ancestralWillSpiritual);

                    itemBuffsDict["Ancestral Will"] = item; // Linked with PhysicalCombat.
                    break;

                    case "Piercing Bowstrings":
                    itemBuffsDict["Piercing Bowstrings"] = item;
                    break;

                    case "Soulfly's Lantern":
                    itemBuffsDict["Soulfly's Lantern"] = item;
                    break;

                    default:
                    break;
                }
            }
        });

/*
        foreach (var temp in baseStatsDict) {
            print(temp);
        }
*/

    }

    // Function when player got knockback.
    public override void Knockback(Vector2 basePosition, Vector2 knockbackForce)
    {
        // If player is in Spiritual form, cancel.
        if (isSpirit)
        {
            return;
        }

        Knockbacking();
        base.Knockback(basePosition, knockbackForce);
    }

    // Function when player took damages.
    public override void TakeDamage(Damage damage, GameObject causeObject)
    {
        // If player is already dead or is in Invincible mode, cancel.
        if (entityState == EntityState.DEAD || currentInvincibleDuration > 0f)
        {
            return;
        }

        currentInvincibleDuration = invincibleDuration;

        // Check if player has already unlock Dashing abilities Lv.4 (Ability to dodge all attacks during Dashing).
        if (abilityUpgradeManager.GetAbility(AbilityType.DASH).abilityLevel[3].purchased && physicalFormController.IsDashing())
        {
            return;
        }

        float dodgeChance = UnityEngine.Random.Range(0f, 100f);

        // if there's enough chance of agility, dodge.
        if (dodgeChance <= GetStat("Agility"))
        {
            Debug.Log("Agility Dodge");
            return;
        }

        // Play Take Damage animation.
        if (!isSpirit)
        {
            physicalFormCombat.SetCombo(0);
            physicalFormController.animator.Play("Anim_Physical_Hurt");
        }
        else
        {
            spiritualFormController.animator.Play("Anim_Spiritual_Hurt");
        }

        takeDamageSFX.Initialization();
        takeDamageSFX.PlayFeedbacks();

        float physicalDamage = damage.physicalDamage;
        float magicDamage = damage.magicDamage;

        Entity entity;

        // Check if player being attacked by Enemy.
        if (causeObject.TryGetComponent(out entity) && causeObject.CompareTag("Enemy"))
        {
            // Karmic Thorns - Reflect damages.
            if (itemBuffsDict.ContainsKey("Karmic Thorns"))
            {
                float reflectPhysicalDamage = (damage.physicalDamage * itemBuffsDict["Karmic Thorns"].GetTotalModifyValue()) / 100f;
                float reflectMagicDamage = (damage.magicDamage * itemBuffsDict["Karmic Thorns"].GetTotalModifyValue()) / 100f;

                entity.TakeDamage(new Damage(reflectPhysicalDamage, reflectMagicDamage), gameObject);
            }

            // Serpentine Skin - Receive -30% damages if enemy is far away.
            if (itemBuffsDict.ContainsKey("Serpentine Skin"))
            {
                float range = itemBuffsDict["Serpentine Skin"].GetTotalModifyValue(PassiveType.RANGE);
                float distance = Vector2.Distance(transform.position, entity.transform.position);

                if (distance < range)
                {
                    float reducePhysicalDamage = (damage.physicalDamage * itemBuffsDict["Serpentine Skin"].GetTotalModifyValue()) / 100f;
                    physicalDamage -= reducePhysicalDamage;

                    float reduceMagicDamage = (damage.magicDamage * itemBuffsDict["Serpentine Skin"].GetTotalModifyValue()) / 100f;
                    magicDamage -= reduceMagicDamage;
                }
            }
        }

        float physicalArmor = (damage.physicalDamage * (GetStat("Physical Armor") / 2f)) / 100f;
        float magicArmor = damage.physicalDamage * GetStat("Magic Armor") / 100f;

        physicalDamage -= physicalArmor;
        physicalDamage = Mathf.Clamp(physicalDamage, 0f, damage.physicalDamage);

        magicDamage -= magicArmor;
        magicDamage = Mathf.Clamp(magicDamage, 0f, damage.magicDamage);

        DamageIndicator(damage, causeObject, "#FF4C4C", "#FF4C4C");
        VirtualCamera.instance.CameraShake(CameraEventName.PLAYER_TAKE_DAMAGE);

        if (isSpirit)
        {
            IncreaseStatUnclamp("Damage Received", magicDamage);
            DecreaseStat("Mana", magicDamage);
        }
        else
        {
            IncreaseStatUnclamp("Damage Received", physicalDamage);
            DecreaseStat("Health", physicalDamage);
        }

        if (GetStat("Health") <= 0f)
        {
            // Elegy of the Deceased - Revive with 30% of max health.
            if (itemBuffsDict.ContainsKey("Elegy of the Deceased"))
            {
                SetStat("Health", GetBaseStat("Health") * 0.3f);
            }
            else
            {
                Die();
            }
        }
    }

    protected override void Die()
    {
        if (entityState == EntityState.DEAD)
        {
            return;
        }

        entityState = EntityState.DEAD;

        // Play Death animation.
        if (!isSpirit)
        {
            physicalFormController.animator.Play("Anim_Physical_Death");
        }
        else
        {
            spiritualFormController.animator.Play("Anim_Spiritual_Death");
        }

        diesSFX.Initialization();
        diesSFX.PlayFeedbacks();

        UIManager.instance.defeatScreen.SetActive(true);
        UIManager.instance.UpdateDefeatStat();
        
        Debug.Log("GAME OVER - RETURN TO HUB");
    }

    // Function to add effect to entity.
    public override void AddEffect(string name, float duration)
    {
        if (HasEffect(name))
        {
            effectDict[name] += duration;
        }
        else
        {
            if (isSpirit)
            {
                spiritualFormController.animator.Play("Anim_Spiritual_GorgoEgg_Stun");
            }

            if (currentStunParticle)
            {
                Destroy(currentStunParticle.gameObject);
            }

            Vector2 position = (Vector2)transform.position + (Vector2.up * spriteRenderer.size.x / 1.75f);
            currentStunParticle = Instantiate(util.particles.stunEffect, position, util.particles.stunEffect.transform.rotation);
            effectDict[name] = duration;
        }
    }

    // Function to update timer while player is being knockbacked (Ignore rigidbody velocity).
    private async void Knockbacking()
    {
        float timer = 0.5f;

        while (timer > 0f)
        {
            isKnockbacking = true;

            timer -= Time.deltaTime;
            await Task.Yield();
        }

        isKnockbacking = false;
    }

    // Function to activate Ancient Lotus's Buff - Increase 100% damages for 5 seconds after transform.
    public IEnumerator AncientLotus(Item item)
    {
        float baseStrength = GetStat("Strength");
        float baseMagicPower = GetStat("Magic Power");

        float strengthPercent = (baseStrength * item.GetTotalModifyValue(PassiveType.DAMAGE)) / 100f;
        float magicPowerPercent = (baseMagicPower * item.GetTotalModifyValue(PassiveType.DAMAGE)) / 100f;

        IncreaseStatUnclamp("Strength", strengthPercent);
        IncreaseStatUnclamp("Magic Power", magicPowerPercent);

        yield return new WaitForSeconds(5);

        DecreaseStat("Strength", strengthPercent);
        DecreaseStat("Magic Power", magicPowerPercent);
    }

    // Function to update Soulfly's Latern Item's damage.
    public void SoulFlyUpdater()
    {
        if (itemBuffsDict.ContainsKey("Soulfly's Lantern"))
        {
            float percent = (GetStat("Magic Power") * itemBuffsDict["Soulfly's Lantern"].GetTotalModifyValue()) / 100f;
            soulFlyDamage = GetBaseStat("Soul Coin") * percent;
        }
        else
        {
            soulFlyDamage = 0f;
        }
    }

    // Function to determine if player is in invincible mode.
    public bool IsInvincible()
    {
        return currentInvincibleDuration > 0f;
    }

    // Function to fetch item buffs.
    public Dictionary<string, Item> GetSpecialBuffDict()
    {
        return itemBuffsDict;
    }

    // Function to debug all Base stats.
    public void BaseDictDebug()
    {
        foreach (KeyValuePair<string, float> pair in baseStatsDict)
        {
            Debug.Log(pair.Key + ": " + pair.Value);
        }
    }

    // Function to debug all current stats.
    public void DictDebug() {
        foreach (KeyValuePair<string, float> pair in statsDict) {
            Debug.Log(pair.Key + ": " + pair.Value);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Player leaving Dungeon Zone - Clear Buff.
        if (other.CompareTag("Dungeon"))
        {
            inventory.Clear();
            ResetStat();
        }
    }

    public bool IsNearTeleport() {
        List<GameObject> allTeleports = GameObject.FindGameObjectsWithTag("Teleport").ToList();
        Debug.Log("here");
        foreach (var tp in allTeleports) {
            print(tp + ", " + tp.transform.position);
            if (Vector2.Distance(tp.gameObject.transform.position, this.transform.position) < 3) {
                return true;
            }
        }
        return false;
    }

}