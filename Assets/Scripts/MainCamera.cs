using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
public class MainCamera : MonoBehaviour
{   
    public CinemachineVirtualCamera vcam;
    public static MainCamera instance;
    public Player player;
    public Vector3 viewOffset;

    [Header("Shaking Camera")]
    public float addTrauma;
    public float maxAngle;
    public Vector2 maxOffset;
    
    [Range(0f,1f)]
    public float linearDecreaseRate;

    [Header("Pan While Attack")]
    public Vector2 panWhileAttackOffset;
    public float resetSpeed;
    private float toPanAttack;
    private float currentResetSpeed;
    private bool isPanning;

    [Header("Follow Y")]
    public float lockAtYPosition;
    public float velocityThreshold;
    public float initFollowSpeed;
    private float followSpeedY;
    private float initYPos;
    private bool wasMovingInYPos;


    [Header("DropDownPan")]
    public float dropDownPanAmount;
    public float resetDropDownSpeed;
    public float heightThreshold;
    public float divingPanAmount;
    public float divingPanTime;
    private float currentDivingPanAmount;
    private float currentDropDownPanAmount;
    private float lastYHeight;
    private float lastYVeloc;
    private bool hasTurned;
    private bool isDiving;
    private float isDivingPanSpeed;
    private bool finishedDiving;

    [Header("Camera Pan")]
    public float amountToPan;
    public float minDistanceToFollow;
    public float minDistanceToCenter;
    public float hasMovedInDistanceCheck;

    [Range(0f,1f)]
    public float smoothingSpeed;
    private bool wasMoving;
    private float trauma;
    private float angle;
    private float offsetX;
    private float offsetY;
    private float currentPanAmount;
    private float scaledBaseSpeed;
    private float playerVeloc;
    private Vector2 lastPlayerPosition;
    private bool hasMovedInDistance;
    private float currentSpiuritualFollowSpeed;
    private Direction previousDirection;

    [Header("Debugging")]

    private float currentYDisplacement;
    private float playerYVeloc;

    AxisState axisState;
    private Camera mainCamera;

    private enum Direction {
        LEFT,RIGHT,SAME
    }

    void Start()
    {
        instance = this;

        mainCamera = Camera.main;
        player = Player.instance;
    }

    void Update()
    {   
        //var transposer = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
        //transposer.m_ScreenX = 0.36f;

        if (Input.GetKeyDown(KeyCode.C)) {
            vcam.transform.position = new Vector3(0,0,0);
            trauma += addTrauma;
        }
        playerVeloc = Mathf.Abs(player.rigidBody.velocity.x);
        
        if (!player.isSpirit) {
            PanCamera();
        } else {
            currentSpiuritualFollowSpeed = Mathf.Abs(player.rigidBody.velocity.x);
            currentPanAmount = 0;
            return;
        }

        if (Input.GetMouseButtonDown(0) && !player.isSpirit && !isPanning) {
            PanWhileAttack();
        } else if (Input.GetMouseButtonDown(0) && !player.isSpirit && isPanning) {
            currentResetSpeed = resetSpeed;
        } 
        
        if (!Input.GetMouseButtonDown(0)) {
            currentResetSpeed -= Time.deltaTime;
        }

        if (currentResetSpeed <= 0 && isPanning) {
            toPanAttack = 0;
            isPanning = false;
        }

        ComputeYFollowDirection();

        if (trauma > 0) {
            angle = maxAngle * TraumaFunction() * Random.Range(-1f, 1f);
            offsetX = maxOffset.x * TraumaFunction() * Random.Range(-1f, 1f);
            offsetY = maxOffset.y * TraumaFunction() * Random.Range(-1f, 1f);
            mainCamera.transform.Rotate(Vector3.up, angle * Time.deltaTime);
            mainCamera.transform.eulerAngles = mainCamera.transform.eulerAngles + new Vector3(0,0,1) * angle;
            mainCamera.transform.position = mainCamera.transform.position + new Vector3(offsetX, offsetY, viewOffset.z);
        }
        
       // camera.transform.position = Vector3.Lerp(transform.position, player.transform.position + new Vector3(currentPanAmount, 0, 0), smoothingSpeed * Time.deltaTime) + viewOffset;
        trauma -= linearDecreaseRate;
        trauma = Mathf.Clamp(trauma, 0, 1);
        currentResetSpeed = Mathf.Clamp(currentResetSpeed, 0, resetSpeed);
    }

    private void FixedUpdate() {
        float velocToUse = 0;
        float tempPlayerVeloc = Mathf.Abs(player.rigidBody.velocity.x);

        float speed = player.GetStat("Physical Move Speed");

        if (player.isSpirit)
        {
            speed = player.GetStat("Spiritual Move Speed");
        }

        if (tempPlayerVeloc < speed) {
            velocToUse = ExponentialFunction(tempPlayerVeloc);
        } else {
            velocToUse = GaussianFunction(tempPlayerVeloc);
        }

        velocToUse = ExponentialFunction(tempPlayerVeloc);
        // Debug.Log(velocToUse);
        mainCamera.transform.position = Vector3.Lerp(transform.position, player.transform.position + new Vector3(currentPanAmount + toPanAttack, followSpeedY - currentDropDownPanAmount, 0) + viewOffset, velocToUse * Time.fixedDeltaTime + (TangentLine(scaledBaseSpeed) + currentSpiuritualFollowSpeed + isDivingPanSpeed) * Time.fixedDeltaTime);
        transform.eulerAngles = new Vector3(0,0,0);
        scaledBaseSpeed -= Time.fixedDeltaTime;
        scaledBaseSpeed = Mathf.Clamp(scaledBaseSpeed, 0, 10);
    }

    private float TraumaFunction() {
        return Mathf.Pow(trauma, 2);
    }

    private float Cosh(float velocity) {
        //1.3f * Mathf.Exp((float)(-(velocity - 3.3)/(float)(2*Mathf.Pow(1.3f,2))))
        return 0.1f * (Mathf.Exp(velocity) + Mathf.Exp(-velocity))/2;
    }

    private float ExponentialFunction(float velocity) {
        return Mathf.Pow(1.1f, velocity)/3 + 1;
    }

    // d/dx 1.1^x + 1;
    private float TangentLine(float x) {
        return Mathf.Log(1.1f) * Mathf.Abs(x) + 2;
    }

    private float GaussianFunction(float x) {
        return  26f * Mathf.Exp((float)(-Mathf.Pow((x - 48f), 2)/(float)(2*Mathf.Pow(19.6f,2))));
    }

    private void ComputeYFollowDirection() {
        /*

        if (finishedDiving) {
            isDiving = false;
            finishedDiving = false;
            StartCoroutine(DivingDropDownPan());
        } else if (isDiving) {
            followSpeedY = 0;
            currentDivingPanAmount = divingPanAmount;
            isDivingPanSpeed = player.physicalFormCombat.speed;
            return;
        }
        if (!isDiving) {
            isDivingPanSpeed = 0;
            currentDivingPanAmount = 0;
        }
        float yDis = GetDisplacementY();
        currentYDisplacement = yDis;
        if (player.rigidBody.velocity.y < 0 && player.rigidBody.velocity.y <= -velocityThreshold) {
            followSpeedY = -Mathf.Abs(camera.transform.position.y - player.transform.position.y);
            wasMovingInYPos = true;
        } else if (!player.isSpirit && player.rigidBody.velocity.y == 0 && wasMovingInYPos && player.GetComponentInChildren<PhysicalFormController>().IsGrounded() && yDis >= heightThreshold) {
            wasMovingInYPos = false;
            StartCoroutine(DropDownPan());
        } else {
            followSpeedY = 0;
        }
        */
    }

    private void PanWhileAttack(){
        isPanning = true;
        float mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
        if (player.LookRight()) {
            toPanAttack = panWhileAttackOffset.x;
        } else {
            toPanAttack = - panWhileAttackOffset.x;
        }
        currentResetSpeed = resetSpeed;
    }

    private IEnumerator DropDownPan() {
        currentDropDownPanAmount = dropDownPanAmount;
        yield return new WaitForSeconds(resetDropDownSpeed);
        currentDropDownPanAmount = 0;
    }

    private IEnumerator DivingDropDownPan() {
        currentDropDownPanAmount = Mathf.Abs((mainCamera.transform.position.y - player.transform.position.y));
        yield return new WaitForSeconds(divingPanTime);
        currentDropDownPanAmount = 0;
        followSpeedY = 0;
        isDivingPanSpeed = 0;
    }

    private void PanCamera() {
        float displacement = GetDistance();
        if (displacement > minDistanceToFollow) {
            if (player.rigidBody.velocity.x > 0) {
                currentPanAmount = amountToPan;
                //camera.transform.position = Vector3.Lerp(transform.position, player.transform.position + viewOffset + new Vector3(panAmount, 0, 0), 0.5f);
            } else if (player.rigidBody.velocity.x < 0) {
                currentPanAmount = -amountToPan;
                //camera.transform.position = Vector3.Lerp(transform.position, player.transform.position + viewOffset + new Vector3(-panAmount, 0, 0), 0.5f);
            }
            previousDirection = GetDirection();
        } else if (hasMovedInDistance && displacement > minDistanceToCenter && HasChangedDirection()) {
            currentPanAmount = 0;
        } 

        if (displacement > hasMovedInDistanceCheck) {
            hasMovedInDistance = true;
        } else {
            hasMovedInDistance = false;
        }
    }

    private float GetDistance() {
        float input = Mathf.Abs(Input.GetAxisRaw("Horizontal"));
        if (input > 0 && !wasMoving) { // started moving in this frame;
            wasMoving = true;
            lastPlayerPosition = player.transform.position;
            return 0;
        } else if (input > 0 && wasMoving) { // moved in last frame and is still moving in this frame
            wasMoving = true;
            float displacement = Mathf.Abs(player.transform.position.x - lastPlayerPosition.x);
            return displacement;
        } else if (input == 0 && wasMoving) { // was moving but stopped in this frame
            wasMoving = false;
            float displacement = Mathf.Abs(player.transform.position.x - lastPlayerPosition.x);
            return displacement;
        } else { // wasn't moving and is not movnig;
            wasMoving = false;
            return 0;
        }
    }

    private float GetDisplacementY() {
        float yVeloc = Mathf.Abs(player.rigidBody.velocity.y);
        playerYVeloc = player.rigidBody.velocity.y;
        HasTurnedInYDirection();
        if (hasTurned) {
            float toReturn = Mathf.Abs(player.transform.position.y - initYPos);
            initYPos = player.transform.position.y;
            return toReturn;
        } else {
            return Mathf.Abs(player.transform.position.y - initYPos);
        }
    }

    private void HasTurnedInYDirection() {
        if (lastYVeloc > 0 && player.transform.position.y < lastYHeight) {
            hasTurned = true;
        } else {
            hasTurned = false;
        }
        lastYVeloc = player.rigidBody.velocity.y;
        lastYHeight = player.transform.position.y;
    }

    private bool HasChangedDirection() {
        if (previousDirection == GetDirection()) {
            return false;
        } else {
            return true;
        }
    }

    private Direction GetDirection() {
        float x = Input.GetAxisRaw("Horizontal");
        if (x > 0) {
            scaledBaseSpeed = 0;
            return Direction.RIGHT;
        } else if (x < 0) {
            scaledBaseSpeed = 0;
            return Direction.LEFT;
        } else {
            scaledBaseSpeed = 2;
        }
        return previousDirection;
    }

    public void SetIsDiving(bool boo) {
        isDiving = boo;
    }

    public void SetFinishedDiving(bool boo) {
        finishedDiving = boo;
    }

    /*
    private void DynamicChanges(Vector2 dir)
    {
        Vector3 startingPos = transform.position;
        float dist = Vector3.Distance(transform.position, startingPos + (Vector3)dir);

        if (dist <= dir.magnitude)
        {
            dist
        }
    }
    */
}
