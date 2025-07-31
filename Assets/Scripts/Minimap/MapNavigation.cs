using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapNavigation : MonoBehaviour
{
    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float zoomIncrement = 5f; // Amount of zoom.
    [SerializeField] private float zoomMin = 1f; // Maximum amount of zoom.
    [SerializeField] private float zoomMax = 5f; // Maximum amount of zoom.

    [Header("References")]
    [SerializeField] private RectTransform rendererTransform; // Renderer parent.

    private void Update()
    {
        Zooming();
        Navigating();
        MapClamping();
    }

    // Function to handle zoom.
    private void Zooming()
    {
        Vector2 center = rendererTransform.rect.size / 2f;
        Vector2 mousePos = (Vector2)rendererTransform.InverseTransformPoint(Input.mousePosition) + center;
        Vector2 movePos = mousePos - center;

        // Zoom in.
        if (Input.mouseScrollDelta.y > 0f)
        {
            rendererTransform.position += (Vector3)movePos;
            rendererTransform.localScale += Vector3.one * zoomIncrement;
            
            // If scale is already reaches maximum, don't move further.
            if (rendererTransform.localScale.x < zoomMax)
            {
                movePos *= 1 + zoomIncrement;
            }
            rendererTransform.position -= (Vector3)movePos;
        }

        // Zoom out.
        if (Input.mouseScrollDelta.y < 0f)
        {
            rendererTransform.position += (Vector3)movePos;
            rendererTransform.localScale -= Vector3.one * zoomIncrement;

            // If scale is already reaches minimum, don't move further.
            if (rendererTransform.localScale.x > zoomMin)
            {
                movePos *= 1 - zoomIncrement;
            }

            rendererTransform.position -= (Vector3)movePos;
        }

        float value = Mathf.Clamp(rendererTransform.localScale.x, zoomMin, zoomMax);
        rendererTransform.localScale = Vector3.one * value;
    }

    // Function to move the map around.
    private void Navigating()
    {
        if (Input.GetKey(KeyCode.W))
        {
            rendererTransform.position += Vector3.down * moveSpeed;
        }

        if (Input.GetKey(KeyCode.S))
        {
            rendererTransform.position += Vector3.up * moveSpeed;
        }

        if (Input.GetKey(KeyCode.A))
        {
            rendererTransform.position += Vector3.right * moveSpeed;
        }

        if (Input.GetKey(KeyCode.D))
        {
            rendererTransform.position += Vector3.left * moveSpeed;
        }
    }

    // Function to clamp the image in the frame.
    private void MapClamping()
    {
        float scale = rendererTransform.localScale.x;

        float minX = (scale - 1) * (rendererTransform.rect.width / 2f);
        float minY = (scale - 1) * (rendererTransform.rect.height / 2f);

        float x = rendererTransform.offsetMin.x;
        float y = rendererTransform.offsetMin.y;

        x = Mathf.Clamp(x, -minX, minX);
        y = Mathf.Clamp(y, -minY, minY);

        rendererTransform.offsetMin = new Vector2(x, y);
        rendererTransform.offsetMax = new Vector2(x, y);
    }
}
