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
    private int humanJumpsUsed;
    private bool jumpHeld;
    private bool jumpCutConsumed;
    private bool wasStableGroundedLastFrame;

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

        humanJumpsUsed = 0;
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
        UpdateJumpBufferTimer();
        ApplyGravityPreset();
    }

    private void FixedUpdate()
    {
        UpdateGroundState();
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

    private void UpdateJumpBufferTimer()
    {
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void UpdateGroundState()
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
            if (!wasStableGroundedLastFrame)
            {
                // Reset jump budget only on a confirmed landing edge so jump count
                // stays deterministic instead of fluctuating with transient contacts.
                humanJumpsUsed = 0;
                jumpCutConsumed = false;
            }
        }
        else
        {
            coyoteTimeCounter -= Time.fixedDeltaTime;
        }

        wasStableGroundedLastFrame = grounded;
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
        float speedMultiplier = ResolveSprintSpeedMultiplier(movement);
        bool isRunning = false;

        switch (formRoot.CurrentForm)
        {
            case PlayerFormType.Human:
                float humanZoneMultiplier = ruleController != null ? ruleController.HumanSpeedMultiplier : 1f;
                bool isGrounded = IsStableGrounded();
                velocity.x = GetSmoothedHorizontalVelocity(
                    velocity.x,
                    horizontalInput * (movement != null ? movement.HumanMoveSpeed : 4f) * speedMultiplier * humanZoneMultiplier,
                    isGrounded,
                    movement);
                bool hasJumpInputBuffered = jumpBufferCounter > 0f;
                int maxHumanJumpCount = movement != null ? movement.MaxHumanJumpCount : 2;
                // MaxHumanJumpCount now means the total jump count in one airtime:
                // first ground/coyote jump counts as 1, extra air jumps consume the rest.
                bool canUseGroundJump = hasJumpInputBuffered && coyoteTimeCounter > 0f && humanJumpsUsed == 0;
                bool canUseAirJump = hasJumpInputBuffered && !canUseGroundJump && humanJumpsUsed > 0 && humanJumpsUsed < maxHumanJumpCount;
                if (canUseGroundJump || canUseAirJump)
                {
                    velocity.y = 0f;
                    rb.velocity = velocity;
                    rb.AddForce(Vector2.up * (movement != null ? movement.HumanJumpForce : 9f), ForceMode2D.Impulse);
                    humanJumpsUsed++;
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
                isRunning = !isGrounded || Mathf.Abs(velocity.x) > 0.05f;
                break;

            case PlayerFormType.Car:
                velocity.x = GetSmoothedHorizontalVelocity(
                    velocity.x,
                    horizontalInput * (movement != null ? movement.CarMoveSpeed : 7f) * speedMultiplier * (ruleController != null ? ruleController.CarSpeedMultiplier : 1f),
                    IsStableGrounded(),
                    movement);
                rb.velocity = velocity;
                isRunning = Mathf.Abs(velocity.x) > 0.05f;
                break;

            case PlayerFormType.Plane:
                velocity.x = horizontalInput * (movement != null ? movement.PlaneMoveSpeed : 6f) * speedMultiplier;
                velocity.y = verticalInput * (movement != null ? movement.PlaneVerticalSpeed : 5f) * speedMultiplier;
                rb.velocity = velocity;
                isRunning = Mathf.Abs(velocity.x) > 0.05f || Mathf.Abs(verticalInput) > 0.01f;
                break;

            case PlayerFormType.Boat:
                float boatSpeed = movement != null ? movement.BoatMoveSpeed : 4.5f;
                if (ruleController != null && ruleController.IsInBlizzard())
                {
                    boatSpeed *= ruleController.BlizzardBoatSpeedMultiplier;
                }

                velocity.x = horizontalInput * boatSpeed * speedMultiplier;
                rb.velocity = velocity;
                isRunning = Mathf.Abs(velocity.x) > 0.05f;
                break;
        }

        float facingHorizontal = Mathf.Abs(velocity.x) > 0.01f ? velocity.x : horizontalInput;
        if (Mathf.Abs(facingHorizontal) > 0.01f)
        {
            formRoot.SetFacingFromHorizontal(facingHorizontal);
        }

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

    private float ResolveSprintSpeedMultiplier(PlayerTuningConfig.MovementSettings movement)
    {
        if (inputReader != null && inputReader.IsSprintHeld)
        {
            return movement != null ? movement.ForwardBoostMultiplier : GameConstants.DefaultForwardBoostMultiplier;
        }

        return 1f;
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
        
        // A valid foot contact is enough to count as grounded. Sinking or moving
        // platforms can carry the player with a non-zero Y velocity, and using the
        // player's world-space vertical speed here makes jump input randomly fail.
        return true;
    }
}
