using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showInteractRange = false; // Determine to show interaction range or not.
    public float interactRange = 1f; // Range to let player interact.

    [Header("Events")]
    public UnityEvent onEnterRange; // Event that excute when player walk close up to interact range.
    public UnityEvent onExitRange; // Event that excute when player walk away from interact range.
    public UnityEvent onInteract; // Event that execute when player interact with NPC.

    private Player player;

    protected virtual void Start()
    {
        player = Player.instance;
    }

    private void Update()
    {
        float distance = Vector2.Distance(transform.position, player.transform.position);

        if (Input.GetKeyDown(KeyCode.F) && distance <= interactRange)
        {
            onInteract.Invoke();
        }
    }

    private void OnDrawGizmos()
    {
        if (showInteractRange)
        {
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
