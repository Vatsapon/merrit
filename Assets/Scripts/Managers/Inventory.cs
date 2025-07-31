using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Key;

public class Inventory : MonoBehaviour
{
    public static Inventory instance;

    [Header("Datas")]
    public List<Slot> slotList = new List<Slot>(); // List that contains all slots.
    [SerializeField]
    private Dictionary<KeyType, int> keyList = new Dictionary<KeyType, int>(); // Dictionary that contains all keys.

    [Header("Components")]
    public Transform slotParent; // Parent Gameobject that contain all slots.
    public Transform keyParent; // Parent Gameobject that contain all keys.
    public GameObject slotPrefab; // Prefab to spawn a slot item.
    public GameObject keyPrefab; // Prefab to spawn a key.

    [Header("References")]
    public GameObject descriptionObject;
    public TextMeshProUGUI itemNameText;
    public Image itemIcon;
    public TextMeshProUGUI itemRarityText;
    public TextMeshProUGUI itemDescriptionText;

    private string itemRarityTextFormat;
    private Item selectedItem;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // Add all type of keys.
        foreach (KeyType keyType in Enum.GetValues(typeof(KeyType)))
        {
            keyList.Add(keyType, 0);
        }

        itemRarityTextFormat = itemRarityText.text;
    }

    private void Update()
    {
        descriptionObject.SetActive(selectedItem);

        if (selectedItem)
        {
            itemNameText.text = selectedItem.itemName;
            itemIcon.sprite = selectedItem.image;
            itemRarityText.text = itemRarityTextFormat.Replace("<rarity>", "<color=#" + ColorUtility.ToHtmlStringRGB(selectedItem.rarityColor) + ">" + selectedItem.itemRarity.ToString() + "</color>");
            itemDescriptionText.text = selectedItem.itemDescription;
        }
    }

    // Function to add slot.
    public void AddItem(Item item)
    {
        if (!HasItem(item))
        {
            GameObject slotObject = Instantiate(slotPrefab, slotParent);
            slotObject.name = item.itemName;
            Image slotImage = slotObject.transform.Find("Icon").GetComponent<Image>();
            slotImage.sprite = item.image;
            slotObject.transform.Find("Panel").GetComponent<Image>().color = item.rarityColor;
            slotObject.GetComponent<Button>().onClick.AddListener(delegate
            {
                SetSelectedItem(item);
            });
            
            Slot slot = new Slot(item, slotObject, slotImage);
            slotList.Add(slot);
            Player.instance.ApplyBuff();
        }
    }

    // Function to remove slot.
    public void RemoveItem(Item item)
    {
        if (HasItem(item))
        {
            GameObject slotObject = FindSlot(item).slotObject;
            Destroy(slotObject);

            slotList.Remove(FindSlot(item));
        }
    }

    // Function to clear inventory.
    public void Clear()
    {
        for (int i = slotList.Count - 1; i >= 0; i--) //modified this
        {
            Destroy(slotList[i].slotObject); 
            slotList.RemoveAt(i);
        }

        slotList.Clear();
    }

    // Function to get item (ScriptableObject) from name. (return null, if there's no such item)
    public Item GetItem(string itemName)
    {
        foreach (Item item in Resources.LoadAll("Items"))
        {
            if (item.itemName.Equals(itemName))
            {
                return item;
            }
        }

        return null;
    }

    // Function to check if player has this item in an inventory.
    public bool HasItem(Item item)
    {
        return FindSlot(item) != null;
    }

    // Function to find a slot that has this item.
    public Slot FindSlot(Item item)
    {
        foreach (Slot slot in slotList)
        {
            if (slot.item.Equals(item))
            {
                return slot;
            }
        }

        return null;
    }

    // Function to add key.
    public void AddKey(KeyType type)
    {
        keyList[type]++;
    }

    // Function to remove key.
    public void RemoveKey(KeyType type)
    {
        keyList[type]--;

        if (keyList[type] <= 0)
        {
            keyList[type] = 0;
        }
    }

    // Function to clear key.
    public void ClearKey()
    {
        foreach (KeyType keyType in Enum.GetValues(typeof(KeyType)))
        {
            keyList[keyType] = 0;
        }
    }

    // Function to get amount of key.
    public int GetKeyAmount(KeyType type)
    {
        return keyList[type];
    }

    // Function set select item.
    public void SetSelectedItem(Item item)
    {
        selectedItem = item;
    }
}

[System.Serializable]
public class Slot
{
    public Item item;
    public GameObject slotObject;
    public Image slotImage;
    public bool hasApplied;

    public Slot(Item item, GameObject slotObject, Image slotImage)
    {
        this.item = item;
        this.slotObject = slotObject;
        this.slotImage = slotImage;
        hasApplied = false;
    }
}
