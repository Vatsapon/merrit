using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Ability;

public class AbilityUpgradeManager : MonoBehaviour
{
    [Header("Settings")]
    public List<GameObject> levelList = new List<GameObject>(); // List of all Level description on UI.
    public List<AbilityUpgrade> abilityList = new List<AbilityUpgrade>(); // List of all abilities object on UI.

    [Header("Components")]
    [SerializeField] private TextMeshProUGUI soulCoinText; // Text that shown amount of Soul Coin.
    [SerializeField] private GameObject descriptionPanel; // Panel that contains all descriptions.
    [SerializeField] private TextMeshProUGUI abilityName; // Ability's name.
    [SerializeField] private TextMeshProUGUI abilityLevelText; // Ability's level.
    [SerializeField] private TextMeshProUGUI abilityDescription; // Ability's description.
    [SerializeField] private TextMeshProUGUI priceText; // Text to show price.
    [SerializeField] private Button upgradeButton; // Button to upgrade.

    private string priceTextFormat; // Text format for price.
    private AbilityUpgrade currentAbility; // Selected ability.

    private GameManager gameManager;
    private UpgradeManager upgradeManager;

    private void Start()
    {
        gameManager = GameManager.instance;
        upgradeManager = UpgradeManager.instance;

        // Add OnClick event command to each Ability button.
        foreach (AbilityUpgrade ability in abilityList)
        {
            Button button = ability.labelPrefab.transform.Find("Button").GetComponent<Button>();
            button.onClick.AddListener(delegate
            {
                Select(ability);
            });
        }

        currentAbility = null;
        priceTextFormat = priceText.text;
    }

    private void Update()
    {
        AbilityButtonUpdater();
        DescriptionUpdater();
    }

    // Function to update Ability button.
    private void AbilityButtonUpdater()
    {
        for (int i = 0; i < abilityList.Count; i++)
        {
            abilityList[i].labelPrefab.transform.Find("Locked").GetComponent<Image>().enabled = upgradeManager.abilityUnlockLevel <= i;
            abilityList[i].labelPrefab.transform.Find("Icon").GetComponent<Image>().sprite = abilityList[i].ability.image;
            abilityList[i].ability.unlocked = upgradeManager.abilityUnlockLevel > i;
        }
    }

    // Function to update item and description.
    private void DescriptionUpdater()
    {
        soulCoinText.text = gameManager.soulCoin.ToString();
        descriptionPanel.SetActive(currentAbility != null);

        // Check if player is selecting ability.
        if (currentAbility != null)
        {
            Ability ability = currentAbility.ability;
            Ability_Level currentUpgrade = ability.GetCurrentUpgrade();

            abilityName.text = currentAbility.abilityName;

            int level = 1;

            if (ability.GetCurrentLevel().statLevel > 0)
            {
                level = ability.GetCurrentLevel().statLevel + 1;
            }

            abilityLevelText.text = "Lv. " + level.ToString();
            abilityDescription.text = ability.abilityDescription;

            for (int i = 0; i < levelList.Count; i++)
            {
                TextMeshProUGUI levelText = levelList[i].transform.Find("Level").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI descriptionText = levelList[i].transform.Find("Description").GetComponent<TextMeshProUGUI>();
                levelText.text = "Lv. " + (i + 1);
                descriptionText.text = ability.abilityLevel[i].statDescription;

                // Check either has upgrade pass this level.
                if (ability.GetCurrentLevel().statLevel >= i)
                {
                    levelText.color = Color.yellow;
                }
                else
                {
                    levelText.color = Color.white;
                }
            }

            string price = "-";

            if (ability.GetCurrentUpgrade() != null)
            {
                price = ability.GetCurrentUpgrade().price.ToString();
            }

            priceText.text = priceTextFormat.Replace("<price>", price);

            upgradeButton.interactable = currentUpgrade != null;
        }
    }

    // Function to select an ability.
    public void Select(AbilityUpgrade ability)
    {
        currentAbility = ability;
    }

    // Function to unselect ability.
    public void Unselect()
    {
        currentAbility = null;
    }

    // Function to purchase upgrade.
    public void Purchase()
    {
        Ability_Level ability = currentAbility.ability.GetCurrentUpgrade();

        // Check if player has enough Soul Coin to purchase / upgrade this stat.
        if (gameManager.soulCoin < ability.price)
        {
            return;
        }

        ability.purchased = true;
    }

    // Function to unlock to next level.
    public void UnlockLevel()
    {
        upgradeManager = UpgradeManager.instance;

        AbilityButtonUpdater();
    }

    // Function to fetch Ability by type.
    public Ability GetAbility(AbilityType type)
    {
        foreach (AbilityUpgrade ability in abilityList)
        {
            if (ability.ability.abilityType == type)
            {
                return ability.ability;
            }
        }

        return null;
    }
}

[System.Serializable]
public class AbilityUpgrade
{
    public string abilityName; // Name of upgrade. (Use for visualize on Inspector)
    public Ability ability; // Type of upgrade.

    public GameObject labelPrefab; // Label object on UI.
}