using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Minimap : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveCameraSpeed = 2f;

    [Header("References")]
    [SerializeField] private Camera miniMapCamera;
    [SerializeField] private RectTransform miniMapImage;

    private Vector2 startPosition = Vector2.zero;

    private Player player;

    private void Start()
    {
        player = Player.instance;
        miniMapCamera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, miniMapCamera.transform.position.z);
    }
    
    private void Update()
    {
        Vector2 difference = startPosition - (Vector2)miniMapCamera.WorldToScreenPoint(player.transform.position);
        miniMapImage.localPosition = difference * moveCameraSpeed;
    }

    public void ResetPosition(Vector2 position)
    {
        miniMapCamera.transform.position = new Vector3(position.x, position.y, miniMapCamera.transform.position.z);
        startPosition = miniMapCamera.WorldToScreenPoint(position);
    }
}
