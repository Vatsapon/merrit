using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using static Entity;

public class SpiritualFormController : MonoBehaviour
{
    [Header("Spiritual Setting")]
    [SerializeField] private float moveSpeed = 5f; // Movement speed during Spiritual form.
    [SerializeField] private float maxMoveSpeed = 10f; // Maximum speed.
    [SerializeField]
    [Range(0f, 1f)]
    private float linearDrag; // Linear Drag

    [Header("References")]
    public Rigidbody2D rigidBody; // The selected Rigidbody for this form.
    public BoxCollider2D colliders; // Spiritual form's Collider.
    public Animator animator; // Animator for Physical form.
    [SerializeField] private GameObject SpiritualBulletPrefab; // Bullet prefab.

    private Player player;
    private GameManager gameManager;

    private Vector2 inputDir; // Input direction

    private void Start()
    {
        player = Player.instance;
        gameManager = GameManager.instance;

        moveSpeed = player.GetBaseStat("Spiritual Move Speed");
    }

    private void Update()
    {
        if (player.spiritualFormCombat.GetIsDashing() || gameManager.IsUI())
        {
            Cursor.SetCursor(Util.instance.prefabs.normalCursor, Vector2.zero, CursorMode.Auto);
            return;
        }

        // Check if player is using Spiritual Resonance.
        if (player.spiritualFormCombat.GetIsHoldingSpiritualResonance())
        {
            return;
        }

        inputDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // Flip sprite when move toward opposite direction.
        if (inputDir.x != 0f)
        {
            player.spriteRenderer.flipX = inputDir.x < 0f;
        }

        animator.SetFloat("MoveX", inputDir.x);
        animator.SetFloat("MoveY", inputDir.y);

        if (player.entityState == EntityState.DEAD)
        {
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
            rigidBody.velocity = Vector2.zero;
            return;
        }
    }

    private void FixedUpdate()
    {

        if (player.entityState == EntityState.DEAD || player.HasEffect("Stun"))
        {
            player.rigidBody.velocity = Vector2.zero;
            return;
        }

        if (Player.instance.spiritualFormCombat.GetIsDashing()) {
            return;
        }

        if (IsMoving())
        {
            rigidBody.AddForce(inputDir * moveSpeed, ForceMode2D.Force);

            if (rigidBody.velocity.magnitude > maxMoveSpeed)
            {
                rigidBody.velocity = Vector2.ClampMagnitude(rigidBody.velocity, maxMoveSpeed);
            }
        } 

        float tempXVelocity = rigidBody.velocity.x * (1 - linearDrag); // for linear drag
        float tempYVelocity = rigidBody.velocity.y * (1 - linearDrag);
        rigidBody.velocity = new Vector2(tempXVelocity, tempYVelocity);
    }

    /// <summary>
    ///     When the player is moving the sprite rotate its head towards the direction not upwards,
    ///     but when the player stops moving the sprite head points upwards
    /// </summary>
    ///

    public bool IsMoving()
    {
        return inputDir != Vector2.zero;
    }

    public void OnEffectWornOut()
    {
        animator.Play("Anim_Spiritual_GorgoEgg_Unstun");
    }
}
