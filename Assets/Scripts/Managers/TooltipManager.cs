using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    public enum TooltipPosition
    {
        TOP_LEFT, TOP_RIGHT, BOTTOM_LEFT, BOTTOM_RIGHT
    }

    [Header("Settings")]
    public float offsetX = 5f; // Offset Tooltip from the screen edge in X axis.
    public float offsetY = 5f; // Offset Tooltip from the screen edge in Y axis.

    [Header("Components")]
    public Transform HUDParent; // HUD Parent to spawn Tool tip as a child inside.
    public GameObject toolTipPrefab; // Prefab to spawn Tool tip.

    private Tooltip currentTooltip; // Current Tooltip.

    private void Start()
    {
        
    }

    public TooltipPosition GetPosition()
    {
        Vector2 mousePosition = Camera.main.ScreenToViewportPoint(Input.mousePosition);

        TooltipPosition position = TooltipPosition.BOTTOM_RIGHT;

        return position;
    }
}
