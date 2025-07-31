using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Prop : Lootable
{
    [Header("Prop Settings")]
    [SerializeField] private int propHit = 1; // Amount of hit before it destroy and drop loots.

    [Header("References")]
    [SerializeField] private GameObject breakParticlePrefab; // Break particle prefab.

    public UnityEvent OnBreak;

    private int hitAmount; // Amount of prop that being hit.
    public bool isStatue = false;
    public bool isBox = false;

    // Function when prop took a hit.
    public void TakeHit()
    {
        hitAmount++;

        // If it reaches limit, break and drop loot.
        if (hitAmount >= propHit)
        {
            Instantiate(breakParticlePrefab, transform.position, Quaternion.identity);

            if (isStatue)
                SFXLibrary.instance.StatueBreakSFX.PlayFeedbacks();

            if (isBox)
                SFXLibrary.instance.BoxBreakSFX.PlayFeedbacks();

            DropLoot();
            OnBreak.Invoke();
            Destroy(gameObject);

            
        }
        if (isStatue)
            SFXLibrary.instance.StatueHitSFX.PlayFeedbacks();
    }

    // Function to add mana to player.
    public void AddMana(float amount)
    {
        Player.instance.IncreaseStat("Mana", amount);
        Instantiate(Util.instance.particles.manaIncreaseEffect, Player.instance.transform.position, Quaternion.identity);
    }
}
