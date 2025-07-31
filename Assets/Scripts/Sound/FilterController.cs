using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilterController : MonoBehaviour
{
    public AudioLowPassFilter spiritModeSoundFilter;

    private void Update()
    {
        if (Player.instance != null && spiritModeSoundFilter != null)
        {
            spiritModeSoundFilter.enabled = Player.instance.isSpirit;
        }
    }
}
