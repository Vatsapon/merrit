using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

public class BossSoundManager : MonoBehaviour
{
    [Header("Phase Indicator")]
    public MMFeedbacks start;
    public MMFeedbacks transitionToPHASE1;
    public MMFeedbacks transitionToPHASE2;
    public MMFeedbacks transitionToMINIPHASE;

    [Header("Boss")]
    public MMFeedbacks bossMoving;
    public MMFeedbacks bossPreAttack;
    public MMFeedbacks bossAttack;

    [Header("Flying Cube")]
    public MMFeedbacks cubeMoving;
    public MMFeedbacks cubePreAttack;
    public MMFeedbacks cubeAttack;
    [Space(5)]
    public MMFeedbacks cubeEnlarge;
    public MMFeedbacks cubeDeflate;
    [Space(5)]
    public MMFeedbacks cubeShake;
    public MMFeedbacks cubeImpact;

    [Header("Shrine")]
    public MMFeedbacks activate;
    public MMFeedbacks deactivate;
}
