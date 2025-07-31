using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using static Ability;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Datas")]
    public int gold; // Gold (Currency)
    public int explorationCoin; // Exploration Coin (Currency)
    public float explorationBarAmount; // Exploration Bar in percentage.
    public float exploreationBarIncrementAmount;
    public int numTeleporter;
    public int currentActivatedTeleporter;
    public int soulCoin; // Soul Coin (Currency)
    public List<GameObject> unlockedTeleport;

    [Header("References")]
    public Camera fogOfWarCamera; // Fog of War Camera.

    [HideInInspector] public Dictionary<GameObject, Vector2> damageIndicatorList = new Dictionary<GameObject, Vector2>(); // List that contains all damage indicators.

    private UpgradeManager upgradeManager;
    private UIManager uiManager;

    private bool isPaused = false; // Determine if game is Pausing.

    private void Awake()
    {
        instance = this;

        GameObject[] objs = GameObject.FindGameObjectsWithTag("GameController");

        if (objs.Length > 1)
        {
            Destroy(gameObject);
        } else {
            DontDestroyOnLoad(gameObject);
        }

        numTeleporter = 0;
        currentActivatedTeleporter = 0;

        unlockedTeleport = new List<GameObject>();
    }

    private void Start()
    {
        upgradeManager = UpgradeManager.instance;
        uiManager = UIManager.instance;

        isPaused = false;
        PauseStatusUpdater();
    }

    private void Update()
    {
        DamageIndicatorUpdater();

        // [ESC] - Pause / Unpause game.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetPause(!isPaused);
        }

        // [I] - Toggle Inventory window.
        if (Input.GetKeyDown(KeyCode.I))
        {
            GameObject inventoryWindow = uiManager.inventoryWindow;

            if (!inventoryWindow.activeSelf && !IsUI())
            {
                inventoryWindow.SetActive(true);
            }
            else
            {
                inventoryWindow.SetActive(!inventoryWindow.activeSelf);
            }
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            SceneManager.LoadScene("TutorialScene");
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            SceneManager.LoadScene("HubScene");
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            SceneManager.LoadScene("Test Tiling Scene");
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            SceneManager.LoadScene("boss test 2");
        }

        // [M] - Toggle on/off map.
        if (Input.GetKeyDown(KeyCode.M))
        {
            uiManager.mapScreen.SetActive(!uiManager.mapScreen.activeSelf);
        }
        
        // [O] - Unlock all skills.
        if (Input.GetKeyDown(KeyCode.O))
        {
            upgradeManager.abilityUnlockLevel = upgradeManager.abilityUpgradeManager.abilityList.Count;

            foreach (Ability ability in Resources.LoadAll("Abilities"))
            {
                ability.unlocked = true;

                foreach (Ability_Level abilityLevel in ability.abilityLevel)
                {
                    abilityLevel.purchased = true;
                }
            }
        }

        // [P] - Unlock all abilities with maximum level.
        if (Input.GetKeyDown(KeyCode.P))
        {
            upgradeManager.abilityUnlockLevel = upgradeManager.abilityUpgradeManager.abilityList.Count;

            foreach (Ability ability in Resources.LoadAll("Abilities"))
            {
                ability.unlocked = true;

                foreach (Ability_Level abilityLevel in ability.abilityLevel)
                {
                    abilityLevel.purchased = true;
                }
            }

            foreach (Item item in Resources.LoadAll("Items/Common")) {
                Inventory.instance.AddItem(item);
            }

            foreach (Item item in Resources.LoadAll("Items/Epic")) {
                Inventory.instance.AddItem(item);
            }

            foreach (Item item in Resources.LoadAll("Items/Rare")) {
                Inventory.instance.AddItem(item);
            }

            gold = 100000000;
            explorationCoin = 10000000;

            foreach (Upgrade upgrade in Resources.LoadAll("Stats")) {
                for (int i = 0 ; i < upgrade.upgradeLevel.Count; i++) {
                    upgrade.GetCurrentUpgrade().unlocked = true;
                    StatUpgradeManager.instance.Purchase(new StatUpgradeShop(null, upgrade));
                }
            }

            gold = 0;

        }


        // [0] - Reload scene.
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void goToHubScene()
    {
        SceneManager.LoadSceneAsync("HubScene");
    }

    public void goToGamePlay()
    {
        SceneManager.LoadSceneAsync("Test Tiling Scene");
    }

    public int GetTeleporterInd(GameObject teleporter) {
        for (int i = 0; i < unlockedTeleport.Count; i++) {
            if (unlockedTeleport[i] == teleporter) {
                return i;
            }
        }
        return -1;
    }

    public void IncreaseExplorationBarAmount()
    {
        currentActivatedTeleporter += 1;
    }

    // Function to update Pause status.
    private void PauseStatusUpdater()
    {
        if (isPaused || DialogueManager.isInConversation)
        {
            Cursor.SetCursor(Util.instance.prefabs.normalCursor, Vector2.zero, CursorMode.Auto);
            Time.timeScale = 0;
        }
        else
        {
            if (Player.instance.isSpirit)
            {
                Cursor.SetCursor(Util.instance.prefabs.aimCursor, Util.instance.prefabs.aimCursor.texelSize / 2f, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(Util.instance.prefabs.normalCursor, Vector2.zero, CursorMode.Auto);
            }
            
            Time.timeScale = 1;
        }

        uiManager.pauseScreen.SetActive(isPaused);
    }

    // Function to set pause value.
    public void SetPause(bool value)
    {
        isPaused = value;
        PauseStatusUpdater();
    }

    // Function to update all damage indicators.
    private void DamageIndicatorUpdater()
    {
        foreach (KeyValuePair<GameObject, Vector2> damageIndicator in damageIndicatorList)
        {
            if (damageIndicator.Key)
            {
                damageIndicator.Key.transform.position = Camera.main.WorldToScreenPoint(damageIndicator.Value);
            }
        }
    }

    // Function to do Freeze Frame.
    public async void FreezeFrame(float duration)
    {
        Time.timeScale = 0f;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            await Task.Yield();
        }

        Time.timeScale = 1f;
    }

    // Function to do Freeze Frame with delay (For Enemy).
    public async void FreezeFrame(float duration, float delay, float afterDuration, AnimationCurve curve)
    {
        float timer = 0f;

        while (timer < delay)
        {
            timer += Time.unscaledDeltaTime;
            await Task.Yield();
        }

        Time.timeScale = 0f;

        timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            await Task.Yield();
        }

        timer = 0f;

        while (timer < afterDuration)
        {
            Time.timeScale = 1f - Mathf.Clamp(Time.timeScale, 0 , curve.Evaluate(timer / afterDuration));

            timer += Time.unscaledDeltaTime;
            await Task.Yield();
        }

        Time.timeScale = 1f;
    }

    // Function to check either is opening any UI.
    public bool IsUI()
    {
        for (int i = 0; (uiManager.UIParent != null) && (i < uiManager.UIParent.childCount); i++)
        {
            if (uiManager.UIParent.GetChild(i).gameObject.activeSelf)
            {
                return true;
            }
        }

        return false;
    }

    // Function to find Upgrade by name.
    public Upgrade GetUpgrade(string name)
    {
        foreach (Upgrade upgrade in Resources.LoadAll("Stats"))
        {
            if (upgrade.statName.Equals(name))
            {
                return upgrade;
            }
        }

        return null;
    }

    // Function to find Item by name.
    public Item GetItem(string name)
    {
        foreach (Item item in Resources.LoadAll("Items"))
        {
            if (item.itemName.Equals(name))
            {
                return item;
            }
        }

        return null;
    }

    // Function to find Ability by enum.
    public Ability GetAbility(AbilityType abilityType)
    {
        foreach (Ability ability in Resources.LoadAll("Abilities"))
        {
            if (ability.abilityType == abilityType)
            {
                return ability;
            }
        }

        return null;
    }

    // Function to unlock everything. (FOR DEBUGGING)
    public void UnlockEverything()
    {
        upgradeManager.abilityUnlockLevel = upgradeManager.abilityUpgradeManager.abilityList.Count;

        foreach (Ability ability in Resources.LoadAll("Abilities"))
        {
            ability.unlocked = true;

            foreach (Ability_Level abilityLevel in ability.abilityLevel)
            {
                abilityLevel.purchased = true;
            }
        }
    }

    public void updateNumTeleporter() {
        numTeleporter = GameObject.FindGameObjectsWithTag("Teleport").Length;
    }
}
