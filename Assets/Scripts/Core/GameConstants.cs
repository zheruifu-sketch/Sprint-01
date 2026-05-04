using UnityEngine;

public static class GameConstants
{
    public const float DefaultGroundCheckRadius = 0.2f;
    public const float DefaultJumpBufferTime = 0.15f;
    public const float DefaultCoyoteTime = 0.12f;
    public const float DefaultWaterDeathDelay = 0.2f;
    public const float DefaultMaxHealth = 100f;
    public const float DefaultHazardDamagePerSecond = 20f;
    public const float DefaultMaxFuel = 100f;
    public const float DefaultCarFuelCostPerSecond = 0.5f;
    public const float DefaultPlaneFuelCostPerSecond = 5f;
    public const float DefaultBoatFuelCostPerSecond = 1f;
    public const float DefaultTransformFuelCost = 8f;
    public const float DefaultForwardBoostMultiplier = 1.45f;
    public const float DefaultForwardBrakeMultiplier = 0.55f;
    public const float DefaultForwardBoostFuelCostPerSecond = 3f;
    public const float DefaultCliffGroundY = 0f;
    public const float DefaultCliffDeathY = -8f;
    public const float DefaultCameraSmoothTime = 0.12f;
    public const float DefaultTransformInvulnerabilityDuration = 1f;
    public const float DefaultEnvironmentPreviewForwardDistance = 1.2f;
    public const float DefaultEnvironmentPreviewBackwardTolerance = 0.15f;
    public const float DefaultEnvironmentPreviewVerticalTolerance = 0.75f;
    public const float DefaultEnvironmentPreviewBoundsExpandPercent = 0.2f;

    public static readonly Vector2 DefaultBoatSwitchCheckOffset = new Vector2(0f, -0.35f);
    public const float DefaultBoatSwitchCheckRadius = 0.2f;
}
