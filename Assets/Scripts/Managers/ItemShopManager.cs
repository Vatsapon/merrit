using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Item;

public class ItemShopManager : MonoBehaviour
{
    [Header("Settings")]
    public int rerollPrice; // Price to Re-roll.
    public int buyLimit = -1; // Buy limitation. (-1 = Unlimited)
    public List<ItemShopChance> itemShopChanceList = new List<ItemShopChance>(); // Chance of each Item Rarity Upgrade level.

    public List<ItemShop> itemShopList = new List<ItemShop>(); // Items inside the shop.

    [Header("Components")]
    [SerializeField] private Button rerollButton; // Button to re-roll items.
    [SerializeField] private Inventory inventory;

    [Header("References")]
    public GameObject descriptionObject;
    public TextMeshProUGUI itemNameText;
    public Image itemIcon;
    public TextMeshProUGUI itemRarityText;
    public TextMeshProUGUI itemDescriptionText;

    private GameManager gameManager;
    private UpgradeManager upgradeManager;

    private List<Item> itemList = new List<Item>(); // List of item to random during Re-roll.
    private string itemRarityTextFormat;
    private GameObject selectedItem;

    [HideInInspector] public int buyCount = 0;
    private Shopkeeper currentShopkeeper;

    private void Start()
    {
        gameManager = GameManager.instance;
        upgradeManager = UpgradeManager.instance;
        itemRarityTextFormat = itemRarityText.text;

        Reroll(true);

        foreach (ItemShop itemShop in itemShopList)
        {
            Button button = itemShop.labelPrefab.transform.Find("Button").GetComponent<Button>();
            button.onClick.AddListener(delegate
            {
                Purchase(itemShop);
            });
        }
    }

    private void Update()
    {
        ItemUpdater();

        descriptionObject.SetActive(selectedItem);

        if (selectedItem)
        {
            foreach (ItemShop itemShop in itemShopList)
            {
                if (itemShop.labelPrefab.Equals(selectedItem) && itemShop.item)
                {
                    itemNameText.text = itemShop.item.itemName;
                    itemIcon.sprite = itemShop.item.image;
                    itemRarityText.text = itemRarityTextFormat.Replace("<rarity>", "<color=#" + ColorUtility.ToHtmlStringRGB(itemShop.item.rarityColor) + ">" + itemShop.item.itemRarity.ToString() + "</color>");
                    itemDescriptionText.text = itemShop.item.itemDescription;

                    break;
                }
            }
        }
    }

    // Function to update all items in the shop.
    private void ItemUpdater()
    {
        bool rerollable = false;

        foreach (ItemShop item in itemShopList)
        {
            if (item.item == null)
            {
                item.labelPrefab.transform.Find("Icon").GetComponent<Image>().sprite = null;
                item.labelPrefab.transform.Find("Frame").GetComponent<Image>().color = Color.white;
                item.labelPrefab.transform.Find("Price").GetComponent<TextMeshProUGUI>().text = "-";
            }
            else
            {
                rerollable = true;

                item.labelPrefab.transform.Find("Icon").GetComponent<Image>().sprite = item.item.image;
                item.labelPrefab.transform.Find("Frame").GetComponent<Image>().color = item.item.rarityColor;
                item.labelPrefab.transform.Find("Price").GetComponent<TextMeshProUGUI>().text = item.item.itemPrice.ToString();
            }

            item.labelPrefab.transform.Find("Blank").GetComponent<Image>().enabled = item.item == null || buyCount == buyLimit;
        }

        if (rerollButton)
        {
            rerollable = rerollPrice <= gameManager.gold && rerollable;
            rerollButton.interactable = rerollable;
        }
    }

    // Function to purchase item from shop.
    public void Purchase(ItemShop itemShop)
    {
        if (itemShop.item.itemPrice > gameManager.gold)
        {
            return;
        }

        buyCount++;
        inventory.AddItem(itemShop.item);
        itemShop.item = null;

        if (currentShopkeeper)
        {
            currentShopkeeper.buyCount++;
            currentShopkeeper.UpdateItem(itemShopList);
        }
    }

    // Function to Re-roll the shop.
    public void Reroll(bool allSlot)
    {
        upgradeManager = UpgradeManager.instance;

        itemList.Clear();

        // Fetches all items (based on unlock upgrade in UpgradeManager).
        foreach (Item item in Resources.LoadAll("Items"))
        {
            // Check if player already has this item in an inventory.
            if (inventory.HasItem(item))
            {
                continue;
            }

            // Check rarity depends on Upgrade level of Item Rarity in UpgradeManager.
            switch (item.itemRarity)
            {
                case RarityType.COMMON:
                itemList.Add(item);
                break;

                case RarityType.RARE:

                if (upgradeManager.itemRarityUnlockLevel >= 1)
                {
                    itemList.Add(item);
                }

                break;

                case RarityType.EPIC:

                if (upgradeManager.itemRarityUnlockLevel >= 2)
                {
                    itemList.Add(item);
                }

                break;
            }
        }

        // Replace Item with new one.
        foreach (ItemShop itemShop in itemShopList)
        {
            if (!allSlot && itemShop.item == null)
            {
                continue;
            }

            Item newItem = FetchRandomItem();
            itemShop.item = newItem;

            itemList.Remove(newItem);
        }

        itemList.Clear();
    }

    // Fetch item rarity with random chance.
    private Item FetchRandomItem()
    {
        ItemShopChance itemChance = itemShopChanceList[upgradeManager.itemRarityUnlockLevel];

        float randomChance = Random.Range(0, 100);

        if (randomChance <= itemChance.epicChance)
        {
            return FetchItem(RarityType.EPIC);
        }

        if (randomChance <= itemChance.rareChance)
        {
            return FetchItem(RarityType.RARE);
        }

        return FetchItem(RarityType.COMMON);
    }

    // Fetch item depends on item rarity.
    private Item FetchItem(RarityType rarity)
    {
        List<Item> tempItemList = new List<Item>(itemList);

        for (int i = 0; i < tempItemList.Count; i++)
        {
            if (tempItemList[i].itemRarity != rarity)
            {
                tempItemList.RemoveAt(i);
            }
        }

        int randomIndex = Random.Range(0, tempItemList.Count);
        return tempItemList[randomIndex];
    }

    // Function to set select item.
    public void SetSelectedItem(GameObject gameObject)
    {
        selectedItem = gameObject;
    }

    // Function to clear select item.
    public void ClearSelectedItem()
    {
        selectedItem = null;
    }

    // Function to set current shopkeeper.
    public void SetShopkeeper(Shopkeeper shopkeeper)
    {
        currentShopkeeper = shopkeeper;
    }
}

[System.Serializable]
public class ItemShop
{
    public Item item; // Item that will be shown.
    public GameObject labelPrefab; // Label object in UI.

    public ItemShop(Item item, GameObject labelPrefab)
    {
        this.item = item;
        this.labelPrefab = labelPrefab;
    }
}

[System.Serializable]
public class ItemShopChance
{
    [Range(0f, 100f)]
    public float commonChance; // Chance to draw Common item.
    [Range(0f, 100f)]
    public float rareChance; // Chance to draw Rare item.
    [Range(0f, 100f)]
    public float epicChance; // Chance to draw Epic item.
}
