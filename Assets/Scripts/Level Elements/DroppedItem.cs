using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DroppedItem : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private bool interactToCollect = false; // Determine to interact to collect item or not.
    [SerializeField] private float delayBeforeCollect = 1f; // Delay before able to collect.
    [SerializeField] private float destroyDelay = 10f; // Delay before destroy item.
    [Space(5)]
    [SerializeField] private float collectRange = 5f; // Collect range.

    [Header("Animations")]
    [SerializeField] private float animationSpeed = 5f;
    [SerializeField] private float animationHeight = 2f;

    public UnityEvent OnCollected;

    protected bool collectable = true; // Make a condition where player can collect item.

    protected float currentDelay;
    protected Player player;

    protected virtual void Start()
    {
        player = Player.instance;
        currentDelay = delayBeforeCollect;

        if (destroyDelay > 0f)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    protected virtual void Update()
    {
        if (transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).localPosition = Vector2.up * Mathf.Sin(Time.time * animationSpeed) * animationHeight;
            }
        }

        if (currentDelay > 0f)
        {
            currentDelay -= Time.deltaTime;
        }

        if (Vector2.Distance(transform.position, player.transform.position) < collectRange)
        {
            if (currentDelay <= 0f && collectable)
            {
                // Collect if player is in Physical form.
                if (!player.isSpirit)
                {
                    if (interactToCollect && Input.GetKeyDown(KeyCode.F))
                    {
                        OnCollected.Invoke();
                        OnCollect();
                        Destroy(gameObject);
                        return;
                    }

                    OnCollected.Invoke();
                    OnCollect();
                    Destroy(gameObject);
                }
            }
        }
    }

    // Function when item has been collected.
    protected virtual void OnCollect() {
        SFXLibrary.instance.CollectingItemSFX.PlayFeedbacks();
    }
}
