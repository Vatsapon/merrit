using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatUpgradeManager : MonoBehaviour
{
    public List<StatUpgradeShop> statUpgradeList = new List<StatUpgradeShop>(); // Shop for Stat Upgrade.

    private GameManager gameManager;
    private Player player;
    public static StatUpgradeManager instance;

    private void Awake() {
        instance = this;
    }

    private void Start()
    {
        player = Player.instance;
        gameManager = GameManager.instance;

        // Add OnClick event command to each Upgrade button of each stats.
        foreach (StatUpgradeShop stat in statUpgradeList)
        {
            Button upgradeButton = stat.labelPrefab.transform.Find("Upgrade Button").GetComponent<Button>();
            upgradeButton.onClick.AddListener(delegate
            {
                Purchase(stat);
            });
        }
    }

    private void Update()
    {
        UpgradeUpdater();

        // DEBUGGING - [R] Reset Upgrade Progress.
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (Upgrade upgrade in Resources.LoadAll("Stats"))
            {
                upgrade.Reset();
            }
        }
    }

    // Function to update all upgrades on UI.
    private void UpgradeUpdater()
    {
        foreach (StatUpgradeShop stat in statUpgradeList)
        {
            GameObject label = stat.labelPrefab;
            Upgrade upgrade = stat.upgrade;
            UpgradeLevel currentUpgrade = upgrade.GetCurrentUpgrade();

            label.transform.Find("Icon").GetComponent<Image>().sprite = upgrade.statImage;
            label.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = upgrade.statName;

            string level = "Max";
            string statModify = "";
            string price = "-";

            // Check if there's more upgrades, show upgrades information.
            if (currentUpgrade != null)
            {
                level = currentUpgrade.statLevel.ToString();

                if (currentUpgrade.statModify >= 0)
                {
                    statModify = "+";
                }
                else
                {
                    statModify = "-";
                }

                statModify += currentUpgrade.statModify;
                price = currentUpgrade.price.ToString();
            }

            label.transform.Find("Level").GetComponent<TextMeshProUGUI>().text = "Lv. " + level;
            label.transform.Find("Modify").GetComponent<TextMeshProUGUI>().text = statModify;
            label.transform.Find("Price").GetComponent<TextMeshProUGUI>().text = price;

            label.transform.Find("Upgrade Button").GetComponent<Button>().interactable = currentUpgrade != null && currentUpgrade.unlocked;
        }
    }

    // Function when press Upgrade button
    public void Purchase(StatUpgradeShop statUpgradeShop)
    {
        UpgradeLevel upgrade = statUpgradeShop.upgrade.GetCurrentUpgrade();

        // Check if player has enough Gold to purchase / upgrade this stat.
        if (!upgrade.unlocked || gameManager.gold < upgrade.price)
        {
            print(upgrade.unlocked);
            return;
        }

        upgrade.purchased = true;

        player.IncreaseBaseStat(statUpgradeShop.upgrade.statName, upgrade.statModify);
        player.ResetStat();
    }

    // Function to unlock to next level.
    public void UnlockLevel()
    {
        foreach (StatUpgradeShop stat in statUpgradeList)
        {
            Upgrade upgrade = stat.upgrade;
            int nextUpgrade = upgrade.GetLastUpgrade() + 1;

            // Check upgrade hasn't reaches maximum.
            if (upgrade.upgradeLevel.Count > nextUpgrade)
            {
                upgrade.upgradeLevel[nextUpgrade].unlocked = true;
            }
        }
    }
}

[System.Serializable]
public class StatUpgradeShop
{

    public StatUpgradeShop(GameObject la, Upgrade up) {
        labelPrefab = la;
        upgrade = up;
    }

    public GameObject labelPrefab; // Label prefab that shown on Shop UI.
    public Upgrade upgrade; // Upgrade of this stat.
}