using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    public TextMeshProUGUI speakerName;
    public TextMeshProUGUI dialogue;
    public TextMeshProUGUI navButtonText;
    public Image speakerSprite;
    public GameObject dialogueBox;
    private int currentIndex;
    private Conversation currentConvo;
    private static DialogueManager instance;
    private Coroutine typing;
    private bool activated = false;

    public static bool isInConversation = false;

    //public delegate void TextEndAction()
    public static event Action textEnd;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        isInConversation = false;
    }
    public static void StartConversation(Conversation convo)
    {
        Time.timeScale = 0f;
        instance.dialogueBox.SetActive(true);
        instance.activated = true;
        instance.currentIndex = 0;
        instance.currentConvo = convo;
        instance.speakerName.text = "";
        instance.dialogue.text = "";
        instance.navButtonText.text = ">";

        instance.ReadNext();
    }

    public void ReadNext()
    {
        print($" length {currentConvo.GetLength()}");
        if (currentIndex > currentConvo.GetLength())
        {
            //Finish Convo
            Time.timeScale = 1f;
            print("finish convo");
            isInConversation = false;
            textEnd?.Invoke();
            dialogueBox.SetActive(false);
            activated = false;
            return;
        }

        speakerName.text = currentConvo.GetLineByIndex(currentIndex).speaker.GetName();

        if (typing == null)
        {
            typing = instance.StartCoroutine(TypeText(currentConvo.GetLineByIndex(currentIndex).dialogue));
        }
        else
        {
            instance.StopCoroutine(typing);
            typing = null;
            typing = instance.StartCoroutine(TypeText(currentConvo.GetLineByIndex(currentIndex).dialogue));
        }

        speakerSprite.sprite = currentConvo.GetLineByIndex(currentIndex).speaker.GetSprite();
        currentIndex++;
    }

    private void Update()
    {
        if (activated && Input.anyKeyDown && !Input.GetKeyDown(KeyCode.E))
        {
            ReadNext();
        }
    }

    private IEnumerator TypeText(string text)
    {
        dialogue.text = "";
        bool complete = false;
        int index = 0;

        while (!complete)
        {
            dialogue.text += text[index];
            index++;
            yield return new WaitForSecondsRealtime(0.02f);
            if (index == text.Length)
            {
                complete = true;
            }
        }

        typing = null;
    }
}
