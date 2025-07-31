using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using MoreMountains.Feedbacks;


public class Door : OutputInteractable
{
    [Header("Door Settings")]
    [SerializeField] private bool hasTimer = false; // Determine if it will deactivate ifself after X second or not.
    [SerializeField] private bool showLiftHeight = false; // Determine to show Lift height or not.
    [Space(5)]
    [SerializeField] private float liftSpeed = 5f; // Lift speed.
    [SerializeField] private float closeSpeed = 7.5f; // Close down speed.
    [SerializeField] private float openDuration = 10f; // Duration that door will open and will close when reaches 0.
    [SerializeField] private float liftHeight = 5f; // Height that the door will lift up.

    private float currentOpenDuration; // Open duration timer.
    private Vector2 savedPosition = Vector2.zero; // Saved default position of the door.

    private Rigidbody2D rigidBody;

    [Header("Feedbacks")]
    public MMFeedbacks movingSFX, impactSFX;
    bool canPlaySFX = false;
    private void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();

        savedPosition = transform.position;
        currentOpenDuration = openDuration;
    }

    private void FixedUpdate()
    {
        // If Activate = Open, else, close.
        if (IsActivate())
        {
            float limitY = savedPosition.y + liftHeight;
            float distance = Mathf.Abs(limitY - transform.position.y);

            // If it's not reaches the top yet, keep lifting.
            if (distance > 0.15f)
            {
                rigidBody.velocity = Vector2.up * liftSpeed;

                if (!movingSFX.IsPlaying)
                    movingSFX.PlayFeedbacks();
            }
            else
            {
                PlayImpactSFX();
                transform.position = new Vector2(savedPosition.x, limitY);
                rigidBody.velocity = Vector2.zero;
                
                if (hasTimer)
                {
                    // If the duration is still not reaches 0, keep opening.
                    if (currentOpenDuration > 0f)
                    {
                        currentOpenDuration -= Time.fixedDeltaTime;
                    }
                    else
                    {
                        // Deactivate and close the door.
                        activated = false;
                        currentOpenDuration = openDuration;
                    }
                }
            }
        }
        else
        {
            float distance = Mathf.Abs(transform.position.y - savedPosition.y);

            // If it's not reaches the same position yet, keep closing.
            if (distance > 0.15f)
            {
                rigidBody.velocity = Vector2.down * closeSpeed;

                if (!movingSFX.IsPlaying)
                    movingSFX.PlayFeedbacks();
            }
            else
            {
                PlayImpactSFX();
                rigidBody.velocity = Vector2.zero;
                currentOpenDuration = openDuration;
            }
        }
    }

    // Function to activate door, separate from SetActivate().
    public void ActivateDoor()
    {
        activated = true;
    }

    public override void SetActivate(bool value) { canPlaySFX = true; }

    private void PlayImpactSFX()
    {
        if (!movingSFX.IsPlaying)
        {
            movingSFX.StopFeedbacks();
        }
        if (canPlaySFX && !impactSFX.IsPlaying)
        {
            impactSFX.PlayFeedbacks();
            canPlaySFX = false;
        }

        
    }

    private void OnDrawGizmos()
    {
        if (showLiftHeight)
        {
            if (!Application.isPlaying)
            {
                savedPosition = transform.position;
            }

            Vector2 offset = (Vector2) savedPosition + (Vector2.up * liftHeight);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(offset, GetComponent<Collider2D>().bounds.size);
        }
    }
}

public class OutputInteractable : MonoBehaviour
{
    public UnityEvent OnActivate; // Event that execute when it activate.
    public UnityEvent OnDeactivate; // Event that execute when it deactivate.

    protected bool activated = false; // Determine if it's activated or not.

    // Function to determine if this interactable is activate or not.
    public bool IsActivate()
    {
        return activated;
    }

    // Function to set it activate / deactivate.
    public virtual void SetActivate(bool value)
    {
        if (!activated && value)
        {
            OnActivate.Invoke();
            Activate();
        }

        if (activated && !value)
        {
            OnDeactivate.Invoke();
            Deactivate();
        }

        activated = value;
    }

    protected virtual void Activate() { }
    protected virtual void Deactivate() { }
}
