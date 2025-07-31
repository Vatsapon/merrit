using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

public class AnimationTrigger : MonoBehaviour
{
    public MMFeedbacks walkingSFX;
    public MMFeedbacks slidingStartSFX;
    public MMFeedbacks slidingLoopSFX;
    public MMFeedbacks slashSFX;
    public MMFeedbacks windUpSFX;
    public MMFeedbacks castSFX;
    public MMFeedbacks teleportSFX;
    public MMFeedbacks wingFlappingSFX;
    public MMFeedbacks dyingSFX;

    // Function to do Freeze Frame.
    public void FreezeFrame(float duration)
    {
        GameManager.instance.FreezeFrame(duration);
    }

    // Function to destroy object.
    public void Destroy()
    {
        Destroy(gameObject);
    }

    public void ATPlayWalkingSFX()
    {
        //if (walkingSFX != null) {
            walkingSFX.PlayFeedbacks();
        //}
    }

    public void PlaySlidingStart()
    {
        slidingStartSFX.PlayFeedbacks();
    }

    public void PlaySlidingLoop()
    {
        slidingLoopSFX.PlayFeedbacks();
    }
    public void PlaySlashSFX()
    {
        slashSFX.PlayFeedbacks();
    }

    public void PlayWindUpSFX()
    {
        windUpSFX.PlayFeedbacks();
    }

    public void PlayCastSFX()
    {
        castSFX.PlayFeedbacks();
    }

    public void PlayTeleportSFX()
    {
        teleportSFX.PlayFeedbacks();
    }

    public void PlayWingFlapingSFX()
    {
        wingFlappingSFX.PlayFeedbacks();
    }

    public void PlayDyingSFX()
    {
        dyingSFX.PlayFeedbacks();
    }
}
