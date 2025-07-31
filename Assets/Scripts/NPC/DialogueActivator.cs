using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueActivator : MonoBehaviour
{
    public Conversation convo;
    public bool playerIsInRange = false;


    public void StartConvo()
    {
        DialogueManager.StartConversation(convo);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerIsInRange = true;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && playerIsInRange && !DialogueManager.isInConversation)
        {
            StartConvo();
            DialogueManager.isInConversation = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerIsInRange = false;
        }
    }
}
