using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    private Player player;

    private Vector2 spawnPos;
    private bool isUnlocked; // Determine if this teleport is unlocked or not.
    private bool isPressing;
    private bool isChoosing;

    private int indLookingAt;
    private GameManager gameManager;
    private CameraManager2 cameraManager2;

    private bool pressingKey;
    public bool inHubScene = false;
    private void Start()
    {
        spawnPos = transform.position;
        player = Player.instance;
        gameManager = GameManager.instance;
        cameraManager2 = CameraManager2.instance;
        isUnlocked = false;
        indLookingAt = -1;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isUnlocked && other.gameObject.CompareTag("Player"))
        {
            GameManager.instance.unlockedTeleport.Add(this.gameObject);
            if(!inHubScene) SFXLibrary.instance.TeleportUnlockSFX.PlayFeedbacks();

            print("Total teleport: " + GameManager.instance.unlockedTeleport.Count);

            GameManager.instance.IncreaseExplorationBarAmount();
            player.IncreaseStatUnclamp("Map Discovered", 1);
            isUnlocked = true;
            Destroy(gameObject.GetComponent<BoxCollider2D>());

            
        }
    }

    private void Update() {

        //pressingKey = (Input.GetKeyDown(KeyCode.F));
        if (!isUnlocked) return;

        if (GameManager.instance.unlockedTeleport.Count == 1) {
            isChoosing = false;
            isPressing = false;
            indLookingAt = -1;
            cameraManager2.SwitchCameraToNormalCamera();
            return;
        }

        if (isPressing) {

            GameObject thisTeleport = this.gameObject;

            int ind;

            if (indLookingAt == -1) {
                ind = GameManager.instance.GetTeleporterInd(thisTeleport);
            } else {
                ind = indLookingAt;
            }

            if (ind == -1) {
                return;
            }

            if (Input.GetKeyDown(KeyCode.A)) {

                if (ind == 0) {
                    ind = gameManager.unlockedTeleport.Count - 1;
                } else {
                    ind -= 1;
                }

                cameraManager2.SetLookAtTeleport(gameManager.unlockedTeleport[ind].transform);

                indLookingAt = ind;
                isChoosing = true;

            } else if (Input.GetKeyDown(KeyCode.D)) {
                if (ind == gameManager.unlockedTeleport.Count - 1) {
                    ind = 0;
                } else {
                    ind += 1;
                }

                cameraManager2.SetLookAtTeleport(gameManager.unlockedTeleport[ind].transform);
                isChoosing = true;
                indLookingAt = ind;

            }
            
        }

        if (isChoosing && Input.GetKeyDown(KeyCode.F)) {
            
            if (gameManager.unlockedTeleport[indLookingAt] == this.gameObject) {
                isChoosing = false;
                isPressing = false;
                indLookingAt = -1;
                cameraManager2.SwitchCameraToNormalCamera();
                return;
            }
            player.transform.position = gameManager.unlockedTeleport[indLookingAt].transform.position;
            SFXLibrary.instance.ActivateTeleportSFX.StopFeedbacks();
            SFXLibrary.instance.TeleportSFX.PlayFeedbacks();
            isChoosing = false;
            isPressing = false;
            indLookingAt = -1;
            cameraManager2.SwitchCameraToNormalCamera();
        }
    }


    private void FixedUpdate() {
        if (!isUnlocked) return;

        float dist = Vector2.Distance(player.transform.position, this.gameObject.transform.position + Vector3.up * 0.6f);
        if (dist <= 2.4f && Input.GetKeyDown(KeyCode.F) && !isPressing && gameManager.unlockedTeleport.Count > 1)
        {
            

            SFXLibrary.instance.ActivateTeleportSFX.PlayFeedbacks();
            isPressing = true;
            cameraManager2.SwitchToTeleportCam();
            indLookingAt = -1;
        }
    }
    
    // Return spawn position.
    public Vector2 GetSpawnPos()
    {
        return spawnPos;
    }
}
