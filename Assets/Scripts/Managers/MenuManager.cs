using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoreMountains.Feedbacks;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    // Load Scene from name.

    [Header("Credits Config")]
    public bool onCreditsPage;
    public Transform creditTexts;
    public float finalYPos;
    public float rollSpeed = 5f;
    [Space(5)]
    public Scrollbar creditScrollbar;
    public float autoScrollSpeed = 5f;

    [Header("Feedbacks/Transitions")]
    public MMFeedbacks toCredits;
    public MMFeedbacks toMainMenu;
    public MMFeedbacks toOptions;

    public bool rollInGameCredit = false;

    private void Update()
    {
        if (onCreditsPage) RollCredits();

        if (rollInGameCredit)
        {
            creditScrollbar.value -= autoScrollSpeed * Time.deltaTime;
            creditScrollbar.value = Mathf.Clamp(creditScrollbar.value, 0f, 1f);

            if (creditScrollbar.value <= 0f)
            {
                creditScrollbar.value = 1f;
            }
        }
    }

    public void LoadScene(string name)
    {
        SceneManager.LoadScene(name);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ToCredits()
    {
        //ResetCreditPosition();

        onCreditsPage = true;
        toCredits.PlayFeedbacks();
    }

    public void BackToMainMenu()
    {
        onCreditsPage = false;
        toMainMenu.PlayFeedbacks();
    }

    public void Options()
    {
        toOptions.PlayFeedbacks();
    }

    public void RollInGameCredit()
    {
        rollInGameCredit = true;
    }

    #region Credits
    void RollCredits()
    {
        if (creditTexts.position.y < finalYPos+1080)
            ResetCreditPosition();

        if (toCredits.IsPlaying) return;

        creditTexts.Translate(rollSpeed * Vector2.up * Time.deltaTime);
    }

    void ResetCreditPosition()
    {
        creditTexts.localPosition = new Vector3(0, 0);
    }

    #endregion
}
