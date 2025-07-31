using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

public class Util : MonoBehaviour
{
    public static Util instance;

    [Header("Particles")]
    public UtilParticle particles; // Class that contains all particles.

    [Header("Prefabs")]
    public UtilPrefab prefabs; // Class that contains all prefabs.

    private void Awake()
    {
        instance = this;
    }
}

[System.Serializable]
public class UtilParticle
{
    [Header("Physical Attack")]
    public GameObject physicalAttackHit1; // Physical Attack hit 1.
    public GameObject physicalAttackHit2; // Physical Attack hit 2.
    public GameObject physicalAttackHit3; // Physical Attack hit 3.
    public GameObject physicalAttackHitUp; // Physical Attack hit up.
    public GameObject physicalDashAttack; // Physical Dash Attack.

    [Header("Spiritual Attack")]
    public GameObject spiritualAttack; // Spiritual attack.
    public GameObject spiritualBulletHitEnemy; // Spiritual bullet on hit enemy.
    public GameObject spiritualBullet1; // Spiritual bullet particle 1.
    public GameObject spiritualBullet1Hit; // Spiritual bullet particle 1 on hit.
    public GameObject spiritualBullet2; // Spiritual bullet particle 2.
    public GameObject spiritualBullet2Hit; // Spiritual bullet particle 2 on hit.
    public GameObject spiritualBullet3; // Spiritual bullet particle 3.
    public GameObject spiritualBullet3Hit; // Spiritual bullet particle 3 on hit.
    public GameObject spiritualBullet3Back; // Spiritual bullet particle 3 (back).
    public GameObject spiritualBullet3BackHit; // Spiritual bullet particle 3 (back) on hit.

    [Header("Blood Splash Enemy")]
    public GameObject bloodSplashEnemy1; // Blood Splash from Attack 1 toward enemy.
    public GameObject bloodSplashEnemy2; // Blood Splash from Attack 2 toward enemy.
    public GameObject bloodSplashEnemy3; // Blood Splash from Attack 3 toward enemy.
    public GameObject bloodSplashEnemyUp; // Blood Splash from Attack up toward enemy.
    public GameObject bloodSplashEnemyDeath; // Blood Splash from the center on enemy's death.
    public GameObject bloodSplashEnemyFluid; // Blood Splash in fluid on enemy's death.

    [Header("Player Ability")]
    public GameObject spiritualResonanceCharging; // Spiritual Resonance charging.
    public GameObject spiritualResonanceInnerExplosion; // Spiritual Resonance explosion. (Inner)
    public GameObject spiritualResonanceOuterExplosion; // Spiritual Resonance explosion. (Outer)
    public GameObject spiritCallingShield; // Spirit Calling's shield.
    public GameObject twistedDiveCharging; // Twisted Dive Charging.
    public GameObject twistedDiveInnerImpact; // Twisted Dive Impact. (Inner)
    public GameObject twistedDiveOuterImpact; // Twisted Dive Impact. (Outer)
    public GameObject circleTwirlGroundArea; // Cirl Twirl ground area.

    [Header("Others")]
    public GameObject playerDashTrail; // Player's dash trail.
    public GameObject playerTransformation; // Player's transformation.
    public GameObject environmentHit; // Particle when something hit with environment.
    public GameObject fragilePlatformBroke; // Particle when fragile platform destroyed.
    public GameObject stunEffect; // Particle when entity got stunned.
    public GameObject burstEffect; // Burst star effect for openning chest.
    public GameObject manaIncreaseEffect; // Effect when mana is increase.
    public GameObject projectileExplosion; // Explosion effect.
    public GameObject protectionShield; // Shield effect.
    public GameObject stompEffect; // Enemy 4 Stomp effect.
    public GameObject bossPortalEffect; // Portal effect when Boss spawn enemy.
}

[System.Serializable]
public class UtilPrefab
{
    public GameObject damageIndicator; // Damage indicator. (UI)
    public GameObject soulItem; // Soul item.
    public GameObject goldCoinItem; // Gold coin item.

    [Space(5)]
    public Texture2D normalCursor;
    public Texture2D aimCursor;
}
