using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

public class RisingWaterHazard : LevelHazardBehaviour
{
    private enum WaterCycleState
    {
        HoldingLow = 0,
        Rising = 1,
        HoldingHigh = 2,
        Falling = 3
    }

    [Header("Optional Overrides")]
    [LabelText("水体表现根节点")]
    [SerializeField] private Transform waterVisualRoot;
    [LabelText("伤害线节点")]
    [SerializeField] private Transform damageLine;
    [LabelText("玩家安全偏移")]
    [SerializeField] private float playerClearanceOffset = 0f;
    [LabelText("水平跟随偏移")]
    [SerializeField] private float horizontalFollowOffset = 0f;

    private HazardProfile hazardProfile;
    private Transform playerTransform;
    private PlayerRuntimeContext playerRuntimeContext;
    private PlayerFormRoot playerFormRoot;
    private PlayerRespawnController playerRespawnController;
    private PlayerHealthController playerHealthController;
    private float currentWaterY;
    private float currentWaterX;
    private WaterCycleState cycleState;
    private float stateTimer;
    private bool initialized;

    public override void Initialize(HazardProfile hazardProfile, Transform playerTransform, GameLevelController levelController)
    {
        this.hazardProfile = hazardProfile;
        this.playerTransform = playerTransform;
        playerRuntimeContext = PlayerRuntimeContext.ResolveFromComponent(playerTransform);
        playerFormRoot = playerRuntimeContext != null ? playerRuntimeContext.FormRoot : null;
        playerRespawnController = playerRuntimeContext != null ? playerRuntimeContext.RespawnController : null;
        playerHealthController = playerRuntimeContext != null ? playerRuntimeContext.HealthController : null;

        currentWaterY = hazardProfile != null ? hazardProfile.RisingWater.StartY : transform.position.y;
        currentWaterX = playerTransform != null
            ? playerTransform.position.x + horizontalFollowOffset
            : (waterVisualRoot != null ? waterVisualRoot.position.x : transform.position.x);
        cycleState = WaterCycleState.HoldingLow;
        stateTimer = GetRandomDuration(hazardProfile.RisingWater.MinHoldAtLowDuration, hazardProfile.RisingWater.MaxHoldAtLowDuration);
        ApplyWaterLineImmediately();
        initialized = true;
    }

    private void Reset()
    {
        waterVisualRoot = transform;
    }

    private void Awake()
    {
        if (waterVisualRoot == null)
        {
            waterVisualRoot = transform;
        }

        if (damageLine == null)
        {
            Transform foundDamageLine = transform.Find("DamageLine");
            if (foundDamageLine != null)
            {
                damageLine = foundDamageLine;
            }
        }
    }

    private void Update()
    {
        if (!initialized || hazardProfile == null)
        {
            return;
        }

        EnsureVisible();
        UpdateWaterHeight();
        UpdateHorizontalFollow();
        UpdatePlayerThreat();
    }

    private void UpdateWaterHeight()
    {
        HazardProfile.RisingWaterSettings settings = hazardProfile.RisingWater;
        switch (cycleState)
        {
            case WaterCycleState.HoldingLow:
                currentWaterY = settings.StartY;
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    cycleState = WaterCycleState.Rising;
                }
                break;

            case WaterCycleState.Rising:
                currentWaterY = Mathf.MoveTowards(currentWaterY, settings.MaxY, settings.RiseSpeed * Time.deltaTime);
                if (Mathf.Approximately(currentWaterY, settings.MaxY))
                {
                    cycleState = WaterCycleState.HoldingHigh;
                    stateTimer = GetRandomDuration(settings.MinHoldAtHighDuration, settings.MaxHoldAtHighDuration);
                }
                break;

            case WaterCycleState.HoldingHigh:
                currentWaterY = settings.MaxY;
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    cycleState = WaterCycleState.Falling;
                }
                break;

            case WaterCycleState.Falling:
                currentWaterY = Mathf.MoveTowards(currentWaterY, settings.StartY, settings.FallSpeed * Time.deltaTime);
                if (Mathf.Approximately(currentWaterY, settings.StartY))
                {
                    cycleState = WaterCycleState.HoldingLow;
                    stateTimer = GetRandomDuration(settings.MinHoldAtLowDuration, settings.MaxHoldAtLowDuration);
                }
                break;
        }

        ApplyWaterLineImmediately();
    }

    private void UpdatePlayerThreat()
    {
        if (playerTransform == null)
        {
            return;
        }

        if (playerFormRoot == null)
        {
            playerRuntimeContext = playerRuntimeContext != null ? playerRuntimeContext : PlayerRuntimeContext.ResolveFromComponent(playerTransform);
            playerFormRoot = playerRuntimeContext != null ? playerRuntimeContext.FormRoot : null;
        }

        float playerY = playerTransform.position.y + playerClearanceOffset;
        float dangerLineY = ResolveDangerLineY();
        if (playerY > dangerLineY)
        {
            return;
        }

        if (IsBoatImmuneToFlood())
        {
            return;
        }

        if (hazardProfile.RisingWater.InstantKillBelowWaterLine)
        {
            if (playerRespawnController != null)
            {
                playerRespawnController.Respawn(FailureType.FellIntoWater);
            }

            return;
        }

        if (playerHealthController == null)
        {
            if (playerRespawnController != null)
            {
                playerRespawnController.Respawn(FailureType.FellIntoWater);
            }

            return;
        }

        playerHealthController.ApplyHazardDamage(Time.deltaTime);
        if (playerHealthController.IsDead() && playerRespawnController != null)
        {
            playerRespawnController.Respawn(FailureType.FellIntoWater);
        }
    }

    private void ApplyWaterLineImmediately()
    {
        if (waterVisualRoot == null)
        {
            return;
        }

        Vector3 position = waterVisualRoot.position;
        position.x = currentWaterX;
        position.y = currentWaterY;
        waterVisualRoot.position = position;
    }

    private void UpdateHorizontalFollow()
    {
        if (playerTransform == null || waterVisualRoot == null)
        {
            return;
        }

        float targetX = playerTransform.position.x + horizontalFollowOffset;
        currentWaterX = targetX;
        ApplyWaterLineImmediately();
    }

    private float ResolveDangerLineY()
    {
        if (damageLine != null)
        {
            return damageLine.position.y;
        }

        if (waterVisualRoot != null)
        {
            return waterVisualRoot.position.y;
        }

        return currentWaterY;
    }

    public bool IsPointBelowDangerLine(Vector3 worldPoint)
    {
        return worldPoint.y <= ResolveDangerLineY();
    }

    public float GetWaterSurfaceY()
    {
        if (waterVisualRoot != null)
        {
            return waterVisualRoot.position.y;
        }

        return currentWaterY;
    }

    public bool IsPointInsideWaterBody(Vector3 worldPoint, float tolerance = 0f)
    {
        return worldPoint.y <= GetWaterSurfaceY() + tolerance;
    }

    private void EnsureVisible()
    {
        if (waterVisualRoot != null && !waterVisualRoot.gameObject.activeSelf)
        {
            waterVisualRoot.gameObject.SetActive(true);
        }
    }

    private bool IsBoatImmuneToFlood()
    {
        return playerFormRoot != null && playerFormRoot.CurrentForm == PlayerFormType.Boat;
    }

    private static float GetRandomDuration(float minDuration, float maxDuration)
    {
        if (maxDuration <= minDuration)
        {
            return Mathf.Max(0f, minDuration);
        }

        return Random.Range(minDuration, maxDuration);
    }
}
