using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Upgrades", menuName = "Upgrades")]
public class Upgrade : ScriptableObject
{
    public string statName; // Name of the stat.
    [TextArea(10, 20)]
    public string statDescription; // Description of the stat.
    public Sprite statImage; // Sprite of the stat.

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
        float totalModifyValue = 0f;
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
    
    // Function to Reset upgrade progress
    public void Reset()
    {
        for (int i = 0; i < upgradeLevel.Count; i++)
        {
            upgradeLevel[i].unlocked = false;
            upgradeLevel[i].purchased = false;
        }
    }
}

[System.Serializable]
public class UpgradeLevel
{
    public int statLevel; // Level of the stat.
    public float statModify; // Value to modify the stat. (Ex. 50%, 14 P, etc.)
    public float price; // Price of this upgrade.
    public bool unlocked = false; // Either upgrade is lock or not.
    public bool purchased = false; // Either this upgrade level has been purchased.
}
