using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("References")]
    [LabelText("玩家形态根节点")]
    [SerializeField] private PlayerFormRoot formRoot;
    [LabelText("地面检测器")]
    [SerializeField] private PlayerGroundSensor groundSensor;
    [LabelText("环境规则控制器")]
    [SerializeField] private PlayerRuleController ruleController;
    [LabelText("输入读取器")]
    [SerializeField] private PlayerInputReader inputReader;
    [LabelText("跑局控制器")]
    [SerializeField] private GameSessionController sessionController;
    [LabelText("玩家调参配置")]
    [SerializeField] private PlayerTuningConfig tuningConfig;

    private float horizontalInput;
    private float verticalInput;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private int remainingHumanJumps;
    private bool jumpHeld;
    private bool jumpCutConsumed;

    private void Reset()
    {
        CacheReferences();
        sessionController = FindObjectOfType<GameSessionController>();
        tuningConfig = PlayerTuningConfig.Load();
    }

    private void Awake()
    {
        CacheReferences();

        if (tuningConfig == null)
        {
            tuningConfig = PlayerTuningConfig.Load();
        }

        if (sessionController == null)
        {
            sessionController = FindObjectOfType<GameSessionController>();
        }
    }

    private void CacheReferences()
    {
        formRoot = formRoot != null ? formRoot : GetComponent<PlayerFormRoot>();
        groundSensor = groundSensor != null ? groundSensor : GetComponent<PlayerGroundSensor>();
        ruleController = ruleController != null ? ruleController : GetComponent<PlayerRuleController>();
        inputReader = inputReader != null ? inputReader : GetComponent<PlayerInputReader>();
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
            jumpHeld = false;
            return;
        }

        horizontalInput = inputReader.HorizontalInput;
        verticalInput = inputReader.VerticalInput;
        jumpHeld = inputReader.JumpHeld;
        if (inputReader.JumpPressedThisFrame)
        {
            jumpBufferCounter = tuningConfig != null ? tuningConfig.Movement.JumpBufferTime : GameConstants.DefaultJumpBufferTime;
        }
    }

    private void UpdateGroundTimers()
    {
        if (groundSensor != null)
        {
            groundSensor.Refresh();
        }

        bool grounded = IsStableGrounded();
        float coyoteTime = tuningConfig != null ? tuningConfig.Movement.CoyoteTime : GameConstants.DefaultCoyoteTime;
        if (grounded)
        {
            coyoteTimeCounter = coyoteTime;
            remainingHumanJumps = tuningConfig != null ? tuningConfig.Movement.MaxHumanJumpCount : 2;
            jumpCutConsumed = false;
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
                formRoot.PlayerRigidbody.gravityScale = movement != null ? movement.BoatGravityScale : 3f;
                break;
        }
    }

    private void ApplyMovement()
    {
        if (formRoot == null || formRoot.PlayerRigidbody == null)
        {
            return;
        }

        if (sessionController == null)
        {
            sessionController = FindObjectOfType<GameSessionController>();
        }

        if (sessionController != null && !sessionController.IsGameplayRunning)
        {
            StopMotion();
            return;
        }

        Rigidbody2D rb = formRoot.PlayerRigidbody;
        Vector2 velocity = rb.velocity;
        PlayerTuningConfig.MovementSettings movement = tuningConfig != null ? tuningConfig.Movement : null;
        float speedMultiplier = ResolveForwardSpeedMultiplier(movement);
        bool isRunning = false;

        if (groundSensor != null)
        {
            groundSensor.Refresh();
        }

        switch (formRoot.CurrentForm)
        {
            case PlayerFormType.Human:
                float humanZoneMultiplier = ruleController != null ? ruleController.HumanSpeedMultiplier : 1f;
                bool isGrounded = IsStableGrounded();
                int maxHumanJumpCount = movement != null ? movement.MaxHumanJumpCount : 2;
                if (isGrounded)
                {
                    remainingHumanJumps = maxHumanJumpCount;
                }

                velocity.x = GetSmoothedHorizontalVelocity(
                    velocity.x,
                    (movement != null ? movement.HumanMoveSpeed : 4f) * speedMultiplier * humanZoneMultiplier,
                    isGrounded,
                    movement);
                bool hasJumpInputBuffered = jumpBufferCounter > 0f;
                bool canUseGroundJump = hasJumpInputBuffered && coyoteTimeCounter > 0f;
                bool canUseAirJump = hasJumpInputBuffered && !canUseGroundJump && remainingHumanJumps > 0;
                if (canUseGroundJump || canUseAirJump)
                {
                    velocity.y = 0f;
                    rb.velocity = velocity;
                    rb.AddForce(Vector2.up * (movement != null ? movement.HumanJumpForce : 9f), ForceMode2D.Impulse);
                    remainingHumanJumps = Mathf.Max(0, remainingHumanJumps - 1);
                    jumpBufferCounter = 0f;
                    coyoteTimeCounter = 0f;
                    jumpCutConsumed = false;
                }
                else
                {
                    if (!jumpHeld && !jumpCutConsumed && rb.velocity.y > 0f)
                    {
                        velocity.y = rb.velocity.y * (movement != null ? movement.ShortJumpMultiplier : 0.45f);
                        jumpCutConsumed = true;
                    }

                    rb.velocity = velocity;
                }
                isRunning = velocity.x > 0.05f && isGrounded;
                break;

            case PlayerFormType.Car:
                velocity.x = GetSmoothedHorizontalVelocity(
                    velocity.x,
                    (movement != null ? movement.CarMoveSpeed : 7f) * speedMultiplier,
                    IsStableGrounded(),
                    movement);
                rb.velocity = velocity;
                isRunning = velocity.x > 0.05f;
                break;

            case PlayerFormType.Plane:
                velocity.x = (movement != null ? movement.PlaneMoveSpeed : 6f) * speedMultiplier;
                velocity.y = verticalInput * (movement != null ? movement.PlaneVerticalSpeed : 5f) * speedMultiplier;
                rb.velocity = velocity;
                isRunning = velocity.x > 0.05f || Mathf.Abs(verticalInput) > 0.01f;
                break;

            case PlayerFormType.Boat:
                float boatSpeed = movement != null ? movement.BoatMoveSpeed : 4.5f;
                if (ruleController != null && ruleController.IsInBlizzard())
                {
                    boatSpeed = (movement != null ? movement.HumanMoveSpeed : 4f) * ruleController.BlizzardSlowMultiplier;
                }

                velocity.x = boatSpeed * speedMultiplier;
                rb.velocity = velocity;
                isRunning = true;
                break;
        }

        formRoot.SetFacingFromHorizontal(1f);
        formRoot.SetRunState(isRunning);
    }

    private void StopMotion()
    {
        if (formRoot == null || formRoot.PlayerRigidbody == null)
        {
            return;
        }

        Rigidbody2D rb = formRoot.PlayerRigidbody;
        Vector2 velocity = rb.velocity;
        if (velocity.sqrMagnitude > 0f)
        {
            rb.velocity = Vector2.zero;
        }

        formRoot.SetRunState(false);
    }

    private float ResolveForwardSpeedMultiplier(PlayerTuningConfig.MovementSettings movement)
    {
        float baseForwardMultiplier = movement != null ? movement.DefaultForwardMultiplier : 1.6f;

        if (inputReader != null)
        {
            if (inputReader.IsForwardBoostHeld)
            {
                float boostMultiplier = movement != null ? movement.ForwardBoostMultiplier : GameConstants.DefaultForwardBoostMultiplier;
                return baseForwardMultiplier * boostMultiplier;
            }

            if (inputReader.IsForwardBrakeHeld)
            {
                float brakeMultiplier = movement != null ? movement.ForwardBrakeMultiplier : GameConstants.DefaultForwardBrakeMultiplier;
                return baseForwardMultiplier * brakeMultiplier;
            }
        }

        return baseForwardMultiplier;
    }

    private float GetSmoothedHorizontalVelocity(float currentVelocityX, float targetVelocityX, bool isGrounded, PlayerTuningConfig.MovementSettings movement)
    {
        float airControlMultiplier = movement != null ? movement.AirControlMultiplier : 0.55f;
        float acceleration = movement != null ? movement.HorizontalAcceleration : 45f;
        float deceleration = movement != null ? movement.HorizontalDeceleration : 60f;
        float controlMultiplier = isGrounded ? 1f : airControlMultiplier;
        float moveRate = Mathf.Abs(targetVelocityX) > 0.01f ? acceleration : deceleration;
        return Mathf.MoveTowards(currentVelocityX, targetVelocityX, moveRate * controlMultiplier * Time.fixedDeltaTime);
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
}
