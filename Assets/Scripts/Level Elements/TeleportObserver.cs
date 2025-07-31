using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeleportObserver : MonoBehaviour
{
    // Start is called before the first frame update
    public Vector2 spawnPos;
    public float iconHeight;
    public float iconWidth;

    public RectTransform panelRectTransform;
    public Transform rendererPanel;
    public GameObject buttonPrefab;
    public GameObject rendererMap;
    private Teleporter parentTeleporter;

    private void Start()
    {
        spawnPos = transform.position;        
        iconHeight /= 2;
        iconWidth /= 2;
        parentTeleporter = GetComponentInParent<Teleporter>();

        IconInitialzation();
    }

    public bool CheckCollideWithMouse(Vector3 mousePos) {
        if (mousePos.x > spawnPos.x + iconWidth) {
            return false;
        } else if (mousePos.x < spawnPos.x - iconWidth) {
            return false;
        } else if (mousePos.y > spawnPos.y + iconHeight) {
            return false;
        } else if (mousePos.y < spawnPos.y - iconHeight) {
            return false;
        }
        return true;
    }

    // Function to generate icon on the map.
    private void IconInitialzation()
    {
        GameObject teleportButton = Instantiate(buttonPrefab, rendererPanel);
        Button button = teleportButton.GetComponent<Button>();
        button.onClick.AddListener(delegate {
            if (!Player.instance.IsNearTeleport()) {
                Player.instance.transform.position = transform.position; 
            }
        });
        /*
        Vector2 position = GameManager.instance.fogOfWarCamera.WorldToScreenPoint(transform.position);
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        Vector2 sizeDelta = (new Vector2(Mathf.Abs(panelRectTransform.sizeDelta.x), Mathf.Abs(panelRectTransform.sizeDelta.y)) / 2f);
        Vector2 panelSize = panelRectTransform.rect.size / screenSize;

        position = (position * panelSize) + sizeDelta;
        teleportButton.transform.position = position;
        */
    }
}
