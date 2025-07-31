using UnityEngine;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;

public class SFXLibrary : MonoBehaviour
{
    public static SFXLibrary instance;

    [Header("UI")]
    public MMFeedbacks uiSelectSFX;
    public MMFeedbacks uiUpgradeSFX;

    [Header("Universal")]
    public MMFeedbacks ImpactSFX;
    [Space(7)]
    public MMFeedbacks CollectingKeySFX;
    public MMFeedbacks CollectingItemSFX;
    public MMFeedbacks CollectingCoinSFX;
    public MMFeedbacks CollectingChestSFX;
    public MMFeedbacks CollectingSpiritualGemstoneSFX;
    [Space(7)]
    public MMFeedbacks StatueHitSFX;
    public MMFeedbacks StatueBreakSFX;
    public MMFeedbacks BoxBreakSFX;
    public MMFeedbacks TeleportSFX;
    public MMFeedbacks TeleportUnlockSFX;
    public MMFeedbacks ActivateTeleportSFX;

    [Header("Boss")]
    public MMFeedbacks ShrineDisappearSFX;


    private void Awake()
    {
        instance = this;
    }

    public void PlaySFX(MMFeedbacks sound)
    {
        sound.PlayFeedbacks();
    }

}
