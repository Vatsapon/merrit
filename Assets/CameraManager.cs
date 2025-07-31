using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
public class CameraManager : MonoBehaviour
{

    public CinemachineVirtualCamera bossCam;
    public CinemachineVirtualCamera normalCam;
    public CinemachineVirtualCamera activeCam;
    public CamFollowTarget target;

    public float targetOrthoSize;
    public float currentOrthoSize;

    // Start is called before the first frame update
    void Start()
    {
        activeCam = null;      
        //SwitchCameraToBossCamera();
    }

    public void SwitchCameraToBossCamera() {
        bossCam.Priority = 10;
        normalCam.Priority = 0;
        activeCam = bossCam;
    }

    public void SwitchCameraToNormalCamera() {
        normalCam.Priority = 10;
        bossCam.Priority = 0;
        activeCam = normalCam;
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if (Input.GetKey(KeyCode.U)) {
            SwitchCameraToBossCamera();
        } else if (Input.GetKey(KeyCode.Y)) {
            SwitchCameraToNormalCamera();
        }   
        */

        bossCam.m_Lens.OrthographicSize = currentOrthoSize;

        if (target.shouldZoomOut()) targetOrthoSize = 7;
        else targetOrthoSize = 6;

        if (currentOrthoSize != targetOrthoSize)
        {
            currentOrthoSize += (targetOrthoSize - currentOrthoSize) * Time.deltaTime * 5;
        }
    }
}
