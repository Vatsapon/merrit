using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : Entity
{

    private BossFightController bossFightController;
    private Player player;
    private Animator anim;
    private SpriteRenderer sr;
    private int numHitToInterrupt;
    private int numHitToInterruptCounter;
    private int maxIter = 30;
    private float chantCounter;
    private float chantDuration;
    private float runAwaySpeed;
    private float errorTolerance = 0.01f;
    private float left;
    private float right;
    private float up;
    private float down;
    private float floatHeight;
    private float physicalMoveSpeed;
    private float minDistToRunAway;
    private bool successfullyChant;
    private bool playerWasSpiritLastFrame;

    // ======================== PHASE 2 ===========================

    [Header("Phase 2 Settings")]
    public GameObject projectile;
    private Damage projectileDamage;
    private float projectileSpeed;
    private float shootCooldown;
    private float shootCooldownCounter;
    private float phase2FloatHeightHigher;
    private float phase2FloatHeightLower;
    private float phase2HeightToWarpBack;
    private float phase2FloatSpeed;
        



    private float floatUpSpeed;
    
    private Vector3 destination;

    protected override void Start() {
        base.Start();
        bossFightController = BossFightController.instance;
        player = Player.instance;
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        chantDuration = bossFightController.chantDuration;
        numHitToInterrupt = bossFightController.numHitToInterrupt;
        numHitToInterruptCounter = numHitToInterrupt;
        runAwaySpeed = bossFightController.runAwaySpeed;
        left = bossFightController.leftDown.x + 1;
        right = bossFightController.rightUp.x - 1;
        down = bossFightController.leftDown.y + 1;
        up = bossFightController.rightUp.y - 1;
        minDistToRunAway = bossFightController.minDistToRunAway;
        floatHeight = bossFightController.bossHeightValue;
        physicalMoveSpeed = bossFightController.physicalMoveSpeed;

        projectileDamage = bossFightController.projectileDamage;
        projectileSpeed = bossFightController.projectileSpeed;
        shootCooldown = bossFightController.shootCooldown;
        shootCooldownCounter = 0;
        phase2FloatHeightHigher = bossFightController.phase2FloatHeightHigher;
        phase2FloatHeightLower = bossFightController.phase2FloatHeightLower;
        phase2FloatSpeed = bossFightController.phase2FloatSpeed;
        phase2HeightToWarpBack = bossFightController.phase2HeightToWarpBack;
        

        destination = new Vector3(-100000, -100000);
    }

    public void Update()
    {
        if (player.transform.position.x > transform.position.x) sr.flipX = true;
        else sr.flipX = false;
    }

    public void ActPhase1() {
        if (player.isSpirit) {
            RunAway();
            playerWasSpiritLastFrame = true;
        } else {
            Chant();
            MoveHorizontal();
            playerWasSpiritLastFrame = false;
        }
    }

    public void ActPhase2() {
        /*
        if (shootCooldownCounter >= shootCooldown && this.transform.position.y < down + phase2FloatHeightLower) {
            ShootProjectile();
            shootCooldownCounter = 0;
        }
        */
        if (shootCooldownCounter >= shootCooldown && transform.position.y >= phase2FloatHeightLower)
        {
            ShootProjectile();
            shootCooldownCounter = 0;
        }

        shootCooldownCounter += Time.deltaTime;
        if (player.isSpirit) {
            FloatUp();
            playerWasSpiritLastFrame = true;
        } else {
            MoveHorizontal();
            playerWasSpiritLastFrame = false;
        }
    }

    private void ShootProjectile() {
        anim.Play("PreAttack");
    }

    public void Attack()
    {
        GameObject gameObject = Instantiate(projectile, this.transform.position, Quaternion.identity);
        if (gameObject != null)
        {
            Vector2 directionalVector = Vector3.Normalize(player.transform.position - this.transform.position);
            gameObject.GetComponent<BossProjectile>().SetDirection(directionalVector);
        }
    }

    private void Chant() {
        
        if (chantCounter > chantDuration) {
            OnSuccessfulChant();
            successfullyChant = true;
        }
        chantCounter += Time.deltaTime;
        if (player.isSpirit) {
            
        } else {
            anim.Play("Chanting");
            MoveHorizontal();
        }

    }

    private void FloatUp() {
        if (!playerWasSpiritLastFrame || destination == new Vector3(-100000, -100000)) {
            playerWasSpiritLastFrame = true;
            destination = GetDestinationToFloatTo();
        }
        if (!IsAtDestination() && (destination != new Vector3(-100000, -100000))) {
            this.transform.position = Vector3.Lerp(this.transform.position, destination, phase2FloatSpeed * Time.deltaTime);
        } else {
            float randX = Random.Range(left, right);
            this.transform.position = new Vector3(randX, phase2HeightToWarpBack + down);
            destination = GetDestinationToFloatTo();
        }
    }

    private Vector3 GetDestinationToFloatTo() {
        return new Vector3(this.transform.position.x, down + phase2FloatHeightHigher, 0);
    }

    private void MoveHorizontal() {
        if (playerWasSpiritLastFrame || destination == new Vector3(-100000, -100000)) {
            float x = Random.Range(left, right);
            destination = new Vector3(x, floatHeight + down, 0);
        }
        if (!IsAtDestination()) {
            this.transform.position = Vector3.Lerp(this.transform.position, destination, physicalMoveSpeed * Time.deltaTime);
        } else {
            float x = Random.Range(left, right);
            destination = new Vector3(x, floatHeight + down, 0);
        }
    }

    private void RunAway() {
        if (!IsAtDestination() && (destination != new Vector3(-100000, -100000))) {
            this.transform.position = Vector3.Lerp(this.transform.position, destination, runAwaySpeed * Time.deltaTime);
        } else {
            RejectionSampling();
        }
    }

    private bool IsAtDestination() {
        if (Vector2.Distance(destination, this.transform.position) < errorTolerance) {
            return true;
        }
        return false;
    }

    private void RejectionSampling() {
        float maxDist = 0;
        float randX;
        float randY;
        Vector3 vector = new Vector3();
        Vector3 playerPos = player.transform.position;
        int iterCounter = 0;
        while (iterCounter < maxIter) {
            randX = Random.Range(left, right);
            randY = Random.Range(down, up);
            Vector3 tempVector = new Vector2(randX, randY);
            float tempDist = Vector2.Distance(tempVector, playerPos);
            if (tempDist > minDistToRunAway) {
                vector = tempVector;
                break;
            }
            if (tempDist > maxDist) {
                maxDist = tempDist;
                vector = tempVector;
            }
            iterCounter += 1;
        }
        iterCounter = 0;
        destination = vector;
    }

    public void TakeDamageFromShrine(Damage damage, GameObject causeObject) {
        base.TakeDamage(damage, causeObject);
        numHitToInterruptCounter -= 1;
        if (numHitToInterruptCounter <= 0) {
            numHitToInterruptCounter = numHitToInterrupt;
            chantCounter = 0;
        }
        if (bossFightController.IsMoreThanLoopBeforeMiniPhase() && bossFightController.bossState == BossFightController.BossPhase.PHASE_1) {
            bossFightController.SetShouldStartMiniPhase(true);
        }
    }

    public override void TakeDamage(Damage damage, GameObject causeObject)
    {   

        if (!player.isSpirit) {
            return;
        }

        base.TakeDamage(damage, causeObject);
        numHitToInterruptCounter -= 1;
        if (numHitToInterruptCounter <= 0) {
            numHitToInterruptCounter = numHitToInterrupt;
            chantCounter = 0;
        }
        if (bossFightController.IsMoreThanLoopBeforeMiniPhase() && bossFightController.bossState == BossFightController.BossPhase.PHASE_1) {
            bossFightController.SetShouldStartMiniPhase(true);
        }

    }

    protected override void Die()
    {
        print("DIEEDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
        UIManager.instance.gameCanvas.GetComponent<Animator>().Play("Credit");
        base.Die();
    }

    public void ActMiniPhase()
    {

    }


    private void OnSuccessfulChant()
    {

    }

    public bool HasSuccessfullyChanted() {
        return successfullyChant;
    }

}
