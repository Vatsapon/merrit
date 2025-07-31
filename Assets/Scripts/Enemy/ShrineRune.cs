using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Item;

public class ShrineRune : Entity
{
    public enum ShrineRuneType
    {
        SPIRITUAL_BLOCKER, ENEMY_STATS_BOOST
    }

    [Header("Shrine Rune Settings")]
    [SerializeField] private ShrineRuneType shrineType; // Type of shrine.
    [SerializeField] private bool showEffectArea = false; // Determine to show effect area or not.
    [Space(5)]
    [SerializeField] private Vector2 effectAreaSize = Vector2.one; // Area size.
    [SerializeField] private Vector2 effectAreaOffset = Vector2.zero; // Area offset.
    [Space(10)]
    [SerializeField] private float invincibleDuration = 5f; // Invincible duration.
    [SerializeField] private List<Stats> boostStats = new List<Stats>(); // List of stats to boost.

    private float currentInvincibleDuration; // Invincible duration timer.
    private List<Entity> entityList = new List<Entity>(); // Saved entity that enter or exit area.

    private Player player;

    protected override void Start()
    {
        base.Start();

        player = Player.instance;
    }

    protected override void Update()
    {
        base.Update();
        EntityListUpdater();

        currentInvincibleDuration -= Time.deltaTime;
        currentInvincibleDuration = Mathf.Clamp(currentInvincibleDuration, 0f, invincibleDuration);
    }

    // Function to update entity list that enter or exit area.
    private void EntityListUpdater()
    {
        for (int i = 0; i < entityList.Count; i++)
        {
            // Check if that entity is dead, remove from list.
            if (entityList[i] == null)
            {
                entityList.RemoveAt(i);
            }
        }

        Vector2 center = (Vector2)transform.position + effectAreaOffset;
        Vector2 position1 = center + (effectAreaSize / 2f);
        Vector2 position2 = center - (effectAreaSize / 2f);

        List<Entity> tempEntityList = new List<Entity>();

        foreach (Collider2D collider in Physics2D.OverlapAreaAll(position1, position2))
        {
            Entity entity;

            if (collider.TryGetComponent(out entity) && !collider.gameObject.Equals(gameObject))
            {
                tempEntityList.Add(entity);

                if (!entityList.Contains(entity))
                {
                    OnEnterArea(entity);
                    entityList.Add(entity);
                }
            }
        }

        for (int i = 0; i < entityList.Count; i++)
        {
            // Check if that entity is outside of area, Remove from list.
            if (!tempEntityList.Contains(entityList[i]))
            {
                OnExitArea(entityList[i]);
                entityList.RemoveAt(i);
            }
        }
    }
    
    // Function that execute when entity enter the area.
    private void OnEnterArea(Entity entity)
    {
        Player player;

        if (entity.TryGetComponent(out player) && shrineType == ShrineRuneType.SPIRITUAL_BLOCKER)
        {
            player.AddEffect("Spiritual Blocker", 999f);
        }

        Enemy enemy;

        if (entity.TryGetComponent(out enemy) && shrineType == ShrineRuneType.ENEMY_STATS_BOOST)
        {
            foreach (Stats stat in boostStats)
            {
                enemy.IncreaseStatUnclamp(stat.name, stat.value);
            }

            enemy.OnTakeDamage += OnEnemyTakeDamage;
        }
    }

    // Function that execute when entity leave the area.
    private void OnExitArea(Entity entity)
    {
        Player player;

        if (entity.TryGetComponent(out player) && shrineType == ShrineRuneType.SPIRITUAL_BLOCKER)
        {
            player.RemoveEffect("Spiritual Blocker");
        }

        Enemy enemy;

        if (entity.TryGetComponent(out enemy) && shrineType == ShrineRuneType.ENEMY_STATS_BOOST)
        {
            foreach (Stats stat in boostStats)
            {
                enemy.DecreaseStat(stat.name, stat.value);            }

            enemy.OnTakeDamage -= OnEnemyTakeDamage;
        }
    }

    // Function when enemy took damages while stay inside area.
    private void OnEnemyTakeDamage()
    {
        currentInvincibleDuration = invincibleDuration;
    }

    // Function when shrine took damages.
    public override void TakeDamage(Damage damage, GameObject causeObject)
    {
        // If the shrine is invincible, nulltify damages.
        if (currentInvincibleDuration <= 0f && shrineType == ShrineRuneType.ENEMY_STATS_BOOST)
        {
            return;
        }

        StartCoroutine(DamageRenderer());

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

        DecreaseStat("Health", physicalDamage + magicDamage);

        if (GetStat("Health") <= 0)
        {
            Die();
        }
    }

    protected override void Die()
    {
        // Check if entity is already dead, don't execute further.
        if (entityState == EntityState.DEAD)
        {
            return;
        }

        foreach (Entity entity in entityList)
        {
            OnExitArea(entity);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (showEffectArea)
        {
            Gizmos.DrawWireCube((Vector2) transform.position + effectAreaOffset, effectAreaSize);
        }
    }
}
