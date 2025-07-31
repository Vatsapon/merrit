using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollowTarget : MonoBehaviour
{
    public Transform[] pos;

    public float[] importance;

    public float[] averagedImportance;

    public float minXDelta;
    public float currentXDelta;
    public bool shouldZoomOut()
    {
        if (currentXDelta > minXDelta) return true;

        return false;
    }

    private void Start()
    {
        float sum = 0;
        for (int i = 0; i < importance.Length; i++)
        {
            sum += importance[i];
        }
        float avg = sum / importance.Length;

        for (int i = 0; i < importance.Length; i++)
        {
            averagedImportance[i] = importance[i] / avg;
        }
    }

    private void Update()
    {
        transform.position = ArrayCalculation();
    }

    Vector3 ArrayCalculation()
    {
        float x = 0;
        float smallest = pos[1].position.x;
        float largest = pos[1].position.x;

        for (int i = 0; i < importance.Length; i++)
        {
            if (i < importance.Length - 4)
            {
                if (pos[i].position.x < smallest)
                    smallest = pos[i].position.x;

                if (pos[i].position.x > largest)
                    largest = pos[i].position.x;
            }

            x += pos[i].position.x * averagedImportance[i];
        }

        currentXDelta = largest - smallest;

        float y = 0;
        for (int i = 0; i < importance.Length; i++)
        {
            y += pos[i].position.y * averagedImportance[i];
        }

        float z = 0;
        for (int i = 0; i < importance.Length; i++)
        {
            z += pos[i].position.z * averagedImportance[i];
        }


        return new Vector3 (x/importance.Length,  y/ importance.Length, z/ importance.Length);
    }
}
