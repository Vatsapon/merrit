using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lever : OutputInteractable
{
    [Header("Lever Settings")]
    [SerializeField] private Door linkedDoor; // Linked door to open.

    private void Update()
    {
        // If the door closed, make lever become interactable again.
        if (linkedDoor != null) {
            if (!linkedDoor.IsActivate())
            {
                if (activated)
                {
                    OnDeactivate.Invoke();
                    Deactivate();
                }

                activated = false;
            }
        }
    }

    protected override void Activate()
    {
        linkedDoor.ActivateDoor();
    }

    public void LinkLeverToDoor(Door door) {
        this.linkedDoor = door;
    }

}
