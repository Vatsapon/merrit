using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.ParticleSystem;

public class BossFightController : MonoBehaviour
{   

    public enum BossPhase {
        PRE_START, PHASE_1, MINI_PHASE, PHASE_2, TRANSITION
    }

    public static BossFightController instance;

    [Header("Room Boundary")]
    public Vector2 leftDown;
    public Vector2 rightUp;

    
    [Header("References")]
    public Boss boss;
    public Animator bossAnim;
    public GameObject canvas;

    public BossShrine bossShrine;
    public List<FlyingCube> flyingCubes = new List<FlyingCube>();
    public Slider hpBar;
    public BossSoundManager soundManager;

    [Header("No touching")]
    public List<FlyingCube> side1Cubes = new List<FlyingCube>();
    public List<FlyingCube> side2Cubes = new List<FlyingCube>(); 

    
    [Header("Flying Cube Settings")]
    public float xRange;
    public float yRange;
    public float laserFrequency;
    public float minFloatHeight;
    public float stopAimSeconds = 0.5f;
    public Damage laserDamage;
    private int flyingCubesCount;
    
    [Header("Phase 1 Settings")]
    public int numLoopBeforeMiniPhase = 5;
    public int numHitToInterrupt = 1;
    public int shrinePopOutAfterNumHits = 3;
    public float shrinePopOutSeconds = 3;
    public float chantDuration;
    public float minDistToRunAway = 7;
    public float runAwaySpeed = 5;
    public float bossHeightValue;
    public float physicalMoveSpeed;

    public float shootPlayerAssistTime = 0;
    private int shrinePopOutCounter;
    private int laserAttackLoopCounter;
    private bool shouldStartMiniPhase = false;
    private bool successfullyChant = false;


    [Header("Mini Phase Settings")]
    public float bossFlyToCenterTime = 2;
    public float changePhaseWaitTime = 1;
    public float miniPhaseDuration = 5f;
    public int baseSpawnNum = 3;
    public int uninterruptedChantSpawnNum = 1;
    public float spacing;
    public float transitionTime;
    public float deflateTime;
    public float delayTimeBeforeSmash;
    public Damage smashDamage;
    public float flyingCubeSmashSpeed;
    public float flyingCubeResetSpeed;
    public float playerRestTime;
    public bool shouldDestroyEnemyOnEndPhase = true;
    public List<GameObject> enemies = new List<GameObject>();
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private float miniPhaseCounter = 0;
    private bool isSmashing;
    private Vector3 initSize;

    [Header("Phase 2 Boss Settings")]

    public GameObject manaPrefab;

    public int shrinePopOutAfterNumHitsPhase2 = 3;
    public float shrinePopOutSecondsPhase2 = 3;
    private int shrinePopOutCounterPhase2;

    public int numManaToSpawn;
    public float spawnOverSeconds;
    public float bossHealthRequired;
    public float TransitionToPhase2Time;
    public Damage projectileDamage;
    public float projectileSpeed;
    public float shootCooldown;
    public float phase2FloatHeightHigher;
    public float phase2FloatHeightLower;
    public float phase2HeightToWarpBack;
    public float phase2FloatSpeed;
    
    [Header("Phase 2 Flying Cube")]

    public float phase2TransitionTime;

    [Header("Phase 2 Horizontal Sweep")]

    public float horizontalSweepHeight;
    public float horiDistFromWall;
    public float horiSweepDist;
    public float horizontalSweepTime;

    [Header("Phase 2 Vertical Sweep")]

    public float distFromLeftWall;
    public float verticalShootTime;
    public float verticalSweepLockTime;
    public float veritcalMoveTime;
    public int numVerticalSweep;
    public float verticalSpacing;
    public float verticalFromGroundOffset;

    private GameObject currentPortalParticle; // current Boss portal particle.

    public bool hasFinishedTransition;
    private bool isSweepingHorizontal;

    private float cooldownCounter;

    private float left;
    private float right;
    private float up;
    private float down;

    public BossPhase bossState;

    public bool activateBossOnStart = false;

    // Start is called before the first frame update
    private void Awake() 
    {
        laserAttackLoopCounter = 0;
        instance = this;
        flyingCubesCount = flyingCubes.Count;
        bossState = BossPhase.PRE_START;
        hasFinishedTransition = false;

        // Need to be commented out
    }

    void Start() {
        left = leftDown.x + 1;
        right = rightUp.x - 1;
        down = leftDown.y + 1;
        up = rightUp.y - 1;

        soundManager = GetComponent<BossSoundManager>();

        if (activateBossOnStart) ActivateBoss();
        VirtualCamera.instance.isInBossPhase = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (bossState.Equals(BossPhase.PRE_START) && !activateBossOnStart)
        {
            Vector3 playerPos = Player.instance.transform.position;
            if (playerPos.x <= right && playerPos.x >= left && playerPos.y >= down && playerPos.y <= up)
            {
                ActivateBoss();
            }
            else return;
        }
        
        hpBar.value = boss.GetStat("Health") / boss.GetBaseStat("Health");

        if (bossState == BossPhase.PRE_START) {
            return;
        }

        if (bossState == BossPhase.TRANSITION) {
            return;
        }

        if (bossState == BossPhase.PHASE_1) {
            boss.ActPhase1();
            foreach (FlyingCube flyingCube in flyingCubes) {
                if (flyingCube.state == FlyingCube.FlyingCubeState.AWAIT_DESTINATION) {
                    Vector3 newPos = flyingCube.GetNewLocation();
                    flyingCube.SetDestination(newPos);
                    laserAttackLoopCounter += 1;
                }
            }

        }

        if (bossState == BossPhase.MINI_PHASE) {

            // Spawn portal particle.
            if (!currentPortalParticle)
            {
                currentPortalParticle = Instantiate(Util.instance.particles.bossPortalEffect, boss.transform.position, Quaternion.identity);
            }


            laserAttackLoopCounter = 0;
            if (miniPhaseCounter > miniPhaseDuration || spawnedEnemies.Count == 0) {
                if (bossState == BossPhase.TRANSITION) {
                    return;
                }
                if (isSmashing) {
                    return;
                }
                bossState = BossPhase.TRANSITION;
                StartCoroutine(TransitionOutOfMiniPhase());
                return;
            }
            miniPhaseCounter += Time.deltaTime;
            
            if (!isSmashing) {
                CheckForSmash();
            }

            return;
        }

        if (bossState == BossPhase.PHASE_2) {
            boss.ActPhase2();
            if (hasFinishedTransition) {
                if (side1Cubes[0].state == FlyingCube.FlyingCubeState.MOVING_HORI) {
                    SweepHorizontal();
                } else if (side1Cubes[0].state == FlyingCube.FlyingCubeState.MOVING_VERTI) {
                    StartCoroutine(SweepVertical());
                } else if (side1Cubes[0].state == FlyingCube.FlyingCubeState.FINISHED_HORI_SWEEP) {
                    InitSweepVertical();
                } else if (side1Cubes[0].state == FlyingCube.FlyingCubeState.FINISHED_VERTI_SWEEP) {
                    InitSweepHorizontal();
                }
            }
            return;
        }

        updatePhase();

    }


    private IEnumerator SpawnMana() {
        float counter = 0;
        float step = spawnOverSeconds / numManaToSpawn;
        Debug.Log("Called, step: " + step + ", ");

        while (counter < spawnOverSeconds) {
            float randX = Random.Range(left, right);
            Vector3 vec = new Vector3(randX, up, 0);
            Instantiate(manaPrefab, vec, Quaternion.identity);
            counter += step;
            yield return new WaitForSeconds(step);
        }
    }

    // Assume one thread, else
    // TO CHECK: Data Race condition

    private void InitSweepVertical() {
        hasFinishedTransition = false;
        float randY = Random.Range(0, up - down - 1.5f);

        StartCoroutine(side1Cubes[0].TransitionToVerticalSweepState(new Vector2(left + distFromLeftWall, down + randY)));
        StartCoroutine(side1Cubes[1].TransitionToVerticalSweepState(new Vector2(left + distFromLeftWall, down + randY + 1.5f)));
        StartCoroutine(side2Cubes[0].TransitionToVerticalSweepState(new Vector2(right - distFromLeftWall, down + randY)));
        StartCoroutine(side2Cubes[1].TransitionToVerticalSweepState(new Vector2(right - distFromLeftWall, down + randY + 1.5f)));

    }

    private IEnumerator SweepVertical() {
        hasFinishedTransition = false;

        int i = 0;

        while(i < numVerticalSweep) {
            int randY1 = Random.Range(0, 5);
            int randY2 = GetRandomY(0, 5, randY1);

            float yPos1 = verticalFromGroundOffset + randY1 * verticalSpacing;
            float yPos2 = verticalFromGroundOffset + randY2 * verticalSpacing;

            StartCoroutine(side1Cubes[0].MoveVerticalSweep(new Vector2(left + distFromLeftWall, down + yPos1)));
            StartCoroutine(side1Cubes[1].MoveVerticalSweep(new Vector2(left + distFromLeftWall, down + yPos2)));
            StartCoroutine(side2Cubes[0].MoveVerticalSweep(new Vector2(right - distFromLeftWall, down + yPos1)));
            StartCoroutine(side2Cubes[1].MoveVerticalSweep(new Vector2(right - distFromLeftWall, down + yPos2)));
            i++;
            yield return new WaitForSeconds(veritcalMoveTime + verticalShootTime + verticalSweepLockTime + 1f);
        }

        hasFinishedTransition = true;
        
        side1Cubes[0].state = FlyingCube.FlyingCubeState.FINISHED_VERTI_SWEEP;
        side1Cubes[1].state = FlyingCube.FlyingCubeState.FINISHED_VERTI_SWEEP;
        side2Cubes[0].state = FlyingCube.FlyingCubeState.FINISHED_VERTI_SWEEP;
        side2Cubes[1].state = FlyingCube.FlyingCubeState.FINISHED_VERTI_SWEEP;

    }


    private int GetRandomY(int min, int max, int not) {
        int y = Random.Range(min, max);
        if (y == not) {
            return GetRandomY(min, max, not);
        }
        return y;
    }

    private void InitSweepHorizontal() {
        hasFinishedTransition = false;
        StartCoroutine(side1Cubes[0].TransitionToHorizontalSweepState(new Vector2(left + horiDistFromWall, horizontalSweepHeight + down)));
        StartCoroutine(side1Cubes[1].TransitionToHorizontalSweepState(new Vector2(left + horiDistFromWall + 1.5f, horizontalSweepHeight + down)));
        StartCoroutine(side2Cubes[0].TransitionToHorizontalSweepState(new Vector2(right - horiDistFromWall, horizontalSweepHeight + down)));
        StartCoroutine(side2Cubes[1].TransitionToHorizontalSweepState(new Vector2(right - horiDistFromWall - 1.5f, horizontalSweepHeight + down)));
    }

    private void SweepHorizontal() {

        StartCoroutine(SpawnMana());

        hasFinishedTransition = false;
        side1Cubes[0].transform.eulerAngles = new Vector3(0, 0, 0);
        side1Cubes[1].transform.eulerAngles = new Vector3(0, 0, 0);
        side2Cubes[0].transform.eulerAngles = new Vector3(0, 0, 0);
        side2Cubes[1].transform.eulerAngles = new Vector3(0, 0, 0);
        StartCoroutine(side1Cubes[0].SweepHorizontal(new Vector2(left + horiSweepDist, horizontalSweepHeight + down)));
        StartCoroutine(side1Cubes[1].SweepHorizontal(new Vector2(left + horiSweepDist + 1.5f, horizontalSweepHeight + down)));
        StartCoroutine(side2Cubes[0].SweepHorizontal(new Vector2(right - horiSweepDist, horizontalSweepHeight + down)));
        StartCoroutine(side2Cubes[1].SweepHorizontal(new Vector2(right - horiSweepDist - 1.5f, horizontalSweepHeight + down)));
    }

    private IEnumerator TransitionOutOfMiniPhase() {
        
        if (currentPortalParticle)
        {
            ParticleSystem portalParticle = currentPortalParticle.GetComponent<ParticleSystem>();
            MainModule mainModule = portalParticle.main;
            mainModule.loop = false;

            for (int i = 0; i < portalParticle.transform.childCount; i++)
            {
                mainModule = portalParticle.transform.GetChild(i).GetComponent<ParticleSystem>().main;
                mainModule.loop = false;
            }
        }

        if (shouldDestroyEnemyOnEndPhase) {
            DespawnMinions();
        }

        yield return new WaitForSeconds(playerRestTime);

        updatePhase();
        miniPhaseCounter = 0;
        isSmashing = false;
        
        StartCoroutine(DeflateCube());

    }

    private void CheckForSmash() {

        FlyingCube tempCube = null;
        
        foreach (FlyingCube flyingCube in flyingCubes) {
            if (flyingCube.CheckPlayerUnder()) {
                tempCube = flyingCube;
                break;
            }
        }

        if (tempCube == null) {
            return;
        }

        StartCoroutine(tempCube.InitSmash(delayTimeBeforeSmash));

    }

    private IEnumerator DeflateCube() {
        bossShrine.gameObject.SetActive(true);
        shouldStartMiniPhase = false;

        Vector3 nowSize = flyingCubes[0].transform.localScale;
        Vector3 toInit = initSize;

        foreach (FlyingCube flyingCube in flyingCubes) {
            flyingCube.state = FlyingCube.FlyingCubeState.AWAIT_DESTINATION;
        }
        
        float counter = 0;
        float step = deflateTime / 100;

        while (counter <= deflateTime) {
            float t = counter / deflateTime;
            foreach (FlyingCube flyingCube in flyingCubes) {
                flyingCube.transform.localScale = Vector3.Lerp(nowSize, toInit, t);
            }
            counter += step;
            yield return new WaitForSeconds(step);
        }

    }

    private IEnumerator InitMiniPhase() {

        bossState = BossPhase.TRANSITION;
        bossShrine.gameObject.SetActive(false);

        Vector3 center = new Vector3((rightUp.x + leftDown.x) / 2, (rightUp.y + leftDown.y) / 2, 0);
        Vector3 bossInitPos = boss.transform.position;

        float step = bossFlyToCenterTime / 50;
        float transitionCounter = 0;
        
        while (transitionCounter < bossFlyToCenterTime) {
            boss.transform.position = Vector3.Lerp(bossInitPos, center, transitionCounter / bossFlyToCenterTime);
            transitionCounter += step;
            yield return new WaitForSeconds(step);
        }

        yield return new WaitForSeconds(changePhaseWaitTime);

        float totalHorizontalDist = rightUp.x - leftDown.x;
        float boxWidth = (totalHorizontalDist - 5 * spacing) / 4;
        float halfWidth = boxWidth / 2;
        float left = leftDown.x;
        float up = rightUp.y - halfWidth - 1;

        float flying1Dest = left + spacing + halfWidth;
        float flying2Dest = left + 2 * spacing + halfWidth + boxWidth;
        float flying3Dest = left + 3 * spacing + halfWidth + 2 * boxWidth;
        float flying4Dest = left + 4 * spacing + halfWidth + 3 * boxWidth;

        Vector3 flying1Init = flyingCubes[0].transform.position;
        Vector3 flying2Init = flyingCubes[1].transform.position;
        Vector3 flying3Init = flyingCubes[2].transform.position;
        Vector3 flying4Init = flyingCubes[3].transform.position;

        Vector3 flying1Size = flyingCubes[0].transform.localScale;
        Vector3 flying2Size = flyingCubes[1].transform.localScale;
        Vector3 flying3Size = flyingCubes[2].transform.localScale;
        Vector3 flying4Size = flyingCubes[3].transform.localScale;

        initSize = flying1Size;

        Vector3 flying1To = new Vector3(flying1Dest, up, 0);
        Vector3 flying2To = new Vector3(flying2Dest, up, 0);
        Vector3 flying3To = new Vector3(flying3Dest, up, 0);
        Vector3 flying4To = new Vector3(flying4Dest, up, 0);

        Vector3 flying1SizeTo = flyingCubes[0].transform.localScale * boxWidth;
        Vector3 flying2SizeTo = flyingCubes[1].transform.localScale * boxWidth;
        Vector3 flying3SizeTo = flyingCubes[2].transform.localScale * boxWidth;
        Vector3 flying4SizeTo = flyingCubes[3].transform.localScale * boxWidth;

        Vector3 flying1Angle = flyingCubes[0].transform.localEulerAngles;
        Vector3 flying2Angle = flyingCubes[1].transform.localEulerAngles;
        Vector3 flying3Angle = flyingCubes[2].transform.localEulerAngles;
        Vector3 flying4Angle = flyingCubes[3].transform.localEulerAngles;

        FlyingCube flying1 = flyingCubes[0];
        FlyingCube flying2 = flyingCubes[1];
        FlyingCube flying3 = flyingCubes[2];
        FlyingCube flying4 = flyingCubes[3];

        flying1.shouldLookAtPlayer = false;
        flying2.shouldLookAtPlayer = false;
        flying3.shouldLookAtPlayer = false;
        flying4.shouldLookAtPlayer = false;

        flying1.state = FlyingCube.FlyingCubeState.INFLATED;
        flying2.state = FlyingCube.FlyingCubeState.INFLATED;
        flying3.state = FlyingCube.FlyingCubeState.INFLATED;
        flying4.state = FlyingCube.FlyingCubeState.INFLATED;

        transitionCounter = 0;
        step = transitionTime / 100;


        while (transitionCounter <= transitionTime) {
            float t = transitionCounter / transitionTime;

            flying1.transform.position = Vector3.Lerp(flying1Init, flying1To, t);
            flying2.transform.position = Vector3.Lerp(flying2Init, flying2To, t);
            flying3.transform.position = Vector3.Lerp(flying3Init, flying3To, t);
            flying4.transform.position = Vector3.Lerp(flying4Init, flying4To, t);

            flying1.transform.localScale = Vector3.Lerp(flying1Size, flying1SizeTo, t);
            flying2.transform.localScale = Vector3.Lerp(flying2Size, flying2SizeTo, t);
            flying3.transform.localScale = Vector3.Lerp(flying3Size, flying3SizeTo, t);
            flying4.transform.localScale = Vector3.Lerp(flying4Size, flying4SizeTo, t);

            flying1.transform.eulerAngles = Vector3.Lerp(flying1Angle, Vector3.zero, t);
            flying2.transform.eulerAngles = Vector3.Lerp(flying2Angle, Vector3.zero, t);
            flying3.transform.eulerAngles = Vector3.Lerp(flying3Angle, Vector3.zero, t);
            flying4.transform.eulerAngles = Vector3.Lerp(flying4Angle, Vector3.zero, t);

            transitionCounter += step;
            yield return new WaitForSecondsRealtime(step);
        }

        miniPhaseCounter = 0;
        bossState = BossPhase.MINI_PHASE;
        SpawnMinions();
    }

    private void SpawnMinions() {
        int totalToSpawn = baseSpawnNum;

        if (boss.HasSuccessfullyChanted()) {
            totalToSpawn += uninterruptedChantSpawnNum;
        }

        for (int i = 0; i < totalToSpawn; i++) {
            GameObject spawned = Instantiate(enemies[Random.Range(0, enemies.Count)], this.transform.position, Quaternion.identity);
            spawnedEnemies.Add(spawned);
        }

    }

    public void ShrinePopOut() {
        StartCoroutine(ShrinePopOutHelper());
    }
    
    public IEnumerator ShrinePopOutHelper() {
        bossShrine.gameObject.SetActive(false);
        
        if (bossState == BossPhase.PHASE_1) {
            yield return new WaitForSeconds(shrinePopOutSeconds);
        } else{
            yield return new WaitForSeconds(shrinePopOutSecondsPhase2);
        }

        if (bossState != BossPhase.MINI_PHASE && !shouldStartMiniPhase) {
            bossShrine.gameObject.SetActive(true);
        }
    }

    private void DespawnMinions() {
        foreach (GameObject enemy in spawnedEnemies) {
            Destroy(enemy);
        }
        spawnedEnemies.Clear();
    }

    private void updatePhase() {
        if (boss.GetStat("Health") / boss.GetBaseStat("Health") >= bossHealthRequired) {
            if (!bossState.Equals(BossPhase.PHASE_1)) soundManager.transitionToPHASE1.PlayFeedbacks();
            bossState = BossPhase.PHASE_1;
        }
        else {
            if (bossState != BossPhase.PHASE_2) {
                bossState = BossPhase.TRANSITION;
                soundManager.transitionToPHASE2.PlayFeedbacks();
                StartCoroutine(TransitionToPhase2());
                return;
            }
            bossState = BossPhase.PHASE_2;
        }
    }

    private IEnumerator TransitionToPhase2() {
        bossShrine.ResetPopOutCounter();
        yield return new WaitForSecondsRealtime(TransitionToPhase2Time);
        bossState = BossPhase.PHASE_2;
        InitSweepHorizontal();
    }

    public void ActivateBoss() {
        soundManager.start.PlayFeedbacks();
        canvas.SetActive(true);
        bossState = BossPhase.PHASE_1;
        Debug.Log("true");
    }

    public bool IsMoreThanLoopBeforeMiniPhase() {
        return (((float) laserAttackLoopCounter) / flyingCubesCount) > numLoopBeforeMiniPhase;
    }

    public void SetShouldStartMiniPhase(bool boo) {
        if (shouldStartMiniPhase || bossState == BossPhase.PHASE_2) {
            return;
        }
        shouldStartMiniPhase = boo;
        StartCoroutine(InitMiniPhase());
    }

    public void UpdateBoundary(Vector2 leftDown, Vector2 rightUp) {
        this.leftDown = leftDown;
        this.rightUp = rightUp;
    }

    public void SetIsSmashing(bool boo) {
        isSmashing = boo;
    }

}
