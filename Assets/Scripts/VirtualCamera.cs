using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using MoreMountains.Feedbacks;


[System.Serializable]
public enum CameraEventName {
    ATTACK3RD, DEFAULT, ENEMY_DIE, PLAYER_TAKE_DAMAGE, TWISTED_DIVE, SPIRIT_RESONANCE, BOULDER_SMASH
}

[System.Serializable]
public class CameraEvent {
    public CameraEventName eventName;
    public float amplitude;
    public float frequency;
    public float duration;
    public SignalSourceAsset source;
}

public class VirtualCamera : MonoBehaviour
{
    public static VirtualCamera instance;
    private enum Direction {
        LEFT,RIGHT,SAME
    }
    public CinemachineVirtualCamera vcam;
    private CinemachineFramingTransposer transposer;
    public Player player;

    [Header("Settings")]
    public Vector3 viewOffset;

    [Header("Spirit Pan Speed")]

    public float spiritPanSpeed;

    [Header("Pan While Attack")]

    public Vector2 panAttackOffset;
    public float attackResetSpeed;
    public float pullBackSpeed;
    private float currentPullBackSpeed;
    private float currentAttackResetSpeed;
    private float toPanAttack;
    private bool isPanningAttack;
    private float attackAnimationCurveCounter = 0;
    [SerializeField] private AnimationCurve panWhileAttackCurve;

    [Header("Pan Left / Right")] 
    public Vector2 panOffset;
    public float minPressTime;
    public float panningSpeed;
    private Vector2 lastPlayerPosition;
    private float trasnposerXtoLerpTo;
    private float currentPressTime;
    private bool wasMoving;
    private bool isPressingA;
    private bool isPressingD;
    private Direction previousDirection;

    [Header("Drop Down Pan")]

    public float minHeight;
    public float dropDownPanSpeed;
    public float dropDownPanAmount;
    public float resetDropDownSpeed;
    public float dropPanCurveCounter;
    [SerializeField] private AnimationCurve dropDownPanCurve;
    private float currentDropDownPanAmount;
    // private float playerYVeloc;
    private float initYPos;
    private float lastYHeight;
    private float lastYVeloc;
    private bool hasTurned;
    // private bool wasMovingInYPos;

    [Header("Camera Shake")]
    public float time;
    public CinemachineImpulseSource source;
    public List<CameraEvent> cameraEventList = new List<CameraEvent>(); 
    private Dictionary<CameraEventName, CameraEvent> camEventDict;
    private float shakeTimer;
    private float shakeTimerTotal;
    private float initAmplitude;
    private float initIntensity;
    private float currentOnTopShakeAmplitude = 0;
    public MMFeedbacks RumblingSFX;
    public MMFeedbacks SRRumblingSFX;

    [Header("Debugging")]
    public float displacementX;

    public bool isInBossPhase;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        transposer = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
        camEventDict = new Dictionary<CameraEventName, CameraEvent>();
        cameraEventList.ForEach(eve => {
            camEventDict[eve.eventName] = eve;
        });

        player = Player.instance;
        lastPlayerPosition = player.transform.position;
        trasnposerXtoLerpTo = 0.5f;
        source = GetComponent<CinemachineImpulseSource>();
        StartCoroutine(TestCurve());
        isInBossPhase = false;
    }

    // Update is called once per frame
    void LateUpdate()
    {

        if (!isInBossPhase) {

            if (Input.GetMouseButtonDown(0) && !player.isSpirit && !isPanningAttack && player.physicalFormCombat.GetAttackDirection() != PhysicalFormCombat.AttackDirection.UP && !GameManager.instance.IsUI()) {
                PanWhileAttack();
            } else if (Input.GetMouseButtonDown(0) && !player.isSpirit && isPanningAttack) {
                currentAttackResetSpeed = attackResetSpeed;
            } 
            
            if (!Input.GetMouseButtonDown(0)) {
                currentAttackResetSpeed -= Time.deltaTime;
                currentPullBackSpeed = 0;
            }

            if (currentAttackResetSpeed <= 0 && isPanningAttack) {
                toPanAttack = 0;
                isPanningAttack = false;
                currentPullBackSpeed = 0;
            }
        }

        
        if (shakeTimer > 0) {
        //   DecayShake();
            shakeTimer -= Time.deltaTime;
        // CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        //  cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = Mathf.Lerp(initIntensity, 0, (1 - shakeTimer / shakeTimerTotal));
        } else {
            shakeTimer = 0;
        }

    }

    private void FixedUpdate() {

        if (!isInBossPhase) {
            if (!player.isSpirit) {
                PanCamera();
                CamEffectVertical();
                transposer.m_ScreenX = Mathf.Lerp(transposer.m_ScreenX, trasnposerXtoLerpTo + toPanAttack, (panningSpeed + currentPullBackSpeed) * Time.fixedDeltaTime);
            //transposer.m_ScreenY = Mathf.Lerp(transposer.m_ScreenY, 0.59f - currentDropDownPanAmount, (dropDownPanSpeed) * Time.fixedDeltaTime);
            } else {
                transposer.m_ScreenX = Mathf.Lerp(transposer.m_ScreenX, 0.5f, (spiritPanSpeed) * Time.fixedDeltaTime);
            }
        }
        
    }

    public void CameraShake(CameraEventName name) {
        CameraEvent cameraEvent = camEventDict[name];

        if (RumblingSFX != null && name != CameraEventName.SPIRIT_RESONANCE) {
            RumblingSFX.PlayFeedbacks();
        }

        source.GenerateImpulse();
        source.m_ImpulseDefinition.m_RawSignal = cameraEvent.source;
        source.m_ImpulseDefinition.m_AmplitudeGain = cameraEvent.amplitude;
        source.m_ImpulseDefinition.m_FrequencyGain = cameraEvent.frequency;
        source.m_ImpulseDefinition.m_TimeEnvelope.m_DecayTime = cameraEvent.duration;
        //Debug.Log($"CameraEvent: {cameraEvent.eventName}, Amp: {cameraEvent.amplitude}");
        shakeTimerTotal = cameraEvent.duration;
        shakeTimer = cameraEvent.duration;

    }

    public void SetInBossPhase() {
        isInBossPhase = true;
    }

    public void CameraShake(List<CameraEventName> eventList) {
        
        float cumAmplitude = 0;
        float cumFrequency = 0;
        float maxDuration = 0;

        eventList.ForEach(evt => {
            CameraEvent temp = camEventDict[evt];
            cumAmplitude += temp.amplitude;
            cumFrequency += temp.frequency;
            if (temp.duration > maxDuration) {
                maxDuration = temp.duration;
            }
        });

        CameraEvent tempSetttings = camEventDict[eventList[0]];

        source.GenerateImpulse();   
        source.m_ImpulseDefinition.m_RawSignal = tempSetttings.source;
        source.m_ImpulseDefinition.m_AmplitudeGain = cumAmplitude;
        source.m_ImpulseDefinition.m_FrequencyGain = cumFrequency;
        source.m_ImpulseDefinition.m_TimeEnvelope.m_DecayTime = maxDuration;
        //Debug.Log($"CameraEvent: {cameraEvent.eventName}, Amp: {cameraEvent.amplitude}");
        shakeTimerTotal = maxDuration;
        shakeTimer = maxDuration;

    }


    //        CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    //        Debug.Log($"CameraEvent: {cameraEvent.eventName}, Amp: {cameraEvent.amplitude}");
    //        cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = cameraEvent.amplitude;
    //        cinemachineBasicMultiChannelPerlin.m_FrequencyGain = cameraEvent.frequency;

    private IEnumerator TestCurve() {
        while (attackAnimationCurveCounter < 1) {
//            Debug.Log(animationCurve.Evaluate(attackAnimationCurveCounter));
            attackAnimationCurveCounter += 0.05f;
            yield return new WaitForSeconds(1f);
        }
    }

    private void PanWhileAttack(){
        isPanningAttack = true;
        float mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
        if (player.LookRight()) {
            toPanAttack = - panAttackOffset.x;
        } else {
            toPanAttack = panAttackOffset.x;
        }
        currentPullBackSpeed = pullBackSpeed;
        currentAttackResetSpeed = attackResetSpeed;
    }

    private void PanCamera() {
        if (Input.GetKey(KeyCode.A) && !isPressingD) {
            currentPressTime += Time.fixedDeltaTime;
            isPressingA = true;
        } else if (!isPressingD) {
            currentPressTime = 0;
            isPressingA = false;
        }

        if (Input.GetKey(KeyCode.D) && !isPressingA) {
            currentPressTime += Time.fixedDeltaTime;
            isPressingD = true;
        } else if (!isPressingA) {
            currentPressTime = 0;
            isPressingD = false;
        }

        if (currentPressTime >= minPressTime) {
            if (player.LookRight()) {
                trasnposerXtoLerpTo = 0.5f - panOffset.x;
            } else {
                trasnposerXtoLerpTo = 0.5f + panOffset.x;
            }
        } else if (HasChangedDirection()) {
            trasnposerXtoLerpTo = 0.5f;
        }
        previousDirection = GetDirection();

    }

    private float GetDisplacementX() {
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
            return Direction.RIGHT;
        } else if (x < 0) {
            return Direction.LEFT;
        }
        return previousDirection;
    }

    private void CamEffectVertical() {
        float displacement = GetDisplacementY();
        if (displacement >= minHeight && player.physicalFormController.IsGrounded() && Input.GetAxisRaw("Horizontal") < 0.1) {
            StartCoroutine(DropDownPanCurve());
            //wasMovingInYPos = false;
        }
    }

    public IEnumerator DropDownPanCurve() {
        float dx = 0;
        while (dx < 1) {
            dx += Time.fixedDeltaTime;
            float dy = dropDownPanCurve.Evaluate(dx);
            //Debug.Log($"dx: {dx}, dy: {dy}");
            transposer.m_ScreenY = 0.59f - dy;
            yield return new WaitForSeconds(Time.fixedDeltaTime);
        }
        transposer.m_ScreenY =  0.59f;
    }

    private IEnumerator DropDownPan() {
        currentDropDownPanAmount = dropDownPanAmount;
        yield return new WaitForSeconds(resetDropDownSpeed);
        currentDropDownPanAmount = 0;
        //CameraShake(amplitude, frequency, time);
        //source.GenerateImpulse();
    }
    private float GetDisplacementY() {
        float yVeloc = Mathf.Abs(player.rigidBody.velocity.y);
        //playerYVeloc = player.rigidBody.velocity.y;
        HasTurnedInYDirection();
        float toReturn;
        if (hasTurned) {
            toReturn = Mathf.Abs(player.transform.position.y - initYPos);
            initYPos = player.transform.position.y;
        } else {
            toReturn = Mathf.Abs(player.transform.position.y - initYPos);
        }
        if (Mathf.Abs(player.rigidBody.velocity.y) == 0) {
            initYPos = player.transform.position.y;
        }
        return toReturn;
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

    public CameraEvent GetCameraEvent(CameraEventName cameraEventName) {
        return camEventDict[cameraEventName];
    }


}
