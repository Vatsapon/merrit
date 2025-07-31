using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ParallaxLayer : MonoBehaviour {
    
    public float parallaxFactor;

    public void Move(float deltaX, float deltaY) {
        
        //print($"x: {deltaX}, y: {deltaY}");

        if (deltaX != 0f) {
            Vector3 newPos = transform.localPosition;
            newPos.x -= deltaX * parallaxFactor;
            transform.localPosition = newPos;
        }

        if (deltaY != 0f) {
            Vector3 newPos = transform.localPosition;
            newPos.y -= deltaY * parallaxFactor;
            transform.localPosition = newPos;
        }

    }

}