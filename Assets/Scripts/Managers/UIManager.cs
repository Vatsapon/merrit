using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MoreMountains.Feedbacks;
using UnityEngine.SceneManagement;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Player Dead")]
    public GameObject gameplay;
    public GameObject diesScreen;
    public float delayRestartSceneAFterPressingAnyButton = 1;
    private float timer;
    private bool shouldRestart = false;

    [Header("HUD References")]
    public Transform UIParent; // UI parent in GameCanvas object.
    public Transform gameCanvas; // Game Canvas Recttransform.
    public GameObject inventoryWindow; // Inventory window.
    public GameObject pauseScreen; // Pause Screen.
    public GameObject mapScreen; // Map Screen.
    public ItemShopManager itemInGameShopManager; // Item In-game shop.
    public GameObject enterForestPrompt; // Prompt for Entering the Forest.
    public GameObject sendCoinPrompt; // Prompt before sending coin.
    public GameObject defeatScreen; // Defeat Screen.
    [Space(5)]
    public GameObject health; // Health object with bar and text.
    public GameObject mana; // Mana object with bar and text.
    public GameObject manaIcon; // Mana (icon) object with cooldown.
    public GameObject explorationBar; // Exploration bar object with bar and animator.
    public GameObject soulBar; // Soul bar object with bar and animator.
    public TextMeshProUGUI goldText; // Gold text shown on HUD.
    public TextMeshProUGUI soulText; // Soul text shown on HUD.
    [Space(5)]
    [SerializeField] private Sprite bigBarHealth;
    [SerializeField] private Sprite bigBarMana;
    [SerializeField] private Sprite smallBarHealth;
    [SerializeField] private Sprite smallBarMana;
    [Space(5)]
    [SerializeField] private GameObject physicalAbilities; // Abilities in Physical form.
    [SerializeField] private GameObject spiritualAbilities; // Abilities in Spiritual form.

    [SerializeField] private List<GameObject> abilityList = new List<GameObject>(); // List of Abilities on UI.

    [Space(10)]
    [SerializeField] private TextMeshProUGUI bronzeKeyAmount; // Amount of collected bronze key.
    [SerializeField] private TextMeshProUGUI silverKeyAmount; // Amount of collected silver key.
    [SerializeField] private TextMeshProUGUI goldKeyAmount; // Amount of collected gold key.

    [Header("Defeat Screen References")]
    [SerializeField] private TextMeshProUGUI goldSentBack; // Amount of gold that will be sent back.
    [SerializeField] private TextMeshProUGUI goldCollected; // Amount of gold that player collected.
    [SerializeField] private TextMeshProUGUI mapDiscovered; // Percentage of discovered part of the map.
    [SerializeField] private TextMeshProUGUI clearTime; // Amount of played time.
    [SerializeField] private TextMeshProUGUI chestOpened; // Amount of chest that player opened.
    [SerializeField] private TextMeshProUGUI enemyKilled; // Amount of enemy that player killed.
    [SerializeField] private TextMeshProUGUI damageReceived; // Amount of damages that player received.
    [SerializeField] private Transform itemList; // Item list.

    private string goldTextFormat;
    private string soulTextFormat;
    private Vector2 barHugeSize; // Saved data of Huge bar. (Use for Health and Mana bar)
    private Vector2 barSmallSize; // Saved data of Small bar. (Use for Health and Mana bar)
    private float amountHugeSize; // Saved data of Huge text size. (Use for Health and Mana bar)
    private float amountSmallSize; // Saved data of Small text size. (Use for Health and Mana bar)
    [SerializeField] public string sendCoinTextFormat;

    private GameManager gameManager;
    private Player player;
    private UpgradeManager upgradeManager;

    [Header("Feedbacks")]
    public MMFeedbacks pauseMenuSoundFilter;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        gameManager = GameManager.instance;
        player = Player.instance;
        upgradeManager = UpgradeManager.instance;

        barHugeSize = health.GetComponent<RectTransform>().sizeDelta;
        barSmallSize = mana.GetComponent<RectTransform>().sizeDelta;
        amountHugeSize = health.transform.Find("AmountText").GetComponent<TextMeshProUGUI>().fontSize;
        amountSmallSize = mana.transform.Find("AmountText").GetComponent<TextMeshProUGUI>().fontSize;

        goldTextFormat = goldText.text;
        soulTextFormat = soulText.text;
        sendCoinTextFormat = sendCoinPrompt.transform.Find("Panel/Title").GetComponent<TextMeshProUGUI>().text;
    }

    private void Update()
    {
        //PlayerDeadSystem
        PlayerDead();

        /*
        //Pause Screen SFX
        if (pauseScreen.activeInHierarchy)
            pauseMenuSoundFilter.PlayFeedbacks();
        else
            pauseMenuSoundFilter.StopFeedbacks();

        */
        HUDUpdater();

    }

    private void PlayerDead()
    {
        /*
        gameplay.SetActive(player.entityState.Equals(Entity.EntityState.ALIVE));
        diesScreen.SetActive(player.entityState.Equals(Entity.EntityState.DEAD));
        */

        if (diesScreen.activeInHierarchy && Input.anyKeyDown)
        { timer = 0; shouldRestart = true; }
        if (diesScreen.activeInHierarchy)
        {
            if (shouldRestart)
            {
                timer += Time.deltaTime;
                if (timer > delayRestartSceneAFterPressingAnyButton)
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }

    // Function to update all HUDs.
    private void HUDUpdater()
    {
        Image healthBar = health.transform.Find("Bar").GetComponent<Image>();
        Image manaBar = mana.transform.Find("Bar").GetComponent<Image>();
        Image manaCooldown = manaIcon.transform.Find("Cooldown").GetComponent<Image>();

        RectTransform healthBarRect = health.GetComponent<RectTransform>();
        RectTransform manaBarRect = mana.GetComponent<RectTransform>();
        TextMeshProUGUI healthText = health.transform.Find("AmountText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI manaText = mana.transform.Find("AmountText").GetComponent<TextMeshProUGUI>();

        Image soulBar = this.soulBar.transform.Find("Background Bar/Bar").GetComponent<Image>();
        Image explorationBar = this.explorationBar.transform.Find("Background Bar/Bar").GetComponent<Image>();

        float healthFill = player.GetStat("Health") / player.GetBaseStat("Health");
        healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, healthFill, 5f * Time.deltaTime);
        float manaFill = player.GetStat("Mana") / player.GetBaseStat("Mana");
        manaCooldown.fillAmount = Mathf.Lerp(manaCooldown.fillAmount, 1 - manaFill, 5f * Time.deltaTime);
        manaBar.fillAmount = Mathf.Lerp(manaBar.fillAmount, manaFill, 5 * Time.deltaTime);

        manaIcon.SetActive(!player.isSpirit);

        if (player.isSpirit)
        {
            manaBarRect.transform.SetAsFirstSibling();
            healthBarRect.sizeDelta = barSmallSize;
            manaBarRect.sizeDelta = barHugeSize;

            healthText.fontSize = amountSmallSize;
            manaText.fontSize = amountHugeSize;
            healthBar.sprite = smallBarHealth;
            manaBar.sprite = bigBarMana;
        }
        else
        {
            healthBarRect.transform.SetAsFirstSibling();
            healthBarRect.sizeDelta = barHugeSize;
            manaBarRect.sizeDelta = barSmallSize;

            healthText.fontSize = amountHugeSize;
            manaText.fontSize = amountSmallSize;
            healthBar.sprite = bigBarHealth;
            manaBar.sprite = smallBarMana;
        }

        healthText.text = ((int)player.GetStat("Health")) + "/" + ((int)player.GetBaseStat("Health"));
        manaText.text = ((int)player.GetStat("Mana")) + "/" + ((int)player.GetBaseStat("Mana"));
        // NOTE: Wait for HUB and level, Create if statement check either in HUB or Level.
        goldText.text = goldTextFormat.Replace("<amount>", ((int)player.GetStat("Gold Coin")).ToString());
        soulText.text = soulTextFormat.Replace("<amount>", ((int)player.GetStat("Soul Coin")).ToString());

        soulBar.fillAmount = player.GetStat("Soul Bar") / player.GetBaseStat("Soul Bar");
        explorationBar.fillAmount = ((float)gameManager.currentActivatedTeleporter) / gameManager.numTeleporter;

        physicalAbilities.SetActive(!player.isSpirit);
        spiritualAbilities.SetActive(player.isSpirit);

        for (int i = 0; i < abilityList.Count; i++)
        {
            if (abilityList[i].transform.Find("Mask/Cooldown"))
            {
                Image cooldown = abilityList[i].transform.Find("Mask/Cooldown").GetComponent<Image>();
                float current = 0f;
                float max = 1f;

                switch (abilityList[i].name)
                {
                    case "Twisted Dive":
                    current = player.physicalFormCombat.GetTwistedDiveCooldown();
                    max = player.physicalFormCombat.twistedDiveCooldown;
                    break;

                    case "Circle Twirl":
                    current = player.physicalFormCombat.GetCircleTwirlCooldown();
                    max = player.physicalFormCombat.circleTwirlCooldown;
                    break;

                    case "Spirit's Calling":
                    current = player.spiritualFormCombat.GetCurrentDashCooldown();
                    max = player.spiritualFormCombat.dashCooldown;
                    break;
                }

                cooldown.fillAmount = current / max;
            }
        }

        // Update ability icon on HUD.
        for (int i = 0; i < abilityList.Count; i++)
        {
            abilityList[i].transform.Find("Mask/Icon").GetComponent<Image>().sprite = upgradeManager.abilityUpgradeManager.abilityList[i].ability.image;
            abilityList[i].transform.Find("Mask/Locked").GetComponent<Image>().enabled = upgradeManager.abilityUnlockLevel - 1 < i;
        }

        bronzeKeyAmount.text = ((int)player.GetStat("Bronze Key Collected")).ToString();
        silverKeyAmount.text = ((int)player.GetStat("Silver Key Collected")).ToString();
        goldKeyAmount.text = ((int)player.GetStat("Gold Key Collected")).ToString();
    }

    // Function to update defeat screen stats.
    public void UpdateDefeatStat()
    {
        goldSentBack.text = goldSentBack.text.Replace("<amount>", player.GetStat("Gold Sent back").ToString());
        goldCollected.text = goldCollected.text.Replace("<amount>", player.GetStat("Gold Collected").ToString());
        mapDiscovered.text = mapDiscovered.text.Replace("<amount>", player.GetStat("Map Discovered").ToString());
        clearTime.text = clearTime.text.Replace("<amount>", TimeFormat(player.GetStat("Clear Time")));
        chestOpened.text = chestOpened.text.Replace("<amount>", player.GetStat("Chest Opened").ToString());
        enemyKilled.text = enemyKilled.text.Replace("<amount>", player.GetStat("Enemy Kill").ToString());

        string damageReceivedString = player.GetStat("Damage Received") >= 1000 ? (int)(player.GetStat("Damage Received") / 1000f) + "K" : ((int)player.GetStat("Damage Received")).ToString();
        damageReceived.text = damageReceived.text.Replace("<amount>", damageReceivedString);

        Inventory inventory = Inventory.instance;

        foreach (Slot slot in inventory.slotList)
        {
            Item item = slot.item;

            GameObject slotObject = Instantiate(inventory.slotPrefab, itemList);
            slotObject.name = item.itemName;
            Image slotImage = slotObject.transform.Find("Icon").GetComponent<Image>();
            slotImage.sprite = item.image;
            slotObject.transform.Find("Panel").GetComponent<Image>().color = item.rarityColor;
        }
    }

    public void Unpause()
    {
        gameManager.SetPause(false);
    }

    // Function to return time into time format.
    private string TimeFormat(float time)
    {
        float second = TimeSpan.FromSeconds(time).Seconds;
        string secondString = second >= 10 ? second.ToString() : 0 + second.ToString();
        float minute = TimeSpan.FromSeconds(time).Minutes;
        string minuteString = minute >= 10 ? minute.ToString() : 0 + minute.ToString();
        float hour = TimeSpan.FromSeconds(time).Hours;
        string hourString = hour >= 10 ? hour.ToString() : 0 + hour.ToString();

        return hourString + ":" + minuteString + ":" + secondString;
    }

    public void PlayButtonSound(bool forUpgrade)
    {
        if (forUpgrade) SFXLibrary.instance.uiUpgradeSFX.PlayFeedbacks();
        else SFXLibrary.instance.uiSelectSFX.PlayFeedbacks();
    }
}