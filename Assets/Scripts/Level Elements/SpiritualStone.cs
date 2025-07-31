using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritualStone : OutputInteractable
{
    [Header("Spiritual Stone Settings")]
    public GameObject linkedDoor; // Door that linked to this stone.

    protected override void Activate()
    {
        linkedDoor.GetComponent<Collider2D>().enabled = false;
    }
}
