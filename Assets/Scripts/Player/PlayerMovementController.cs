using UnityEngine;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFormRoot formRoot;
    [SerializeField] private PlayerGroundSensor groundSensor;
    [SerializeField] private PlayerRuleController ruleController;

    [Header("Move Settings")]
    [SerializeField] private float humanMoveSpeed = 4f;
    [SerializeField] private float carMoveSpeed = 7f;
    [SerializeField] private float planeMoveSpeed = 6f;
    [SerializeField] private float planeVerticalSpeed = 5f;
    [SerializeField] private float boatMoveSpeed = 4.5f;
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float humanJumpForce = 9f;
    [SerializeField] private float humanGravityScale = 3f;
    [SerializeField] private float carGravityScale = 4f;
    [SerializeField] private float planeGravityScale = 0f;
    [SerializeField] private float boatGravityScale = 3f;
    [SerializeField] private float jumpBufferTime = GameConstants.DefaultJumpBufferTime;
    [SerializeField] private float coyoteTime = GameConstants.DefaultCoyoteTime;

    private float horizontalInput;
    private float verticalInput;
    private bool sprintHeld;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;

    private void Reset()
    {
        formRoot = GetComponent<PlayerFormRoot>();
        groundSensor = GetComponent<PlayerGroundSensor>();
        ruleController = GetComponent<PlayerRuleController>();
    }

    private void Awake()
    {
        if (formRoot == null)
        {
            formRoot = GetComponent<PlayerFormRoot>();
        }
    }

    private void Update()
    {
        ReadInput();
        UpdateGroundTimers();
        ApplyGravityPreset();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void ReadInput()
    {
        horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontalInput -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontalInput += 1f;
        }

        verticalInput = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            verticalInput += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            verticalInput -= 1f;
        }

        sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    private void UpdateGroundTimers()
    {
        bool grounded = groundSensor != null && groundSensor.IsGrounded;
        if (grounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void ApplyGravityPreset()
    {
        if (formRoot == null || formRoot.PlayerRigidbody == null)
        {
            return;
        }

        switch (formRoot.CurrentForm)
        {
            case PlayerFormType.Human:
                formRoot.PlayerRigidbody.gravityScale = humanGravityScale;
                break;
            case PlayerFormType.Car:
                formRoot.PlayerRigidbody.gravityScale = carGravityScale;
                break;
            case PlayerFormType.Plane:
                formRoot.PlayerRigidbody.gravityScale = planeGravityScale;
                break;
            case PlayerFormType.Boat:
                formRoot.PlayerRigidbody.gravityScale = boatGravityScale;
                break;
        }
    }

    private void ApplyMovement()
    {
        if (formRoot == null || formRoot.PlayerRigidbody == null)
        {
            return;
        }

        Rigidbody2D rb = formRoot.PlayerRigidbody;
        Vector2 velocity = rb.velocity;
        float speedMultiplier = sprintHeld ? sprintMultiplier : 1f;

        switch (formRoot.CurrentForm)
        {
            case PlayerFormType.Human:
                float humanZoneMultiplier = ruleController != null ? ruleController.HumanSpeedMultiplier : 1f;
                velocity.x = horizontalInput * humanMoveSpeed * speedMultiplier * humanZoneMultiplier;
                if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
                {
                    velocity.y = 0f;
                    rb.velocity = velocity;
                    rb.AddForce(Vector2.up * humanJumpForce, ForceMode2D.Impulse);
                    jumpBufferCounter = 0f;
                    coyoteTimeCounter = 0f;
                }
                else
                {
                    rb.velocity = velocity;
                }
                break;

            case PlayerFormType.Car:
                velocity.x = horizontalInput * carMoveSpeed * speedMultiplier;
                rb.velocity = velocity;
                break;

            case PlayerFormType.Plane:
                velocity.x = horizontalInput * planeMoveSpeed * speedMultiplier;
                velocity.y = verticalInput * planeVerticalSpeed * speedMultiplier;
                rb.velocity = velocity;
                break;

            case PlayerFormType.Boat:
                velocity.x = horizontalInput * boatMoveSpeed * speedMultiplier;
                rb.velocity = velocity;
                break;
        }

        formRoot.SetFacingFromHorizontal(horizontalInput);
    }
}
