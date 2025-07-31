using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Ability;

[CreateAssetMenu(fileName = "New Ability", menuName = "Abilities")]
public class Ability : ScriptableObject
{
    public enum AbilityType
    {
        DASH, TWISTED_DIVE, CIRCLE_TWIRL, SPIRITUAL_COMBO, SPIRITUAL_RESONANCE, SPIRIT_CALLING
    }

    public enum AbilityModifyType
    {
        DAMAGE, RANGE, AMOUNT, TIME
    }

    public AbilityType abilityType; // Type of ability.
    public Sprite image; // Image of ability.
    [TextArea(10, 20)]
    public string abilityDescription; // Description of the ability.
    public bool unlocked = false; // Determine if ability is unlocked or not.
    public List<Ability_Level> abilityLevel = new List<Ability_Level>(); // List of Ability stats to modify.

    // Function to get current stat level. (Start with Level 0)
    public Ability_Level GetCurrentLevel()
    {
        int highestLevel = 0;
        Ability_Level currentLevel = null;

        foreach (Ability_Level upgrade in abilityLevel)
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
    public Ability_Level GetCurrentUpgrade()
    {
        int lowestLevel = abilityLevel.Count;
        Ability_Level currentUpgrade = null;

        foreach (Ability_Level upgrade in abilityLevel)
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

        for (int i = 0; i < abilityLevel.Count; i++)
        {
            if (abilityLevel[i].purchased)
            {
                latest = i;
            }
        }

        return latest;
    }

    // Function to return total modify value (Combine all modify values up until now)
    public float GetTotalModifyValue(AbilityModifyType modifyType)
    {
        float totalModifyValue = 0f;
        int currentLevel = 0;

        // Check if stat is now Level 1 or higher.
        if (GetCurrentLevel() != null)
        {
            currentLevel = GetCurrentLevel().statLevel;
        }

        foreach (Ability_Level upgrade in abilityLevel)
        {
            if (upgrade.statLevel <= currentLevel)
            {
                foreach (Ability_Stat modify in upgrade.abilityStatList)
                {
                    if (modify.modifyType.Equals(modifyType))
                    {
                        totalModifyValue += modify.modifyValue;
                    }
                }
            }
        }

        return totalModifyValue;
    }

    // Function to Reset upgrade progress
    public void Reset()
    {
        for (int i = 0; i < abilityLevel.Count; i++)
        {
            abilityLevel[i].purchased = false;
        }

        abilityLevel[0].purchased = true;
        unlocked = false;
    }
}

[System.Serializable]
public class Ability_Stat
{
    public AbilityModifyType modifyType; // Ability Modify type that use to modify stats.
    public float modifyValue; // Modify value.
}

[System.Serializable]
public class Ability_Level
{
    public int statLevel; // Level of the stat.
    [TextArea(10, 20)]
    public string statDescription; // Description of the stat.
    public List<Ability_Stat> abilityStatList = new List<Ability_Stat>(); // Ability stat list.
    public float price; // Price of this upgrade.
    public bool purchased = false; // Either this upgrade level has been purchased.
}
