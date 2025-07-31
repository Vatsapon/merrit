using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossShrine : Entity
{
    private BossFightController bossFightController;
    private Player player;
    private SpriteRenderer sr;

    private int numIntToPopOut;
    private int popOutCounter;

    private int numToPopOutPhase2;
    private int popOutCounterPhase2;

    public Color color;

    protected override void Start()
    {
        base.Start();
        bossFightController = BossFightController.instance;
        player = Player.instance;
        sr = GetComponent<SpriteRenderer>();
        numIntToPopOut = bossFightController.shrinePopOutAfterNumHits;
        numToPopOutPhase2 = bossFightController.shrinePopOutAfterNumHitsPhase2;
        popOutCounterPhase2 = 0;
        popOutCounter = 0;
    }

    public override void TakeDamage(Damage damage, GameObject causeObject)
    {
        if (player.isSpirit) {
            return;
        }

        bossFightController.boss.TakeDamageFromShrine(damage, causeObject);
        popOutCounter += 1;
        popOutCounterPhase2 += 1;

        StartCoroutine(Hurt());
        
        if (bossFightController.bossState != BossFightController.BossPhase.PHASE_2 && popOutCounter >= numIntToPopOut) {
            popOutCounter = 0;
            bossFightController.ShrinePopOut();
        }

        if (bossFightController.bossState == BossFightController.BossPhase.PHASE_2 && popOutCounterPhase2 >= numToPopOutPhase2) {
            popOutCounterPhase2 = 0;
            bossFightController.ShrinePopOut();
        }


    }

    public void ResetPopOutCounter() {
        popOutCounterPhase2 = 0;
        popOutCounter = 0;
    }

    IEnumerator Hurt()
    {
        sr.color = color;
        sr.color = Color.LerpUnclamped(color, Color.white, .1f);
        yield return new WaitForSeconds(.2f);
        sr.color = Color.white;
    }
}
