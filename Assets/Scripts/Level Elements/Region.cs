using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A class that contain the overall data in that region.
public class Region : MonoBehaviour
{
    [Header("Region Setting")]
    public float manaRegeneration = 5f; // Amount of Spirit power that will be regenerate per second.
    public float manaConsumption = 2f; // Amount of Spirit power that will be consume per second.

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Check either player is inside the region.
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            player.SetStatUnclamp("Mana Regeneration", manaRegeneration);
            player.SetStatUnclamp("Mana Consumption", manaConsumption);
        }
    }
}
