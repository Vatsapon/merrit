using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static UpgradeManager;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager instance;

    public enum UpgradeType
    {
        STAT, ABILITY, ITEM_RARITY
    }

    [Header("Datas")]
    public int statUnlockLevel = 0; // Current unlock level of Stats.
    public int abilityUnlockLevel = 0; // Current unlock level of Abilities.
    public int itemRarityUnlockLevel = 0; // Current unlock level of Item Rarity.

    [Space(10)]
    public List<Upgrades> upgradeList = new List<Upgrades>();

    [Header("Components")]
    public TextMeshProUGUI explorationCoinText; // Text that shown amount of Exploration Coin.
    public StatUpgradeManager statUpgradeManager;
    public AbilityUpgradeManager abilityUpgradeManager;

    private GameManager gameManager;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        gameManager = GameManager.instance;

        // Add OnClick event command to each Upgrade button of each one.
        foreach (Upgrades upgrade in upgradeList)
        {
            Button button = upgrade.labelPrefab.transform.Find("Button").GetComponent<Button>();
            button.onClick.AddListener(delegate
            {
                Purchase(upgrade);
            });
        }

        // DEBUGGING - RESET ALL UPGRADE PROGRESS.
        foreach (Item item in Resources.LoadAll("Items"))
        {
            item.Reset();
        }

        foreach (Ability ability in Resources.LoadAll("Abilities"))
        {
            ability.Reset();
        }
        
        foreach (Upgrade stat in Resources.LoadAll("Stats"))
        {
            stat.Reset();
        }
    }

    private void Update()
    {
        UpgradeUpdater();
    }
    
    // Function to update all upgrades on UI.
    private void UpgradeUpdater()
    {
        foreach (Upgrades upgrade in upgradeList)
        {
            bool locked = false;
            int level = 0;

            switch (upgrade.upgradeType)
            {
                case UpgradeType.STAT:
                level = statUnlockLevel;
                break;

                case UpgradeType.ABILITY:
                level = abilityUnlockLevel;
                break;

                case UpgradeType.ITEM_RARITY:
                level = itemRarityUnlockLevel;
                break;
            }

            if (upgrade.level > level + 1)
            {
                locked = true;
            }

            upgrade.labelPrefab.transform.Find("Purchased").gameObject.SetActive(upgrade.level < level + 1);
            upgrade.labelPrefab.transform.Find("Locked").gameObject.SetActive(locked);
            upgrade.labelPrefab.transform.Find("Button").GetComponent<Image>().raycastTarget = !locked;
        }

        explorationCoinText.text = gameManager.explorationCoin.ToString();
    }

    // Function when player purchase an upgrade.
    public void Purchase(Upgrades upgrade)
    {
        // Check if player doesn't have enough Exploration Coin, return.
        if (upgrade.price > gameManager.explorationCoin)
        {
            return;
        }

        switch (upgrade.upgradeType)
        {
            case UpgradeType.STAT:

            statUpgradeManager.UnlockLevel();
            statUnlockLevel++;

            break;

            case UpgradeType.ABILITY:

            abilityUpgradeManager.UnlockLevel();
            abilityUnlockLevel++;

            break;

            case UpgradeType.ITEM_RARITY:

            itemRarityUnlockLevel++;

            break;
        }
    }
}

[Serializable]
public class Upgrades
{
    public string upgradeName; // Name of upgrade. (Use for visualize on Inspector)
    public UpgradeType upgradeType; // Type of upgrade.
    public int level; // Level to upgrade.
    public int price; // Price of the upgrade.

    public GameObject labelPrefab; // Label object in UI.
}
