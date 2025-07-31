using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Item;

public class ItemUpgradeManager : MonoBehaviour
{
    public List<ItemUpgradeShop> itemUpgradeList = new List<ItemUpgradeShop>(); // Shop for Item Upgrade.

    [Header("Components")]
    [SerializeField] private Transform contentParent; // Parent of all Item Upgrade label GameObject.
    [SerializeField] private GameObject labelPrefab; // Prefab to spawn on UI as parent.

    private GameManager gameManager;
    private UpgradeManager upgradeManager;

    private void Start()
    {
        gameManager = GameManager.instance;
        upgradeManager = UpgradeManager.instance;

        // Add OnClick event command to each Upgrade button of each stats.
        foreach (ItemUpgradeShop itemStat in itemUpgradeList)
        {
            // Check if there's no game object, spawn new one.
            if (!itemStat.labelPrefab)
            {
                itemStat.labelPrefab = Instantiate(labelPrefab, contentParent);
                itemStat.labelPrefab.name = itemStat.item.itemName;
            }

            Button upgradeButton = itemStat.labelPrefab.transform.Find("Upgrade Button").GetComponent<Button>();
            upgradeButton.onClick.AddListener(delegate
            {
                Purchase(itemStat);
            });
        }
    }

    private void Update()
    {
        UpgradeUpdater();
    }

    // Function to update all upgrades on UI.
    private void UpgradeUpdater()
    {
        foreach (ItemUpgradeShop stat in itemUpgradeList)
        {
            GameObject label = stat.labelPrefab;
            Item item = stat.item;
            UpgradeLevel currentUpgrade = item.GetCurrentUpgrade();

            int rarityLevel = 0;

            switch (item.itemRarity)
            {
                case RarityType.RARE:
                rarityLevel = 1;
                break;

                case RarityType.EPIC:
                rarityLevel = 2;
                break;
            }

            label.transform.Find("Locked").GetComponent<Image>().enabled = upgradeManager.itemRarityUnlockLevel < rarityLevel;

            label.GetComponent<Image>().color = item.rarityColor;
            label.transform.Find("Icon").GetComponent<Image>().sprite = item.image;
            label.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = item.itemName;

            string level = "Max";
            string price = "-";

            // Check if there's more upgrades, show upgrades information.
            if (currentUpgrade != null)
            {
                level = currentUpgrade.statLevel.ToString();
                price = currentUpgrade.price.ToString();
            }

            label.transform.Find("Level").GetComponent<TextMeshProUGUI>().text = "Lv. " + level;
            label.transform.Find("Price").GetComponent<TextMeshProUGUI>().text = price;

            label.transform.Find("Upgrade Button").GetComponent<Button>().interactable = currentUpgrade != null && currentUpgrade.unlocked;
        }
    }

    // Function when press Upgrade button
    public void Purchase(ItemUpgradeShop itemUpgradeShop)
    {
        UpgradeLevel upgrade = itemUpgradeShop.item.GetCurrentUpgrade();

        // Check if player has enough Gold to purchase / upgrade this stat.
        if (!upgrade.unlocked || gameManager.gold < upgrade.price)
        {
            return;
        }

        upgrade.purchased = true;
    }

    // Function to unlock to next level.
    public void UnlockLevel()
    {
        foreach (ItemUpgradeShop stat in itemUpgradeList)
        {
            Item item = stat.item;
            int nextUpgrade = item.GetLastUpgrade() + 1;

            // Check upgrade hasn't reaches maximum.
            if (item.upgradeLevel.Count > nextUpgrade)
            {
                item.upgradeLevel[nextUpgrade].unlocked = true;
            }
        }
    }
}

[System.Serializable]
public class ItemUpgradeShop
{
    [HideInInspector]
    public GameObject labelPrefab; // Label prefab that shown on Shop UI.
    public Item item; // Upgrade of this item.
}
