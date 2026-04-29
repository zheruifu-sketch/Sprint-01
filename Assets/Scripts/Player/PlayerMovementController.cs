using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("References")]
    [LabelText("玩家运行时上下文")]
    [SerializeField] private PlayerRuntimeContext runtimeContext;
    [LabelText("玩家形态根节点")]
    [SerializeField] private PlayerFormRoot formRoot;
    [LabelText("地面检测器")]
    [SerializeField] private PlayerGroundSensor groundSensor;
    [LabelText("环境规则控制器")]
    [SerializeField] private PlayerRuleController ruleController;
    [LabelText("输入读取器")]
    [SerializeField] private PlayerInputReader inputReader;
    [LabelText("关卡灾害控制器")]
    [SerializeField] private LevelHazardController hazardController;
    [LabelText("玩家调参配置")]
    [SerializeField] private PlayerTuningConfig tuningConfig;

    private float horizontalInput;
    private float verticalInput;
    private bool sprintHeld;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private int humanJumpCount;
    private bool boatFloatSimulationActive;

    private void Reset()
    {
        runtimeContext = GetComponent<PlayerRuntimeContext>();
        SyncFromContext();
        hazardController = FindObjectOfType<LevelHazardController>();
        tuningConfig = PlayerTuningConfig.Load();
    }

    private void Awake()
    {
        runtimeContext = runtimeContext != null ? runtimeContext : GetComponent<PlayerRuntimeContext>();
        SyncFromContext();

        if (hazardController == null)
        {
            hazardController = LevelHazardController.GetOrCreateInstance();
        }

        if (tuningConfig == null)
        {
            tuningConfig = runtimeContext != null ? runtimeContext.TuningConfig : PlayerTuningConfig.Load();
        }
    }

    private void SyncFromContext()
    {
        if (runtimeContext == null)
        {
            return;
        }

        runtimeContext.RefreshReferences();
        formRoot = formRoot != null ? formRoot : runtimeContext.FormRoot;
        groundSensor = groundSensor != null ? groundSensor : runtimeContext.GroundSensor;
        ruleController = ruleController != null ? ruleController : runtimeContext.RuleController;
        inputReader = inputReader != null ? inputReader : runtimeContext.InputReader;
        tuningConfig = tuningConfig != null ? tuningConfig : runtimeContext.TuningConfig;
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
            jumpBufferCounter = tuningConfig != null ? tuningConfig.Movement.JumpBufferTime : GameConstants.DefaultJumpBufferTime;
        }
    }

    private void UpdateGroundTimers()
    {
        bool grounded = IsStableGrounded();
        float coyoteTime = tuningConfig != null ? tuningConfig.Movement.CoyoteTime : GameConstants.DefaultCoyoteTime;
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

        PlayerTuningConfig.MovementSettings movement = tuningConfig != null ? tuningConfig.Movement : null;
        bool shouldUseBoatFloatMode = ShouldUseBoatFloatMode();
        UpdateBoatFloatSimulationState(shouldUseBoatFloatMode);

        switch (formRoot.CurrentForm)
        {
            case PlayerFormType.Human:
                formRoot.PlayerRigidbody.gravityScale = movement != null ? movement.HumanGravityScale : 3f;
                break;
            case PlayerFormType.Car:
                formRoot.PlayerRigidbody.gravityScale = movement != null ? movement.CarGravityScale : 4f;
                break;
            case PlayerFormType.Plane:
                formRoot.PlayerRigidbody.gravityScale = movement != null ? movement.PlaneGravityScale : 0f;
                break;
            case PlayerFormType.Boat:
                formRoot.PlayerRigidbody.gravityScale = shouldUseBoatFloatMode ? 0f : (movement != null ? movement.BoatGravityScale : 3f);
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
        PlayerTuningConfig.MovementSettings movement = tuningConfig != null ? tuningConfig.Movement : null;
        float speedMultiplier = sprintHeld ? (movement != null ? movement.SprintMultiplier : 1.6f) : 1f;
        bool isRunning = false;
        bool shouldUseBoatFloatMode = ShouldUseBoatFloatMode();

        switch (formRoot.CurrentForm)
        {
            case PlayerFormType.Human:
                float humanZoneMultiplier = ruleController != null ? ruleController.HumanSpeedMultiplier : 1f;
                velocity.x = horizontalInput * (movement != null ? movement.HumanMoveSpeed : 4f) * speedMultiplier * humanZoneMultiplier;
                bool isGrounded = IsStableGrounded();
                int maxHumanJumpCount = movement != null ? movement.MaxHumanJumpCount : 2;
                bool canUseGroundJump = jumpBufferCounter > 0f && coyoteTimeCounter > 0f;
                bool canUseAirJump = jumpBufferCounter > 0f && !isGrounded && humanJumpCount > 0 && humanJumpCount < maxHumanJumpCount;
                if (canUseGroundJump || canUseAirJump)
                {
                    velocity.y = 0f;
                    rb.velocity = velocity;
                    rb.AddForce(Vector2.up * (movement != null ? movement.HumanJumpForce : 9f), ForceMode2D.Impulse);
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
                velocity.x = horizontalInput * (movement != null ? movement.CarMoveSpeed : 7f) * speedMultiplier;
                rb.velocity = velocity;
                isRunning = Mathf.Abs(horizontalInput) > 0.01f;
                break;

            case PlayerFormType.Plane:
                velocity.x = horizontalInput * (movement != null ? movement.PlaneMoveSpeed : 6f) * speedMultiplier;
                velocity.y = verticalInput * (movement != null ? movement.PlaneVerticalSpeed : 5f) * speedMultiplier;
                rb.velocity = velocity;
                isRunning = Mathf.Abs(horizontalInput) > 0.01f || Mathf.Abs(verticalInput) > 0.01f;
                break;

            case PlayerFormType.Boat:
                float boatSpeed = movement != null ? movement.BoatMoveSpeed : 4.5f;
                if (ruleController != null && ruleController.IsInBlizzard())
                {
                    boatSpeed = (movement != null ? movement.HumanMoveSpeed : 4f) * ruleController.BlizzardSlowMultiplier;
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

        return Mathf.Abs(rb.velocity.y) <= (tuningConfig != null ? tuningConfig.Movement.GroundedVelocityThreshold : 0.1f);
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
        float floatHeightOffset = tuningConfig != null ? tuningConfig.Movement.BoatFloatHeightOffset : 0.85f;
        float snapDeadZone = tuningConfig != null ? tuningConfig.Movement.BoatFloatSnapDeadZone : 0.08f;
        float verticalSpeed = tuningConfig != null ? tuningConfig.Movement.BoatFloatVerticalSpeed : 8f;
        float targetY = waterSurfaceY + floatHeightOffset;
        position.y = Mathf.Abs(targetY - position.y) <= snapDeadZone
            ? targetY
            : Mathf.MoveTowards(position.y, targetY, verticalSpeed * Time.fixedDeltaTime);
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

        float floatHeightOffset = tuningConfig != null ? tuningConfig.Movement.BoatFloatHeightOffset : 0.85f;
        float activationMargin = tuningConfig != null ? tuningConfig.Movement.BoatFloatActivationMargin : 0.35f;
        float maxSupportedY = waterSurfaceY + floatHeightOffset + activationMargin;
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
