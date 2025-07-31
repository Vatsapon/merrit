using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour
{
    public Collider2D _collider;
    public PlatformEffector2D _effector;

    [Header("Settings")]
    [SerializeField] private float waitTime = 0.25f;

    [SerializeField] private List<Collider2D> colliders = new List<Collider2D>();

    private float currentWaitTime; // Wait timer.
    private bool isFlipped; // Check if flipping.

    private void Start()
    {
        if (colliders.Count == 0)
        {
            colliders.Add(GetComponent<Collider2D>());
        }

        isFlipped = false;
    }

    private void Update()
    {
        // Check if timer runs out, reverse back to normal.
        if (currentWaitTime == 0 && isFlipped)
        {

            foreach (Collider2D collider in colliders)
            {
                Physics2D.IgnoreCollision(collider, Player.instance.GetComponent<Collider2D>(), false);
            }

            isFlipped = false;
        }

        // Check if player jump down the platform.
        if (Input.GetKey(KeyCode.S))
        {
            if (currentWaitTime <= 0)
            {

                foreach (Collider2D collider in colliders)
                {
                    Physics2D.IgnoreCollision(collider, Player.instance.GetComponent<Collider2D>(), true);
                }

                currentWaitTime = waitTime;
                isFlipped = true;
            }
        }

        currentWaitTime -= Time.deltaTime;
        currentWaitTime = Mathf.Clamp(currentWaitTime, 0, waitTime);
    }

    public void Switcher(bool state)
    {
        _collider.usedByEffector = state;
        _effector.enabled = state;
    }
}
