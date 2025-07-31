using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbienceSwitcher : MonoBehaviour
{
    public AudioSource normal;
    public AudioSource spirit;

    void Update()
    {
        if (Player.instance.isSpirit)
        {
            spirit.volume = 1;
            normal.volume = 0;
        }
        else 
        {
            spirit.volume = 0;
            normal.volume = 1;
        }
    }
}
