using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Item;

[CreateAssetMenu(fileName = "New Items", menuName = "Items")]
public class Item : ScriptableObject
{
    public enum PassiveType
    {
        HEALTH, RESISTANCE, MOVEMENT_SPEED, AGILITY, STRENGTH, MAGIC_POWER, DAMAGE, PHYSICAL_ARMOR, MAGIC_ARMOR, MANA, MANA_REGENERATION, SOUL, GOLD, AREA_DAMAGE, REVIVE, SLOW_IMMUNE, RANGE, PENETRATION_PROJECTILE, TIME, CONDITIONAL
    }

    public enum RarityType
    {
        COMMON, RARE, EPIC
    }

    public string itemName; // Type of item.
    public RarityType itemRarity = RarityType.COMMON; // Rarity of item.
    public Color rarityColor = Color.white; // Color of rarity.
    [TextArea(10, 20)]
    public string itemDescription; // Description of item.
    public Sprite image; // Image of item.
    public int itemPrice; // Price of item.
    public List<Item_Stat> passives = new List<Item_Stat>(); // List of all passive effects.
    public List<UpgradeLevel> upgradeLevel = new List<UpgradeLevel>(); // List of level to upgrade.

    // Function to get current stat level. (Start with Level 0)
    public UpgradeLevel GetCurrentLevel()
    {
        int highestLevel = 0;
        UpgradeLevel currentLevel = null;

        foreach (UpgradeLevel upgrade in upgradeLevel)
        {
            if (upgrade.statLevel >= highestLevel && upgrade.purchased)
            {
                highestLevel = upgrade.statLevel;
                currentLevel = upgrade;
            }
        }

        return currentLevel;
    }

    // Function to get current upgrade progress (If stat is now Level 1, Upgrade will be next one which is Level 2).
    public UpgradeLevel GetCurrentUpgrade()
    {
        int lowestLevel = upgradeLevel.Count;
        UpgradeLevel currentUpgrade = null;

        foreach (UpgradeLevel upgrade in upgradeLevel)
        {
            if (upgrade.statLevel <= lowestLevel && !upgrade.purchased)
            {
                lowestLevel = upgrade.statLevel;
                currentUpgrade = upgrade;
            }
        }

        return currentUpgrade;
    }

    // Function to get the furthest upgrade that can go.
    public int GetLastUpgrade()
    {
        int latest = -1;

        for (int i = 0; i < upgradeLevel.Count; i++)
        {
            if (upgradeLevel[i].unlocked)
            {
                latest = i;
            }
        }

        return latest;
    }

    // Function to return total modify value (Combine all modify values up until now)
    public float GetTotalModifyValue()
    {
        float totalModifyValue = passives[0].modifyValue;

        // Check if it's Common Item, return default value.
        if (itemRarity == RarityType.COMMON)
        {
            return totalModifyValue;
        }

        int currentLevel = 0;

        // Check if stat is now Level 1 or higher.
        if (GetCurrentLevel() != null)
        {
            currentLevel = GetCurrentLevel().statLevel;
        }

        foreach (UpgradeLevel upgrade in upgradeLevel)
        {
            if (upgrade.statLevel <= currentLevel)
            {
                totalModifyValue += upgrade.statModify;
            }
        }

        return totalModifyValue;
    }

    // Function to return total modify value (Combine all modify values up until now)
    public float GetTotalModifyValue(PassiveType passiveType)
    {
        if (GetPassive(passiveType) == null)
        {
            return 0f;
        }

        float totalModifyValue = GetPassive(passiveType).modifyValue;

        // Check if it's Common Item, return default value.
        if (itemRarity == RarityType.COMMON)
        {
            return totalModifyValue;
        }

        int currentLevel = 0;

        // Check if stat is now Level 1 or higher.
        if (GetCurrentLevel() != null)
        {
            currentLevel = GetCurrentLevel().statLevel;
        }

        foreach (UpgradeLevel upgrade in upgradeLevel)
        {
            if (upgrade.statLevel <= currentLevel)
            {
                totalModifyValue += upgrade.statModify;
            }
        }

        return totalModifyValue;
    }

    // Function to fetch Passive by type.
    public Item_Stat GetPassive(PassiveType type)
    {
        foreach (Item_Stat stats in passives)
        {
            if (stats.passiveType.Equals(type))
            {
                return stats;
            }
        }

        return null;
    }

    // Function to Reset upgrade progress
    public void Reset()
    {
        for (int i = 0; i < upgradeLevel.Count; i++)
        {
            upgradeLevel[i].unlocked = true;
            upgradeLevel[i].purchased = false;
        }
    }
}

[System.Serializable]
public class Item_Stat
{
    public string statName; // Name of the stat. (Visualize on Inspector)
    public PassiveType passiveType; // Passive type that use to modify stats.
    public float modifyValue; // Value to modify.
    [Range(0, 100)]
    public float modifyChance = 100f; // Chance to modify. (0 - 100)
}
