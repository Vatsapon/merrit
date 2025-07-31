using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
public class FlyingCube : MonoBehaviour
{
    public enum FlyingCubeState {
        MOVING, AIMING, SHOOTING, AWAIT_DESTINATION, INFLATED, SMASHING, RESETTING, MOVING_HORI, MOVING_VERTI, TRANSITION_TO_NEW_STATE, FINISHED_HORI_SWEEP, FINISHED_VERTI_SWEEP
    }
    public FlyingCubeState state = FlyingCubeState.MOVING;
    public Transform face;
    private Rigidbody2D rigidbody;
    private Player player;

    [Header("Look At Config")]
    public Transform lookAtObject;
    public bool shouldLookAtPlayer = true;
    private float rotZ;
    public float turningSpeed;
    public float aimingTolerance;
    public LineRenderer lineRenderer;
    public float lineWidth;


    [Header("Movement Config")]
    public float flyingSpeed;
    public float maxRayCastX = 10;
    public float maxRayCastY = 5;
    public float lerpDuration = 2;
    public Vector2 destination;
    public float positioningTolerance;
    public LayerMask groundLayerMask;
    private float elapsedDuration = 0;
    private Vector3 fromPosition;

    [Header("Aim Config")]
    public float aimDuration;
    private float aimCounter;

    [Header("Shooting Config")]
    public float shootDuration;
    public LayerMask playerLayer;
    public LayerMask groundLayer;
    private float shootCounter;
    private float shootPlayerAssistTime;

    [Header("Laser Settings")]
    public GameObject laserPrefab;
    private Damage laserDamage;
    private float laserFrequency;
    private float shootInterval;
    private float shootFrequencyCounter;
    private float minFloatHeight;
    private float stopAimSeconds;

    private float resetSpeed;
    private float smashSpeed;
    private Damage smashDamage;
    private Vector3 initPos;

    [Header("Shake")]

    public float shakeIntensityX;
    public float shakeIntensityY;


    [Header("Phase 2")]
    
    private float horizontalSweepTime;
    private float verticalSweepLockTime;
    private float veritcalMoveTime;
    private float verticalShootTime;
    private int numVerticalSweep;
    private float transitionTime;

    private bool shouldDrawLineOfFireHori;
    private bool shouldDrawLineOfFireVerti;

    private float up;
    private float down;
    private float left;
    private float right;


    [Header("Feedbacks")]
    public AudioSource movingSFX;
    private float initialMoveVolume;
    public GameObject preAttackSFX;
    public GameObject attackSFX;
    public AudioSource attackLoopSFX;
    [Space(5)]
    public AudioSource shakeSFX;
    public AudioSource impactSFX;
    private float shootTime;
    private float postShootCounter;


    [Header("Customization")]
    public GameObject glowLight;
    public Color preAttackColor;
    public Color attackColor;



    private BossFightController bossFightController;
    private void Start() {
        bossFightController = BossFightController.instance;
        this.rigidbody = GetComponent<Rigidbody2D>();
        resetSpeed = bossFightController.flyingCubeResetSpeed;
        smashSpeed = bossFightController.flyingCubeSmashSpeed;
        smashDamage = bossFightController.smashDamage;
        laserDamage = bossFightController.laserDamage;
        laserFrequency = bossFightController.laserFrequency;
        shootInterval = 1 / laserFrequency;
        minFloatHeight = bossFightController.minFloatHeight;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        stopAimSeconds = bossFightController.stopAimSeconds;
        shootPlayerAssistTime = bossFightController.shootPlayerAssistTime;

        transitionTime = bossFightController.phase2TransitionTime;
        horizontalSweepTime = bossFightController.horizontalSweepTime;

        veritcalMoveTime = bossFightController.veritcalMoveTime;
        verticalSweepLockTime = bossFightController.verticalSweepLockTime;
        numVerticalSweep = bossFightController.numVerticalSweep;
        verticalShootTime = bossFightController.verticalShootTime;

        player = Player.instance;

        left = bossFightController.leftDown.x + 1;
        right = bossFightController.rightUp.x - 1;
        down = bossFightController.leftDown.y + 1;
        up = bossFightController.rightUp.y - 1;

        initialMoveVolume = movingSFX.volume;
        shootTime = attackSFX.GetComponent<AudioSource>().clip.length;
        attackLoopSFX.Pause();

        lookAtObject = Player.instance.transform;
    }

    void Update()
    {
        MoveSoundControl();

        Debug.DrawRay(face.position, face.forward * 1000);
        lineRenderer.SetPositions(new Vector3[] {
            Vector3.zero, Vector3.zero
        });

        if (shouldDrawLineOfFireHori) {
            AimHorizontal();
            DrawLineOfFire();
        } else if (shouldDrawLineOfFireVerti) {
            AimVertical();
            DrawLineOfFire();
        }

        if (bossFightController.bossState == BossFightController.BossPhase.PHASE_1 || bossFightController.bossState == BossFightController.BossPhase.MINI_PHASE) {
            Phase1();
        }


        if (lineRenderer.GetPosition(0) == Vector3.zero)
        {
            //pre
            preAttackSFX.SetActive(false);

            //short laser attack for phase 1
            if (postShootCounter >= 0) postShootCounter += Time.deltaTime;
            if (postShootCounter > shootTime + 0.1f)
            {
                attackSFX.SetActive(false);
                postShootCounter = -1;
            }

            //constant beamming attack for phase 2
            if (bossFightController.bossState != BossFightController.BossPhase.PHASE_2) attackLoopSFX.Stop();
            if (!bossFightController.hasFinishedTransition) attackLoopSFX.Stop();
            Glow(false);

        }

    }



    public IEnumerator TransitionToVerticalSweepState(Vector2 dest) {

        float step = transitionTime / 100;
        float transitionCounter = 0;
        Vector3 initPos = this.transform.position;
        

        state = FlyingCubeState.TRANSITION_TO_NEW_STATE;

        while (transitionCounter <= transitionTime) {
            float t = transitionCounter / transitionTime;
            this.transform.position = Vector3.Lerp(initPos, dest, t);
            transitionCounter += step;
            yield return new WaitForSecondsRealtime(step);
        }

        state = FlyingCubeState.MOVING_VERTI;
        bossFightController.hasFinishedTransition = true;

    }

    public IEnumerator MoveVerticalSweep(Vector2 dest) {

        AimHorizontal();
        shouldDrawLineOfFireVerti = false;

        float step = veritcalMoveTime / 40;
        float transitionCounter = 0;
        Vector3 initPos = this.transform.position;

        while (transitionCounter <= veritcalMoveTime) {
            float t = transitionCounter / veritcalMoveTime;
            this.transform.position = Vector3.Lerp(initPos, dest, t);
            transitionCounter += step;
            yield return new WaitForSeconds(step);
        }

        shouldDrawLineOfFireHori = true;
        lineRenderer.startColor = attackColor;
        
        yield return new WaitForSeconds(verticalSweepLockTime);

        float shootCounter = 0;

        step = verticalShootTime / 40;

        while (shootCounter <= verticalShootTime) {
            shootCounter += step;
            Shoot(true);
            yield return new WaitForSeconds(step);
             attackSFX.SetActive(false);

        }

        state = FlyingCubeState.MOVING_VERTI;
        shouldDrawLineOfFireHori = false;
    
    }

    public IEnumerator TransitionToHorizontalSweepState(Vector2 dest) {

        float step = transitionTime / 100;
        float transitionCounter = 0;
        Vector3 initPos = this.transform.position;

        state = FlyingCubeState.TRANSITION_TO_NEW_STATE;

        while (transitionCounter <= transitionTime) {
            float t = transitionCounter / transitionTime;
            this.transform.position = Vector3.Lerp(initPos, dest, t);
            transitionCounter += step;
            yield return new WaitForSeconds(step);
        }

        state = FlyingCubeState.MOVING_HORI;
        bossFightController.hasFinishedTransition = true;
    }

    public IEnumerator SweepHorizontal(Vector2 dest) {

        float step = horizontalSweepTime / 100;
        float transitionCounter = 0;
        Vector3 initPos = this.transform.position;

        shouldDrawLineOfFireHori = false;
        shouldDrawLineOfFireVerti = true;
        lineRenderer.startColor = attackColor;
        lineRenderer.endColor = attackColor;

        state = FlyingCubeState.MOVING_HORI;

        while (transitionCounter <= horizontalSweepTime) {
            float t = transitionCounter / horizontalSweepTime;
            this.transform.position = Vector3.Lerp(initPos, dest, t);
            transitionCounter += step;
            Shoot(true);
            yield return new WaitForSeconds(step);
            attackSFX.SetActive(false);

        }

        state = FlyingCubeState.FINISHED_HORI_SWEEP;
        bossFightController.hasFinishedTransition = true;
        shouldDrawLineOfFireHori = false;
        shouldDrawLineOfFireVerti = false;
    }

    private void DrawLineVertical() {
        float yDirection = player.transform.position.y - this.transform.position.y > 0 ? 1 : - 1;
        float y = yDirection * 10;
        lineRenderer.SetPositions(
            new Vector3[] {
                face.position, new Vector3(face.position.x, face.position.y + y)
            }
        );
    }

    private void DrawLineHorizontal() {
        float xDirection = player.transform.position.x - this.transform.position.x > 0 ? 1 : - 1;
        float x = xDirection * 10;
        lineRenderer.SetPositions(
            new Vector3[] {
                face.position, new Vector3(x + face.position.x, face.position.y)
            }
        );
    }

    private void DrawLineOfFire() {
        RaycastHit2D raycastHit2D = Physics2D.Raycast(face.position, face.forward, Mathf.Infinity, groundLayer);
        Vector3 vec = face.forward;
        Vector3 par = vec * raycastHit2D.distance + face.position;
        lineRenderer.SetPositions(new Vector3[] {
            face.position, par
        });
    }

    private void Phase1() {

        LookAtPlayer(shouldLookAtPlayer);

        if (state == FlyingCubeState.INFLATED) {
            elapsedDuration = 0;
            return;
        }

        if (state == FlyingCubeState.SMASHING) {
            Smash();
            return;
        }

        if (state == FlyingCubeState.RESETTING) {
            rigidbody.velocity = Vector2.zero;
            this.transform.position = Vector3.Lerp(this.transform.position, initPos, resetSpeed * Time.deltaTime);
            if (Vector2.Distance(this.transform.position, initPos) < 0.05) {
                state = FlyingCubeState.INFLATED;
                this.transform.position = initPos;
                bossFightController.SetIsSmashing(false);
                rigidbody.velocity = Vector2.zero;
            }
            return;
        }

        if (state == FlyingCubeState.MOVING)
        {
            shouldLookAtPlayer = true;
            MoveToDestination();
            if (ReachDestination()) {
                state = FlyingCubeState.AIMING;
            }
        }

        if (state == FlyingCubeState.AIMING)
        {
            DrawLineOfFire();

            //Aim Sound
            if (!preAttackSFX.activeInHierarchy) preAttackSFX.SetActive(true);

            lineRenderer.startColor = preAttackColor;
            lineRenderer.endColor = preAttackColor;
            aimCounter += Time.deltaTime;

            Glow(true);

            if (aimCounter <= stopAimSeconds) {
                shouldLookAtPlayer = true;
            } else {
                shouldLookAtPlayer = false;
            }

            if (aimCounter >= aimDuration + stopAimSeconds) {
                state = FlyingCubeState.SHOOTING;
                aimCounter = 0;
            }
        }

        if (state == FlyingCubeState.SHOOTING)
        {
            DrawLineOfFire();
            lineRenderer.startColor = attackColor;
            lineRenderer.endColor = attackColor;
            Debug.DrawRay(face.position, face.forward * 1000, attackColor);

            shouldLookAtPlayer = false;
            shootCounter += Time.deltaTime;

            if (shootCounter <= shootDuration - shootPlayerAssistTime) {
                Shoot(bossFightController.bossState == BossFightController.BossPhase.PHASE_2 && bossFightController.hasFinishedTransition);
            }

            if (shootCounter >= shootDuration) {
                state = FlyingCubeState.AWAIT_DESTINATION;
                shootCounter = 0;
            }

        }
    }

    private void AimHorizontal() {

        float signedPos = lookAtObject.position.x - this.transform.position.x;

        if (signedPos >= 0) {
            transform.rotation = Quaternion.Euler(0, 0, 90);
        } else {
            transform.rotation = Quaternion.Euler(0, 0, -90);
        }

    }

    private void AimVertical() {
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    private void Shoot(bool phase2) {
        RaycastHit2D raycast = Physics2D.Raycast(face.position, face.forward, Mathf.Infinity,  playerLayer);
        if (!attackSFX.activeInHierarchy && !phase2)
        {
            attackSFX.SetActive(true);
            postShootCounter = 0;
        }
        if (phase2 && !attackLoopSFX.isPlaying)
        {
            attackLoopSFX.Play();
        }

        if (lineRenderer.endColor == attackColor) Glow(true);

        if (raycast && raycast.collider.gameObject.name == "Player") {
            raycast.collider.gameObject.GetComponent<Player>().TakeDamage(laserDamage, raycast.collider.gameObject);
        }
    }

    public bool CheckPlayerUnder() {
        Glow(false);
        Vector2 playerPos = Player.instance.transform.position;
        float leftSide = this.transform.position.x - this.transform.localScale.x / 2;
        float rightSide = this.transform.position.x + this.transform.localScale.x / 2;
        if (playerPos.x >= leftSide && playerPos.x <= rightSide) {
            return true;
        }
        return false;
    }

    public IEnumerator InitSmash(float delayTime) {
        bossFightController.SetIsSmashing(true);
        shakeSFX.Play(); 
        // Do shaking here

        Vector3 beforePos = this.transform.position;
        float step = delayTime / 50;
        float counter = 0;

        while (counter < delayTime) {
            float randX = Random.Range(-shakeIntensityX, shakeIntensityX);
            float randY = Random.Range(-shakeIntensityY, shakeIntensityY);
            this.transform.position = new Vector3(beforePos.x + randX, beforePos.y + randY, 0);
            counter += step;
            yield return new WaitForSeconds(step);
        }

        this.transform.position = beforePos;

        state = FlyingCubeState.SMASHING;
        initPos = this.transform.position;
        Smash();

        Glow(true);
    }

    private void Smash() {
        rigidbody.velocity = new Vector2(0, -smashSpeed);

        Collider2D[] collided = Physics2D.OverlapBoxAll(this.transform.position, this.transform.localScale, 0);
        foreach (Collider2D collider in collided) {
            if (collider.gameObject.tag.Equals("Player")) {
                collider.gameObject.GetComponent<Player>().TakeDamage(smashDamage, this.gameObject);
            }
            if (collider.tag.Equals("Ground")) {
                impactSFX.Play();
                VirtualCamera.instance.CameraShake(CameraEventName.BOULDER_SMASH);

                state = FlyingCubeState.RESETTING;
            }
        }

        Glow(true);
    }

    public void SetDestination(Vector3 targetDestination)
    {
        if (state == FlyingCubeState.AWAIT_DESTINATION) {
            destination = targetDestination;
            state = FlyingCubeState.MOVING;
        }
    }

    public void MoveToDestination() {
        if (elapsedDuration < lerpDuration) {
            this.transform.position = Vector3.Lerp(fromPosition, destination, elapsedDuration / lerpDuration);
            elapsedDuration += Time.deltaTime;
        }
    }

    public Vector3 GetNewLocation() {
        
        Vector3 currentPosition = this.transform.position;
        fromPosition = currentPosition;

        RaycastHit2D raycastHitUp = Physics2D.Raycast(this.transform.position, Vector2.up, maxRayCastY, groundLayerMask);
        RaycastHit2D raycastHitDown = Physics2D.Raycast(this.transform.position, Vector2.down, maxRayCastY, groundLayerMask);
        RaycastHit2D raycastHitRight = Physics2D.Raycast(this.transform.position, Vector2.right, maxRayCastX, groundLayerMask);
        RaycastHit2D raycastHitLeft = Physics2D.Raycast(this.transform.position, Vector2.left, maxRayCastX, groundLayerMask);

        float upRange;
        float downRange;
        float rightRange;
        float leftRange;

        if (raycastHitUp.collider == null) {
            upRange = maxRayCastY;
        } else {
            upRange = raycastHitUp.distance;
        }

        if (raycastHitDown.collider == null) {
            downRange = - maxRayCastY;
        } else {
            downRange = -raycastHitDown.distance;
        }

        if (raycastHitRight.collider == null) {
            rightRange = maxRayCastX;
        } else {
            rightRange = raycastHitRight.distance;
        }

        if (raycastHitLeft.collider == null) {
            leftRange = -maxRayCastX;
        } else {
            leftRange = -raycastHitLeft.distance;
        }

        float newX = Random.Range(leftRange, rightRange);
        float newY = Random.Range(downRange + minFloatHeight, upRange);
        Vector3 newPos = new Vector3(newX, newY, 0) + this.transform.position;

        return newPos;
    }

    bool ReachDestination()
    {
        if (elapsedDuration >= lerpDuration)
        {
            elapsedDuration = 0;
            return true;
        }
        return false;
    }

    void LookAtPlayer(bool input)
    {
        if (input)
        {
            Vector2 direction = lookAtObject.position - transform.position;
            float angle = Vector2.SignedAngle(face.forward, direction);

            //AimTolerance
            if (Mathf.Abs(angle) < aimingTolerance)
            {
                //Instant Lock
                return;
            }


            float turnDirection = 1;
            if (angle < 0)
                turnDirection = -1;

            rotZ += Time.deltaTime * turningSpeed * turnDirection;
            transform.rotation = Quaternion.Euler(0, 0, rotZ);
        }
    }

    public void Glow(bool input)
    {
        glowLight.SetActive(input);
    }
    public void Glow()
    {
        glowLight.SetActive(false);
    }

    public void MoveSoundControl()
    {
        float speed = rigidbody.velocity.magnitude;
        movingSFX.volume = speed / initialMoveVolume;
    }

}
