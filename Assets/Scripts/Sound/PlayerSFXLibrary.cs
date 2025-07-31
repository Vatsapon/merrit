using UnityEngine;
using MoreMountains.Tools;

public class PlayerSFXLibrary : MonoBehaviour
{
    public static PlayerSFXLibrary instance;


    [Header("General")]
    public AudioClip formChange;
    public AudioClip dashing;

    [Header("Physical")]
    public AudioClip attackUp;
    public AudioClip first;
    public AudioClip second;
    public AudioClip physicalBasicAttack;
    public AudioClip dashAttack;
    [Space(2)]
    public AudioClip landing;
    public AudioClip jump;
    [Space(2)]
    public AudioClip gpLanding;
    public AudioClip gpCharging;
    public AudioClip falling;
    [Space(2)]
    public AudioClip[] walking;
    public float delayAfterLanding = 0.2f;
    public bool justLanded;

    [Header("Spirit")]
    public AudioClip spiritBasicAttack;
    public AudioClip spiritBasicAttackAfter;
    public AudioClip transformFail;
    public AudioClip OutOfMana;

    private void Awake()
    {
        instance = this;
    }

    public void PlaySFX(AudioClip sound)
    {
        if (sound == physicalBasicAttack)
        {
            MMSoundManagerPlayOptions options;
            options = MMSoundManagerPlayOptions.Default;
            options.Pitch = Random.Range(0.94f, 1.4f);
            options.Volume = Random.Range(0.9f, 1.1f);
        }

        MMSoundManagerSoundPlayEvent.Trigger(sound, MMSoundManager.MMSoundManagerTracks.Sfx, transform.position);
    }

    public void PlayWalkingSFX()
    {
        if (justLanded)
        {
            justLanded = false;
            return;
        }

        int rand = Random.Range(0, walking.Length);
        MMSoundManagerSoundPlayEvent.Trigger(walking[rand], MMSoundManager.MMSoundManagerTracks.Sfx, transform.position);
    }
}
