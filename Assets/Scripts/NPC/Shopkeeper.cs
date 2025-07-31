using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shopkeeper : Interactable
{
    [Header("Shopkeeper Settings")]
    [SerializeField] private int buyLimit = 2; // Buy limitation.

    [HideInInspector] public int buyCount;

    private bool generated = false; // Determine if item is already generated or not.
    [SerializeField] private List<ItemShop> itemList = new List<ItemShop>();

    public void LoadItemData()
    {
        ItemShopManager shopManager = UIManager.instance.itemInGameShopManager;
        shopManager.gameObject.SetActive(true);

        shopManager.buyLimit = buyLimit;
        shopManager.buyCount = buyCount;
        shopManager.SetShopkeeper(this);

        if (!generated)
        {
            print("GENERATE " + name);
            generated = true;

            shopManager.Reroll(true);
            foreach (ItemShop item in shopManager.itemShopList)
            {
                itemList.Add(new ItemShop(item.item, item.labelPrefab));
            }
        }
        else
        {
            print("LOAD ITEM " + name);
            for (int i = 0; i < shopManager.itemShopList.Count; i++)
            {
                shopManager.itemShopList[i].item = itemList[i].item;
                shopManager.itemShopList[i].labelPrefab = itemList[i].labelPrefab;
            }
        }
    }

    // Function to update item stock.
    public void UpdateItem(List<ItemShop> itemShop)
    {
        for (int i = 0; i < itemList.Count; i++)
        {
            itemList[i].item = itemShop[i].item;
        }
    }
}
