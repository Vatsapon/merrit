using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Cinemachine;
public class CameraManager2 : MonoBehaviour
{

    public CinemachineVirtualCamera normalCam;

    public CinemachineVirtualCamera teleportCam;

    public CinemachineVirtualCamera activeCam;

    public static CameraManager2 instance;
    public bool isChoosingTeleport;

    public void Awake() {
        instance = this;
    }

    // Start is called before the first frame update
    void Start() {
        if (normalCam == null) {
            normalCam = GameObject.FindGameObjectWithTag("Normal Cam").GetComponent<CinemachineVirtualCamera>();
            activeCam = normalCam;
        }
        if (teleportCam == null) {
            teleportCam = GameObject.FindGameObjectWithTag("Teleport Cam").GetComponent<CinemachineVirtualCamera>();
        }
        
        isChoosingTeleport = false;
    }

    public void SwitchCameraToNormalCamera() {
        normalCam.Priority = 10;
        teleportCam.Priority = 0;
        activeCam = normalCam;
        isChoosingTeleport = false;
    }

    public void SwitchToTeleportCam() {
        teleportCam.Priority = 10;
        normalCam.Priority = 0;
        activeCam = teleportCam;
        teleportCam.Follow = Player.instance.transform;
        isChoosingTeleport = true;
    }

    public void SetLookAtTeleport(Transform look) {
        if (activeCam == teleportCam) {
            teleportCam.Follow = look;
        }
    }

}
