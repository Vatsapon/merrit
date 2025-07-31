using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dungeon : MonoBehaviour
{
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player.instance.SetStat("Gold Coin", 0f);
            Player.instance.SetStat("Gold Coin", 0f);
        }
    }
}
