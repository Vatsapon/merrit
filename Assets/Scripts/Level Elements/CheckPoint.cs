using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CheckPoint : MonoBehaviour
{
    public static int sendCount = 0;
    
    public int maxSendCount = 3;

    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }
    
    private void Update()
    {
        ContactFilter2D filter = new ContactFilter2D().NoFilter();
        List<Collider2D> collided = new List<Collider2D>();
        col.OverlapCollider(filter, collided); // get all collided objects
        foreach (Collider2D c in collided)
        {
            if (c.CompareTag("Player") && Input.GetKeyDown(KeyCode.F) && sendCount < maxSendCount)
            { // check for player, limits sending to 3 times
                print($"Send Coin: {sendCount}");
                
                float tax = GetTax();

                UIManager uiManager = UIManager.instance;
                uiManager.sendCoinPrompt.SetActive(true);
                uiManager.sendCoinPrompt.transform.Find("Panel/Title").GetComponent<TextMeshProUGUI>().text = uiManager.sendCoinTextFormat.Replace("<gold>", sendCount.ToString()).Replace("<tax>", tax.ToString("F2"));
                uiManager.sendCoinPrompt.transform.Find("Panel/Buttons/Yes Button").GetComponent<Button>().onClick.RemoveAllListeners();
                uiManager.sendCoinPrompt.transform.Find("Panel/Buttons/Yes Button").GetComponent<Button>().onClick.AddListener(delegate
                {
                    sendCoins(tax);
                });
            }
        }
    }

    public float GetTax() {
        float tax;
        switch (sendCount) { // check for the number of time that player send money back
            case 0:
                tax = Random.Range(5, 16);
                break;
            case 1:
                tax = Random.Range(10,26);
                break;
            case 2:
                tax = Random.Range(20,31);
                break;
            default:
                tax = 0;
                break;
        }
        return tax;
    }

    public void sendCoins(float tax) {

        sendCount += 1;
        int goldCoin = (int) Player.instance.GetStat("Gold Coin"); // use getters method
        int soulCoin = (int)Player.instance.GetStat("Soul Coin");
        GameManager.instance.gold += (int)((float)goldCoin - goldCoin * (tax/100.0)); // taxation
        GameManager.instance.soulCoin += (int)((float)goldCoin - soulCoin * (tax/100.0));
        Player.instance.SetStat("Gold Coin", 0f); // use setter method
        Player.instance.SetStat("Gold Coin", 0f);
    }

}
