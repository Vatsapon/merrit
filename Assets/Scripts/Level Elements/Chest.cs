using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Item;
using static Key;
using static UnityEngine.ParticleSystem;

public class Chest : Lootable
{
    [Header("Chest Setting")]
    public KeyType keyType = KeyType.BRONZE; // Required key to open this chest.
    [Space(5)]
    [SerializeField] private bool showInteractionRange = false; // Determine to show interaction range or not.
    public float interactionRange = 5f; // Range to interact to open chest.
    [SerializeField] private Vector2 itemSpreadForce = Vector2.zero; // Spread force for item.

    [Header("Item Chance")]
    [Range(0f, 1f)]
    [SerializeField] private float rareChance = 1f; // Rare item chance.
    [Range(0f, 1f)]
    [SerializeField] private float epicChance = 1f; // Epic item chance.

    [Header("References")]
    [SerializeField] private GameObject droppedItem;
    [SerializeField] private Sprite openSprite;

    private Player player;
    private Inventory inventory;

    private void Start()
    {
        player = Player.instance;
        inventory = Inventory.instance;
    }

    private void Update()
    {
        // If there's no more loots or doesn't have required key, don't do anything.
        if (IsLooted() || inventory.GetKeyAmount(keyType) <= 0)
        {   
            return;
        }

        float distance = Vector2.Distance(transform.position, player.transform.position);

        // If player is in interaction range, can open chest.
        if (distance <= interactionRange)
        {
            // [F] - Open Chest.
            if (Input.GetKeyDown(KeyCode.F))
            {
                player.IncreaseStatUnclamp("Chest Opened", 1);
                SFXLibrary.instance.CollectingChestSFX.PlayFeedbacks();

                inventory.RemoveKey(keyType);
                DropLoot();
                DropItem();

                Instantiate(Util.instance.particles.burstEffect, transform.position, Quaternion.identity);

                GetComponent<SpriteRenderer>().sprite = openSprite;
            }
        }
    }

    // Function to drop item with rarity.
    private void DropItem()
    {
        RarityType selectedRarity = RarityType.COMMON;

        float chance = Random.Range(0f, 1f);

        if (chance < epicChance)
        {
            selectedRarity = RarityType.RARE;
        }
        else
        {
            chance = Random.Range(0f, 1f);

            if (chance < rareChance)
            {
                selectedRarity = RarityType.COMMON;
            }
        }

        List<Item> items = new List<Item>();

        foreach (Item item in Resources.LoadAll("Items"))
        {
            items.Add(item);
        }
        
        if (selectedRarity == RarityType.EPIC && HasAllItemRarity(items, selectedRarity))
        {
            selectedRarity = RarityType.RARE;
        }

        if (selectedRarity == RarityType.RARE && HasAllItemRarity(items, selectedRarity))
        {
            selectedRarity = RarityType.COMMON;
        }

        if (selectedRarity == RarityType.COMMON && HasAllItemRarity(items, selectedRarity))
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemRarity != selectedRarity || Inventory.instance.HasItem(items[i]))
            {
                items.RemoveAt(i);
            }
        }

        Item selectedItem = items[Random.Range(0, items.Count)];

        GameObject droppedLoot = Instantiate(droppedItem, transform.position, Quaternion.identity);

        for (int i = 0; i < droppedLoot.transform.childCount; i++)
        {
            SpriteRenderer spriteRenderer;

            if (droppedLoot.transform.GetChild(i).TryGetComponent(out spriteRenderer))
            {
                spriteRenderer.sprite = selectedItem.image;
            }

            ParticleSystem particle;

            if (droppedLoot.transform.GetChild(i).TryGetComponent(out particle))
            {
                Color rarityColor = selectedItem.rarityColor;

                MainModule main = particle.main;
                main.startColor = new Color(rarityColor.r, rarityColor.g, rarityColor.b, main.startColor.color.a);
            }
        }

        DroppedItem droppable = droppedLoot.GetComponent<DroppedItem>();
        droppable.OnCollected.AddListener(delegate
        {
            Inventory.instance.AddItem(selectedItem);
        });

        Rigidbody2D rigidBody;

        // If item has rigidbody, make it spread away from the chest with spreadForce.
        if (droppedLoot.TryGetComponent(out rigidBody))
        {
            float forceX = Random.Range(-itemSpreadForce.x, itemSpreadForce.x);
            float forceY = Random.Range(0f, itemSpreadForce.y);

            rigidBody.velocity = Vector2.zero;
            rigidBody.AddForce(new Vector2(forceX, forceY) * 50f, ForceMode2D.Impulse);
        }
    }

    // Function to determine if player had all items with specific rarity.
    private bool HasAllItemRarity(List<Item> items, RarityType type)
    {
        int rarityCount = 0;
        int inventoryCount = 0;

        foreach (Item item in items)
        {
            if (item.itemRarity == type)
            {
                rarityCount++;

                if (Inventory.instance.HasItem(item))
                {
                    inventoryCount++;
                }
            }
        }

        return inventoryCount >= rarityCount;
    }

    private void OnDrawGizmos()
    {
        if (showInteractionRange)
        {
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}

public class Lootable : MonoBehaviour
{
    [Header("Loot Settings")]
    public Vector2 spreadForce = Vector2.one; // Force to spread item when chest has been opened.
    public List<Loot> loots = new List<Loot>(); // Loots that will drop from the chest.

    // Function to check if it already looted.
    protected bool IsLooted()
    {
        return loots.Count == 0;
    }

    // Function to drop all loots.
    protected void DropLoot()
    {
        foreach (Loot loot in loots)
        {
            for (int i = 0; i < loot.amount; i++)
            {
                GameObject droppedLoot = Instantiate(loot.prefab, transform.position, Quaternion.identity);
                Rigidbody2D rigidBody;

                // If item has rigidbody, make it spread away from the chest with spreadForce.
                if (droppedLoot.TryGetComponent(out rigidBody))
                {
                    float forceX = Random.Range(-spreadForce.x, spreadForce.x);
                    float forceY = Random.Range(0f, spreadForce.y);

                    rigidBody.AddForce(new Vector2(forceX, forceY) * 200f);
                }
            }
        }

        loots.Clear();
    }

    [System.Serializable]
    public class Loot
    {
        public string name;
        public GameObject prefab;
        public int amount;
    }
}
