using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Entity : MonoBehaviour
{
    public enum EntityState
    {
        ALIVE, DEAD
    }

    [Header("Entity Settings")]
    public EntityState entityState = EntityState.ALIVE; // Entity's state.
    public List<Stats> statsList = new List<Stats>(); // Stats that can be set from Inspector.
    [SerializeField] protected bool showDamageIndicator = true; // Determine to show Damage indicator or not.
    [SerializeField] protected bool showDamageIndicatorArea = false; // Determine to show Damage indicator or not. (For Debugging)
    [SerializeField] protected Vector2 damageIndicatorOffsetMin = Vector2.zero; // Damage Indicator minimum offset from entity.
    [SerializeField] protected Vector2 damageIndicatorOffsetMax = Vector2.one; // Damage Indicator maximum offset from entity.

    [Space(10)]
    public Rigidbody2D rigidBody; // Entity's Rigidbody.
    public SpriteRenderer spriteRenderer; // Entity's sprite renderer.

    protected Dictionary<string, float> statsDict = new Dictionary<string, float>(); // Hashmap that contains Entity's current stats.
    protected Dictionary<string, float> baseStatsDict = new Dictionary<string, float>(); // Hashmap that contains Entity's base stats.
    protected Dictionary<string, float> effectDict = new Dictionary<string, float>(); // Hashmap that contains Entity's effects.

    protected GameManager gameManager;
    [HideInInspector] public Util util;

    protected Color savedColor = Color.white; // Saved color.
    protected Vector3 spawnPos;
    protected GameObject currentStunParticle; // Current stun particle.

    protected System.Action OnEffectWornOut;

    protected virtual void Awake()
    {
        // Initialize all stats to Hashmaps.
        foreach (Stats stat in statsList)
        {
            baseStatsDict[stat.name] = stat.value;
            statsDict[stat.name] = stat.value;
        }

        if (!rigidBody)
        {
            TryGetComponent(out rigidBody);
        }
    }

    protected virtual void Start()
    {
        gameManager = GameManager.instance;
        util = Util.instance;

        if (!spriteRenderer)
        {
            TryGetComponent(out spriteRenderer);
        }

        if (spriteRenderer)
        {
            savedColor = spriteRenderer.color;
        }

        spawnPos = transform.position;
    }

    protected virtual void Update()
    {
        // Update current value for Debugging on Inspector.
        foreach (Stats stat in statsList)
        {
            stat.UpdateValue(statsDict[stat.name]);
        }

        EffectUpdater();

        if (HasEffect("Stun"))
        {
            return;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (HasEffect("Stun"))
        {
            return;
        }
    }

    // Function when entity got knockback.
    public virtual void Knockback(Vector2 basePosition, Vector2 knockbackForce)
    {
        if (basePosition.x > transform.position.x)
        { // if the enemy is left to the player
            rigidBody.AddForce(new Vector2(-1f, 1f) * knockbackForce * 100f);
        }
        else
        { // if the enemy is right to the player
            rigidBody.AddForce(knockbackForce * 100f);
        }
    }

    // Function when entity took damages.
    public virtual void TakeDamage(Damage damage, GameObject causeObject)
    {
        StartCoroutine(DamageRenderer());
        DamageIndicator(damage, causeObject, "#FFFFFF", "#88CCFF");

        DecreaseStat("Health", damage.physicalDamage + damage.magicDamage);

        // Check if entity's healh is below or equal to 0, dead.
        if (GetStat("Health") <= 0f)
        {
            Die();
        }
    }

    // Function when entity is dead.
    protected virtual void Die()
    {
        // Check if entity is already dead, don't execute further.
        if (entityState == EntityState.DEAD)
        {
            return;
        }

        entityState = EntityState.DEAD;
        Destroy(gameObject);
    }

    // Function to Render damage as Damage Indicator.
    protected IEnumerator DamageRenderer()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        spriteRenderer.color = savedColor;
    }

    // Function to play Damage indicator.
    protected void DamageIndicator(Damage damage, GameObject causeObject, string colorCodePhysical, string colorCodeMagical)
    {
        // If show Damage indicator, instantiate prefab.
        if (showDamageIndicator)
        {
            GameObject damageIndicator = Instantiate(util.prefabs.damageIndicator, UIManager.instance.gameCanvas);
            TextMeshProUGUI indicatorText = damageIndicator.GetComponentInChildren<TextMeshProUGUI>();

            // If entity is on the left side, change damage indicator's direction.
            if (causeObject.transform.position.x >= transform.position.x)
            {
                damageIndicator.GetComponent<Animator>().SetBool("Right", false);
            }

            string damageText = ((int)damage.physicalDamage).ToString();
            string colorCode = colorCodePhysical;

            // If it's Magic Damage, use Blue color.
            if (damage.magicDamage > 0f)
            {
                colorCode = colorCodeMagical;
                damageText = ((int)damage.magicDamage).ToString();
            }

            indicatorText.text = indicatorText.text = "<color=" + colorCode + ">" + indicatorText.text;
            indicatorText.text = indicatorText.text.Replace("<value>", damageText);

            float randomX = Random.Range(damageIndicatorOffsetMin.x, damageIndicatorOffsetMax.x);
            float randomY = Random.Range(damageIndicatorOffsetMin.y, damageIndicatorOffsetMax.y);
            float randomSideX = Random.Range(-1f, 1f);

            Vector2 position = (Vector2)transform.position + new Vector2(randomX * randomSideX, randomY);
            damageIndicator.transform.position = Camera.main.WorldToScreenPoint(position);
            gameManager.damageIndicatorList.Add(damageIndicator, position);
        }
    }

    // Function to check if player is facing to the right.
    public bool LookRight()
    {
        return !spriteRenderer.flipX;
    }

    // Function to reset all stats to Base stats.
    public void ResetStat()
    {
        statsDict = baseStatsDict;
    }

    // Function to fetch Base stat by name.
    public float GetBaseStat(string statName)
    {
        return baseStatsDict[statName];
    }

    // Function to set Base stat by name.
    public void SetBaseStat(string statName, float value)
    {
        baseStatsDict[statName] = value;
        statsDict[statName] = baseStatsDict[statName];
    }

    // Function to fetch current stat by name.
    public float GetStat(string statName)
    {
        return statsDict[statName];
    }

    // Function to set current stat by name.
    public void SetStat(string statName, float value)
    {
        value = Mathf.Clamp(value, 0f, statsDict[statName]);
        statsDict[statName] = value;
    }

    // Function to set current stat by name (No clamp).
    public void SetStatUnclamp(string statName, float value)
    {
        statsDict[statName] = value;
    }

    // Function to increase Base stat by name.
    public void IncreaseBaseStat(string statName, float value)
    {
        if (baseStatsDict.ContainsKey(statName)) {
            baseStatsDict[statName] += value;
        } else {
            print("Cannot find stat named: " + statName);
        }
    }

    // Function to decrease Base stat by name.
    public void DecreaseBaseStat(string statName, float value)
    {
        baseStatsDict[statName] -= value;
    }

    // Function to increase current stat by name.
    public void IncreaseStat(string statName, float value)
    {
        statsDict[statName] += value;
        statsDict[statName] = Mathf.Clamp(statsDict[statName], 0f, baseStatsDict[statName]);
    }

    // Function to decrease current stat by name.
    public void DecreaseStat(string statName, float value)
    {
        statsDict[statName] -= value;
        statsDict[statName] = Mathf.Clamp(statsDict[statName], 0f, baseStatsDict[statName]);
    }

    // Function to increase current stat by name (No clamp).
    public void IncreaseStatUnclamp(string statName, float value)
    {
        statsDict[statName] += value;
    }

    // Function to update all effects duration.
    private void EffectUpdater()
    {
        List<string> expireEffectList = new List<string>(effectDict.Keys);
        
        foreach (string expireEffect in expireEffectList)
        {
            if (effectDict[expireEffect] > 0f)
            {
                effectDict[expireEffect] -= Time.deltaTime;
            }
            else
            {
                effectDict.Remove(expireEffect);
                
                if (expireEffect.Equals("Stun"))
                {
                    OnEffectWornOut.Invoke();

                    if (currentStunParticle)
                    {
                        Destroy(currentStunParticle.gameObject);
                    }
                }
            }
        }
    }

    // Function to determine if entity has an effect.
    public bool HasEffect(string name)
    {
        return effectDict.ContainsKey(name);
    }
    
    // Function to add effect to entity.
    public virtual void AddEffect(string name, float duration)
    {
        if (HasEffect(name))
        {
            effectDict[name] += duration;
        }
        else
        {
            if (currentStunParticle)
            {
                Destroy(currentStunParticle.gameObject);
            }

            Vector2 position = (Vector2) transform.position + (Vector2.up * spriteRenderer.size.x / 1.75f);
            currentStunParticle = Instantiate(util.particles.stunEffect, position, util.particles.stunEffect.transform.rotation);
            effectDict[name] = duration;
        }
    }

    // Function to remove effect from entity.
    public void RemoveEffect(string name)
    {
        effectDict.Remove(name);

        if (name.Equals("Stun") && currentStunParticle)
        {
            Destroy(currentStunParticle.gameObject);
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (showDamageIndicatorArea)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, damageIndicatorOffsetMin);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, damageIndicatorOffsetMax);
        }
    }

    public Vector3 GetSpawnPos() {
        return spawnPos;
    }

}

[System.Serializable]
public class Stats
{
    public string name;
    public float value;
    [ReadOnly]
    public float currentValue; // This value use for showing on Inspector in Debugging mode.

    public Stats(string name, float value)
    {
        this.name = name;
        this.value = value;
    }

    // Function to update current value.
    public void UpdateValue(float value)
    {
        currentValue = value;
    }
}

// Base Damage
[System.Serializable]
public class Damage
{
    public float magicDamage;
    public float physicalDamage;

    public Damage(float physicalDamage, float magicDamage)
    {
        this.magicDamage = magicDamage;
        this.physicalDamage = physicalDamage;
    }
}

[System.Serializable]
public class Shrine
{
    [Header("Shrine Settings")]
    [SerializeField] protected float physicalArmor; // Physical Armor.
    [SerializeField] protected float magicArmor; // Magic Armor.
    [SerializeField] protected Vector2 effectRange = Vector2.one; // Effect range.
    [SerializeField] protected Vector2 effectOffset = Vector2.zero; // Effect offset.
}

public class ReadOnlyAttribute : PropertyAttribute
{

}
