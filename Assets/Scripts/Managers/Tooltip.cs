using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tooltip : MonoBehaviour
{
    public enum TooltipPosition
    {
        TOP_LEFT, TOP_RIGHT, BOTTOM_LEFT, BOTTOM_RIGHT
    }

    [Header("Components")]
    public RectTransform panelRect;

    private void Update()
    {
        Vector2 mousePosition = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        print(mousePosition);
    }

    public TooltipPosition GetPosition()
    {
        Vector2 mousePosition = Camera.main.ScreenToViewportPoint(Input.mousePosition);

        TooltipPosition position = TooltipPosition.BOTTOM_RIGHT;

        return position;
    }
}
