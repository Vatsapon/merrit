using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

public class PlayerSFXHandler : MonoBehaviour
{
    public MMFeedbacks Walk;
    public MMFeedbacks Dash;
    public MMFeedbacks Jump;
    [Space(5)]
    public MMFeedbacks Combo1;
    public MMFeedbacks Combo21;
    public MMFeedbacks Combo22;
    public MMFeedbacks Combo3;
    public MMFeedbacks DashAttack;
    public MMFeedbacks Up;
    [Space(5)]
    public MMFeedbacks CircleTwirl1;
    public MMFeedbacks CircleTwirl2;



    [Header("Spirit Related")]
    public MMFeedbacks ResonanceExplosion;

    public void PlayDashSFX()
    {
        Dash.PlayFeedbacks();
    }

    public void PlayJumpSFX()
    {
        Jump.PlayFeedbacks();
    }

    //COMBO
    public void PlayCombo1SFX()
    {
        Combo1.PlayFeedbacks();
    }

    public void PlayCombo21SFX()
    {
        Combo21.PlayFeedbacks();
    }

    public void PlayCombo22SFX()
    {
        Combo22.PlayFeedbacks();
    }

    public void PlayCombo3SFX()
    {
        Combo3.PlayFeedbacks();
    }

    public void PlayDashAttackSFX()
    {
        DashAttack.PlayFeedbacks();
    }

    public void PlayUpSFX()
    {
        Up.PlayFeedbacks();
    }

    public void PlayExplosionSFX()
    {
        ResonanceExplosion.PlayFeedbacks();
    }

    public void PlayCircleTwirl1()
    {
        CircleTwirl1.PlayFeedbacks();
    }
    public void PlayCircleTwirl2()
    {
        CircleTwirl2.PlayFeedbacks();

    }
}
