using UnityEngine;

public static class GameConstants
{
    public const float DefaultGroundCheckRadius = 0.2f;
    public const float DefaultJumpBufferTime = 0.15f;
    public const float DefaultCoyoteTime = 0.12f;
    public const float DefaultTransformCooldown = 1f;
    public const float DefaultWaterDeathDelay = 0.2f;
    public const float DefaultMaxHealth = 100f;
    public const float DefaultHazardDamagePerSecond = 20f;
    public const float DefaultMaxEnergy = 100f;
    public const float DefaultCarEnergyCostPerSecond = 0.5f;
    public const float DefaultPlaneEnergyCostPerSecond = 5f;
    public const float DefaultBoatEnergyCostPerSecond = 1f;
    public const float DefaultCliffGroundY = 0f;
    public const float DefaultCliffDeathY = -8f;
    public const float DefaultCameraSmoothTime = 0.12f;

    public static readonly Vector2 DefaultBoatSwitchCheckOffset = new Vector2(0f, -0.35f);
    public const float DefaultBoatSwitchCheckRadius = 0.2f;
}
