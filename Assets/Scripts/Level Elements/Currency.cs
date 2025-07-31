using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Currency : DroppedItem
{
    public enum CurrencyType
    {
        GOLD, SOUL
    }

    [SerializeField] private CurrencyType currencyType = CurrencyType.GOLD;
    [Space(5)]
    [SerializeField] private int amount; // Amount of currency.

    protected override void Update()
    {
        base.Update();

        if (currentDelay <= 0f)
        {
            Vector2 direction = player.transform.position - transform.position;
            float distance = Vector2.Distance(player.transform.position, transform.position);
            float speed = distance < 3f ? 5f : 3f;
            transform.position += (Vector3)direction.normalized * distance * speed * Time.deltaTime;
        }
    }

    protected override void OnCollect()
    {
        Player player = Player.instance;
        SFXLibrary.instance.CollectingCoinSFX.PlayFeedbacks();
        if (currencyType == CurrencyType.GOLD)
        {
            player.IncreaseStatUnclamp("Gold Collected", 1);
            player.IncreaseStatUnclamp("Gold Coin", (int)amount);
        }
        else
        {
            // If it reaches maximum, add soul coin.
            if (player.GetStat("Soul Bar") + amount >= player.GetBaseStat("Soul Bar"))
            {
                int soulRemain = (int)player.GetStat("Soul Bar") - amount;
                player.SetStat("Soul Bar", soulRemain);
                player.IncreaseStatUnclamp("Soul Coin", 1);
            }
            else
            {
                player.IncreaseStat("Soul Bar", amount);
            }

            UIManager.instance.soulBar.GetComponent<Animator>().Play("Play");
        }
    }

    // Set amount of currency.
    public void SetAmount(int value)
    {
        amount = value;
    }
}
