using UnityEngine;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFormRoot formRoot;
    [SerializeField] private PlayerGroundSensor groundSensor;
    [SerializeField] private PlayerRuleController ruleController;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private LevelHazardController hazardController;

    [Header("Move Settings")]
    [SerializeField] private float humanMoveSpeed = 4f;
    [SerializeField] private float carMoveSpeed = 7f;
    [SerializeField] private float planeMoveSpeed = 6f;
    [SerializeField] private float planeVerticalSpeed = 5f;
    [SerializeField] private float boatMoveSpeed = 4.5f;
    [SerializeField] private float boatFloatHeightOffset = 0.85f;
    [SerializeField] private float boatFloatVerticalSpeed = 8f;
    [SerializeField] private float boatFloatSnapDeadZone = 0.08f;
    [SerializeField] private float boatFloatActivationMargin = 0.35f;
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float humanJumpForce = 9f;
    [SerializeField] private float humanGravityScale = 3f;
    [SerializeField] private float carGravityScale = 4f;
    [SerializeField] private float planeGravityScale = 0f;
    [SerializeField] private float boatGravityScale = 3f;
    [SerializeField] private float jumpBufferTime = GameConstants.DefaultJumpBufferTime;
    [SerializeField] private float coyoteTime = GameConstants.DefaultCoyoteTime;
    [SerializeField] private int maxHumanJumpCount = 2;
    [SerializeField] private float groundedVelocityThreshold = 0.1f;

    private float horizontalInput;
    private float verticalInput;
    private bool sprintHeld;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private int humanJumpCount;
    private bool boatFloatSimulationActive;

    private void Reset()
    {
        formRoot = GetComponent<PlayerFormRoot>();
        groundSensor = GetComponent<PlayerGroundSensor>();
        ruleController = GetComponent<PlayerRuleController>();
        inputReader = GetComponent<PlayerInputReader>();
        hazardController = FindObjectOfType<LevelHazardController>();
    }

    private void Awake()
    {
        if (formRoot == null)
        {
            formRoot = GetComponent<PlayerFormRoot>();
        }

        if (inputReader == null)
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        if (hazardController == null)
        {
            hazardController = LevelHazardController.GetOrCreateInstance();
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
        if (inputReader == null)
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            sprintHeld = false;
            return;
        }

        horizontalInput = inputReader.HorizontalInput;
        verticalInput = inputReader.VerticalInput;
        sprintHeld = inputReader.SprintHeld;
        if (inputReader.JumpPressedThisFrame)
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    private void UpdateGroundTimers()
    {
        bool grounded = IsStableGrounded();
        if (grounded)
        {
            coyoteTimeCounter = coyoteTime;
            humanJumpCount = 0;
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

        bool shouldUseBoatFloatMode = ShouldUseBoatFloatMode();
        UpdateBoatFloatSimulationState(shouldUseBoatFloatMode);

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
                formRoot.PlayerRigidbody.gravityScale = shouldUseBoatFloatMode ? 0f : boatGravityScale;
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
        bool isRunning = false;
        bool shouldUseBoatFloatMode = ShouldUseBoatFloatMode();

        switch (formRoot.CurrentForm)
        {
            case PlayerFormType.Human:
                float humanZoneMultiplier = ruleController != null ? ruleController.HumanSpeedMultiplier : 1f;
                velocity.x = horizontalInput * humanMoveSpeed * speedMultiplier * humanZoneMultiplier;
                bool isGrounded = IsStableGrounded();
                bool canUseGroundJump = jumpBufferCounter > 0f && coyoteTimeCounter > 0f;
                bool canUseAirJump = jumpBufferCounter > 0f && !isGrounded && humanJumpCount > 0 && humanJumpCount < maxHumanJumpCount;
                if (canUseGroundJump || canUseAirJump)
                {
                    velocity.y = 0f;
                    rb.velocity = velocity;
                    rb.AddForce(Vector2.up * humanJumpForce, ForceMode2D.Impulse);
                    humanJumpCount = Mathf.Min(maxHumanJumpCount, humanJumpCount + 1);
                    jumpBufferCounter = 0f;
                    coyoteTimeCounter = 0f;
                }
                else
                {
                    rb.velocity = velocity;
                }
                isRunning = Mathf.Abs(horizontalInput) > 0.01f && isGrounded;
                break;

            case PlayerFormType.Car:
                velocity.x = horizontalInput * carMoveSpeed * speedMultiplier;
                rb.velocity = velocity;
                isRunning = Mathf.Abs(horizontalInput) > 0.01f;
                break;

            case PlayerFormType.Plane:
                velocity.x = horizontalInput * planeMoveSpeed * speedMultiplier;
                velocity.y = verticalInput * planeVerticalSpeed * speedMultiplier;
                rb.velocity = velocity;
                isRunning = Mathf.Abs(horizontalInput) > 0.01f || Mathf.Abs(verticalInput) > 0.01f;
                break;

            case PlayerFormType.Boat:
                float boatSpeed = boatMoveSpeed;
                if (ruleController != null && ruleController.IsInBlizzard())
                {
                    boatSpeed = humanMoveSpeed * ruleController.BlizzardSlowMultiplier;
                }

                if (shouldUseBoatFloatMode)
                {
                    ApplyBoatFloatTransformMovement(boatSpeed * speedMultiplier);
                }
                else
                {
                    velocity.x = horizontalInput * boatSpeed * speedMultiplier;
                    velocity.y = 0f;
                    rb.velocity = velocity;
                }

                isRunning = false;
                break;
        }

        formRoot.SetFacingFromHorizontal(horizontalInput);
        formRoot.SetRunState(isRunning);
    }

    private bool IsStableGrounded()
    {
        if (groundSensor == null)
        {
            return false;
        }

        if (!groundSensor.IsGrounded)
        {
            return false;
        }

        Rigidbody2D rb = formRoot != null ? formRoot.PlayerRigidbody : null;
        if (rb == null)
        {
            return true;
        }

        return Mathf.Abs(rb.velocity.y) <= groundedVelocityThreshold;
    }

    private void ApplyBoatFloatTransformMovement(float horizontalSpeed)
    {
        if (formRoot == null || hazardController == null)
        {
            return;
        }

        if (!hazardController.TryGetGlobalWaterSurfaceY(out float waterSurfaceY))
        {
            return;
        }

        Rigidbody2D rb = formRoot.PlayerRigidbody;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        Vector3 position = transform.position;
        position.x += horizontalInput * horizontalSpeed * Time.fixedDeltaTime;
        float targetY = waterSurfaceY + boatFloatHeightOffset;
        position.y = Mathf.Abs(targetY - position.y) <= boatFloatSnapDeadZone
            ? targetY
            : Mathf.MoveTowards(position.y, targetY, boatFloatVerticalSpeed * Time.fixedDeltaTime);
        transform.position = position;
    }

    private bool ShouldUseBoatFloatMode()
    {
        if (formRoot == null || formRoot.CurrentForm != PlayerFormType.Boat || hazardController == null)
        {
            return false;
        }

        if (!hazardController.TryGetGlobalWaterSurfaceY(out float waterSurfaceY))
        {
            return false;
        }

        float maxSupportedY = waterSurfaceY + boatFloatHeightOffset + boatFloatActivationMargin;
        return transform.position.y <= maxSupportedY;
    }

    private void UpdateBoatFloatSimulationState(bool shouldUseBoatFloatMode)
    {
        if (formRoot == null || formRoot.PlayerRigidbody == null)
        {
            return;
        }

        if (boatFloatSimulationActive == shouldUseBoatFloatMode)
        {
            return;
        }

        boatFloatSimulationActive = shouldUseBoatFloatMode;
        formRoot.PlayerRigidbody.simulated = !shouldUseBoatFloatMode;
        if (!shouldUseBoatFloatMode)
        {
            formRoot.PlayerRigidbody.velocity = Vector2.zero;
        }
    }
}
