using System;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using static Item;

public class Enemy : Entity
{
    public enum EnemyState
    {
        PATROL, CHASE, ATTACK
    }

    [Header("Enemy Settings")]
    [SerializeField] protected EnemyState enemyState = EnemyState.PATROL;
    [Space(5)]
    [SerializeField] private int soulDrop = 5; // Drop amount of soul.
    [SerializeField] private int goldDrop = 1; // Drop amount of gold.
    [Space(5)]
    public Damage attackDamage; // Attack damages.
    [SerializeField] protected float moveSpeed = 5f; // Movement speed.
    [SerializeField] protected float attackCooldown; // Attack cooldown before start attack.
    [SerializeField] protected float attackDelay; // Attack delay between each attack.
    [SerializeField] protected float aggroDelay; // Aggro delay after detected player.
    [SerializeField] protected float hitRecoverDelay; // Hit recover delay after got attack.
    [Space(5)]
    [SerializeField] protected bool knockbackable = true; // Determine to make enemy knockbackable or not.
    [SerializeField] protected bool stunAfterHit = true; // Detemrine if enemy will stun after took damages or not.
    [SerializeField] private float freezeFrameTime = 0.2f; // Duration of freeze frame.
    [SerializeField] private float freezeFrameDelay = 0.1f; // Delay before start freeze frame.
    [SerializeField] private float freezeFrameAfterDuration = 1f; // Duration after freeze frame. (For Animation curve)
    [SerializeField] private AnimationCurve freezeFrameAfterCurve; // Animation curve after freeze frame.

    [Header("Attack Detection")]
    [SerializeField] protected bool showAttackDetection = false; // Determine to show Attack Detection or not.
    [Space(5)]
    [SerializeField] protected Vector2 aggroDetection = Vector2.one; // Range to make enemy start chasing toward player.
    [SerializeField] protected Vector2 aggroOffset = Vector2.zero; // Offset of Aggro's detection.
    [SerializeField] protected Vector2 attackDetection = Vector2.one; // Range to make enemy start attacking player.
    [SerializeField] protected Vector2 attackOffset = Vector2.zero; // Offset of Attack's detection.

    protected float currentAttackCooldown; // Attack cooldown timer.
    protected float currentAttackDelay; // Attack delay timer.
    protected float currentAggroDelay; // Aggro delay timer.
    protected float currentHitRecoverDelay; // Hit recover delay timer.
    protected int moveX = 1; // Determine if enemy moving left or right.
    protected bool ignoreHitRecover = false; // Determine to ignore hit recover delay or not. (While enemy using abilities).
    private bool firstAttack = false; // Determine if it has already attack player before. (Use for set Attack delay when change to Attack state)

    public Action OnTakeDamage; // Event when enemy take damages.

    protected Player player;
    protected Animator animator;

    protected override void Start()
    {
        base.Start();

        player = Player.instance;
        animator = GetComponent<Animator>();

        currentAggroDelay = aggroDelay;
    }

    protected override void Update()
    {
        base.Update();

        if (entityState == EntityState.DEAD)
        {
            return;
        }

        currentAttackCooldown -= Time.deltaTime;
        currentAttackCooldown = Mathf.Clamp(currentAttackCooldown, 0f, attackCooldown);

        currentAttackDelay -= Time.deltaTime;
        currentAttackDelay = Mathf.Clamp(currentAttackDelay, 0f, attackDelay);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // If got hit, wait for recover time before start action.
        if (currentHitRecoverDelay > 0f)
        {
            currentHitRecoverDelay -= Time.fixedDeltaTime;

            // If it's not knockbackable, make it froze and stay still.
            if (!knockbackable)
            {
                rigidBody.velocity = Vector2.zero;
            }

            // If it's not being ignore, make it prevent from doing other actions.
            if (!ignoreHitRecover)
            {
                return;
            }
        }

        switch (enemyState)
        {
            case EnemyState.PATROL:
            Patrol();
            break;

            case EnemyState.CHASE:
            Chase();
            break;

            case EnemyState.ATTACK:
            Attack();
            break;
        }
    }

    // Function that execute during Patrol state.
    protected virtual void Patrol()
    {
        DirectionUpdater();

        // If player is in Aggro area, go to Chase state.
        if (IsPlayerInAggro())
        {
            // Delay before start Chasing.
            if (currentAggroDelay <= 0f)
            {
                rigidBody.velocity = Vector2.zero;
                currentAttackCooldown = attackCooldown;
                ChangeState(EnemyState.CHASE);

                enemyState = EnemyState.CHASE;
                return;
            }

            // Make enemy look at player's direction.
            if (player.transform.position.x <= transform.position.x)
            {
                moveX = -1;
            }
            else
            {
                moveX = 1;
            }

            DirectionUpdater();

            currentAggroDelay -= Time.fixedDeltaTime;
            currentAggroDelay = Mathf.Clamp(currentAggroDelay, 0f, aggroDelay);

            rigidBody.velocity = Vector2.up * rigidBody.velocity.y;
        }
        else
        {
            // Else, reset Aggro delay timer.
            currentAggroDelay = aggroDelay;
        }
    }

    // Function that execute during Chase state.
    protected virtual void Chase()
    {
        MoveDirectionUpdater();
        DirectionUpdater();

        // If player is in Attack area, go to Attack state.
        if (IsPlayerInAttackRange())
        {
            // Delay before start Attacking.
            if (currentAttackCooldown <= 0f)
            {
                if (!firstAttack)
                {
                    currentAttackDelay = 0f;
                    firstAttack = true;
                }

                ChangeState(EnemyState.ATTACK);

                enemyState = EnemyState.ATTACK;
                return;
            }

            rigidBody.velocity = Vector2.up * rigidBody.velocity.y;
        }

        // Check if player is out of Aggro range, back to Patrol state.
        if (!IsPlayerInAggro())
        {
            currentAggroDelay = aggroDelay;
            ChangeState(EnemyState.PATROL);

            enemyState = EnemyState.PATROL;
        }
    }

    // Function that execute during Attack state.
    protected virtual void Attack()
    {
        MoveDirectionUpdater();
        DirectionUpdater();

        rigidBody.velocity = Vector2.zero;

        // If enemy is able to attack and player is in range, Attack.
        if (currentAttackDelay <= 0f && IsPlayerInAttackRange())
        {
            player.TakeDamage(new Damage(attackDamage.physicalDamage, attackDamage.magicDamage), gameObject);
            currentAttackDelay = attackDelay;
        }

        // If player is not in Aggro's detection, back to Chase state.
        if (!IsPlayerInAggro())
        {
            currentAttackCooldown = attackCooldown;
            ChangeState(EnemyState.CHASE);

            enemyState = EnemyState.CHASE;
        }
    }

    // Function that execute when change states.
    protected virtual void ChangeState(EnemyState toState) { }

    // Function to update enemy move direction. (moveX)
    protected virtual void MoveDirectionUpdater()
    {
        float distance = Vector2.Distance(player.transform.position, transform.position);

        // If it's close enough, return to not make sprite flipping around player.
        if (distance < 0.1f)
        {
            return;
        }

        if (player.transform.position.x < transform.position.x)
        {
            moveX = -1;
        }
        else
        {
            moveX = 1;
        }
    }

    // Function to update enemy facing direction.
    protected virtual void DirectionUpdater()
    {
        spriteRenderer.flipX = moveX < 0;
    }

    // Function to get all detected collider within detection size.
    protected Collider2D[] GetDetectionArea(Vector2 offset, Vector2 size)
    {
        Vector2 center = (Vector2)transform.position + offset;
        Vector2 position1 = center + (size / 2f);
        Vector2 position2 = center - (size / 2f);

        return Physics2D.OverlapAreaAll(position1, position2);
    }

    // Function to determine if player is stil in aggro area.
    protected bool IsPlayerInAggro()
    {
        foreach (Collider2D collider in GetDetectionArea(aggroOffset, aggroDetection))
        {
            if (player != null && collider.gameObject.Equals(player.gameObject))
            {
                Vector2 direction = player.transform.position - transform.position;
                float distance = Vector2.Distance(transform.position, player.transform.position);
                RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, LayerMask.GetMask("Ground"));

                // Check if there's a wall blocked enemy's sight, return.
                if (hit.collider != null)
                {
                    return false;
                }

                return true;
            }
        }

        return false;
    }

    // Function to determine if player is stil in attack area.
    protected bool IsPlayerInAttackRange()
    {
        foreach (Collider2D collider in GetDetectionArea(attackOffset, attackDetection))
        {
            if (collider.gameObject.Equals(player.gameObject))
            {
                Vector2 direction = player.transform.position - transform.position;
                float distance = Vector2.Distance(transform.position, player.transform.position);
                RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, LayerMask.GetMask("Ground"));

                // Check if there's a wall blocked enemy's sight, return.
                if (hit.collider != null)
                {
                    return false;
                }

                return true;
            }
        }

        return false;
    }

    public override void Knockback(Vector2 basePosition, Vector2 knockbackForce)
    {
        if (!knockbackable)
        {
            return;
        }

        base.Knockback(basePosition, knockbackForce);
    }

    // Function when enemy took damages.
    public override void TakeDamage(Damage damage, GameObject causeObject)
    {

        try
        {
            OnTakeDamage.Invoke();
        }
        catch (NullReferenceException) { }

        StartCoroutine(DamageRenderer());

        if (stunAfterHit)
        {
            currentHitRecoverDelay = hitRecoverDelay;
        }

        float physicalDamage = damage.physicalDamage;
        float magicDamage = damage.magicDamage;

        // Rogue's Guidebook - Deal +40% damages with enemy 90%+ healths.
        if (player.GetSpecialBuffDict().ContainsKey("Rogue's Guidebook"))
        {
            if (GetStat("Health") / GetBaseStat("Health") >= player.GetSpecialBuffDict()["Rogue's Guidebook"].GetTotalModifyValue(PassiveType.CONDITIONAL) / 100f)
            {
                float addPhysicalDamage = (damage.physicalDamage * player.GetSpecialBuffDict()["Rogue's Guidebook"].GetTotalModifyValue()) / 100f;
                physicalDamage += addPhysicalDamage;

                float addMagicDamage = (damage.magicDamage * player.GetSpecialBuffDict()["Rogue's Guidebook"].GetTotalModifyValue()) / 100f;
                magicDamage += addMagicDamage;
            }
        }

        // Rogue's Legacy - Deal +60% damages with enemy 90%+ healths.
        if (player.GetSpecialBuffDict().ContainsKey("Rogue's Legacy"))
        {
            if (GetStat("Health") / GetBaseStat("Health") >= player.GetSpecialBuffDict()["Rogue's Legacy"].GetTotalModifyValue(PassiveType.CONDITIONAL) / 100f)
            {
                float addPhysicalDamage = (damage.physicalDamage * player.GetSpecialBuffDict()["Rogue's Legacy"].GetTotalModifyValue()) / 100f;
                physicalDamage += addPhysicalDamage;

                float addMagicDamage = (damage.magicDamage * player.GetSpecialBuffDict()["Rogue's Legacy"].GetTotalModifyValue()) / 100f;
                magicDamage += addMagicDamage;
            }
        }

        float physicalArmor = (damage.physicalDamage * (GetStat("Physical Armor") / 2f)) / 100f;
        float magicArmor = damage.physicalDamage * GetStat("Magic Armor") / 100f;

        physicalDamage -= physicalArmor;
        physicalDamage = Mathf.Clamp(physicalDamage, 0f, damage.physicalDamage);

        magicDamage -= magicArmor;
        magicDamage = Mathf.Clamp(magicDamage, 0f, damage.magicDamage);

        DamageIndicator(damage, causeObject, "#FFFFFF", "#88CCFF");
        DecreaseStat("Health", physicalDamage + magicDamage);

        if (GetStat("Health") <= 0)
        {
            BloodSplash(causeObject);
            Die();
        }
    }

    protected override void Die()
    {
        if (entityState == EntityState.DEAD)
        {
            return;
        }

        entityState = EntityState.DEAD;

        gameManager.FreezeFrame(freezeFrameTime, freezeFrameDelay, freezeFrameAfterDuration, freezeFrameAfterCurve);
        VirtualCamera.instance.CameraShake(CameraEventName.ENEMY_DIE);

        SFXLibrary.instance.PlaySFX(SFXLibrary.instance.ImpactSFX);
        DropCurrency();

        // Peculiar Sponge - 15% chances to recover 10 Mana points after killing enemy.
        if (player.GetSpecialBuffDict().ContainsKey("Peculiar Sponge"))
        {
            float randomChance = UnityEngine.Random.Range(0f, 100f);

            if (randomChance <= player.GetSpecialBuffDict()["Peculiar Sponge"].GetPassive(PassiveType.MANA).modifyChance)
            {
                player.IncreaseStat("Mana", player.GetSpecialBuffDict()["Peculiar Sponge"].GetTotalModifyValue());
            }
        }

        // Bat's Tongue - 10% chances to recover 2 Health points after killing enemy.
        if (player.GetSpecialBuffDict().ContainsKey("Bat's Tongue"))
        {
            float randomChance = UnityEngine.Random.Range(0f, 100f);

            if (randomChance <= player.GetSpecialBuffDict()["Bat's Tongue"].GetPassive(PassiveType.HEALTH).modifyChance)
            {
                player.IncreaseStat("Health", player.GetSpecialBuffDict()["Bat's Tongue"].GetTotalModifyValue());
            }
        }

        Destroy(gameObject);
    }

    // Function to spawn Blood Splash on death.
    protected void BloodSplash(GameObject causeObject)
    {
        Instantiate(util.particles.bloodSplashEnemyDeath, transform.position, Quaternion.identity);
        GameObject bloodSplash = Instantiate(util.particles.bloodSplashEnemyFluid, transform.position, util.particles.bloodSplashEnemyDeath.transform.rotation);
        MainModule splashMain = bloodSplash.GetComponent<ParticleSystem>().main;
        ShapeModule splashShape = bloodSplash.GetComponent<ParticleSystem>().shape;

        float height = Vector2.Distance(transform.position, causeObject.transform.position);

        // If enemy got attack from above, splash from center.
        if (height > 0.5f && causeObject.transform.position.y > transform.position.y)
        {
            MinMaxCurve speedCurve = splashMain.startSpeed;
            speedCurve.constantMin += 5f;
            speedCurve.constantMax += 5f;
            splashMain.startSpeed = speedCurve;

            splashShape.shapeType = ParticleSystemShapeType.Sphere;
            return;
        }

        // If enemy is on the right, splash to opposite direction.
        if (causeObject.Equals(player.gameObject))
        {
            if (!player.LookRight())
            {
                Vector3 rotation = splashShape.rotation;
                splashShape.rotation = new Vector3(rotation.x - 90f, rotation.y, rotation.z);
            }
            return;
        }

        if (causeObject.transform.position.x >= transform.position.x)
        {
            Vector3 rotation = splashShape.rotation;
            splashShape.rotation = new Vector3(-rotation.x, rotation.y, rotation.z);
        }
    }

    protected virtual void OnDrawGizmos()
    {
        if (showAttackDetection)
        {
            Vector2 aggroCenter = (Vector2)transform.position + aggroOffset;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(aggroCenter, aggroDetection);

            Vector2 attackCenter = (Vector2)transform.position + attackOffset;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(attackCenter, attackDetection);
        }
    }

    // Function to drop currency.
    private void DropCurrency()
    {
        player = Player.instance;

        int soulAmount = soulDrop;
        int goldAmount = goldDrop;

        if (player.GetSpecialBuffDict().ContainsKey("Fellsworth's Requiem"))
        {
            soulAmount = (int)(soulDrop + soulDrop * player.GetSpecialBuffDict()["Fellsworth's Requiem"].GetTotalModifyValue());
        }

        if (player.GetSpecialBuffDict().ContainsKey("Queen Bee's Scale"))
        {
            goldAmount = (int)(goldDrop + goldDrop * player.GetSpecialBuffDict()["Queen Bee's Scale"].GetTotalModifyValue());
        }
        
        for (int i = 0; i < soulAmount; i ++)
        {
            Vector2 randomDirection = UnityEngine.Random.insideUnitCircle;
            float randomForce = UnityEngine.Random.Range(200f, 300f);
            GameObject soul = Instantiate(util.prefabs.soulItem, transform.position, Quaternion.identity);
            soul.GetComponent<Rigidbody2D>().AddForce(randomDirection * randomForce);
        }

        for (int i = 0; i < goldAmount; i++)
        {
            Vector2 randomDirection = UnityEngine.Random.insideUnitCircle;
            float randomForce = UnityEngine.Random.Range(200f, 300f);
            GameObject gold = Instantiate(util.prefabs.goldCoinItem, transform.position, Quaternion.identity);
            gold.GetComponent<Rigidbody2D>().AddForce(randomDirection * randomForce);
        }
    }
}
