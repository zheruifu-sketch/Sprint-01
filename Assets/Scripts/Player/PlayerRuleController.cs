using UnityEngine;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerRuleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFormRoot formRoot;
    [SerializeField] private PlayerEnvironmentContext environmentContext;
    [SerializeField] private LevelHazardController hazardController;
    [SerializeField] private PlayerTuningConfig tuningConfig;

    private readonly Collider2D[] boatSwitchResults = new Collider2D[16];

    public float HumanSpeedMultiplier => IsInBlizzardAsHuman() ? (tuningConfig != null ? tuningConfig.EnvironmentRules.BlizzardHumanSpeedMultiplier : 0.3f) : 1f;
    public float BlizzardSlowMultiplier => tuningConfig != null ? tuningConfig.EnvironmentRules.BlizzardHumanSpeedMultiplier : 0.3f;

    private void Reset()
    {
        formRoot = GetComponent<PlayerFormRoot>();
        environmentContext = GetComponent<PlayerEnvironmentContext>();
        hazardController = FindObjectOfType<LevelHazardController>();
        tuningConfig = PlayerTuningConfig.Load();
    }

    private void Awake()
    {
        if (formRoot == null)
        {
            formRoot = GetComponent<PlayerFormRoot>();
        }

        if (environmentContext == null)
        {
            environmentContext = GetComponent<PlayerEnvironmentContext>();
        }

        if (hazardController == null)
        {
            hazardController = LevelHazardController.GetOrCreateInstance();
        }

        if (tuningConfig == null)
        {
            tuningConfig = PlayerTuningConfig.Load();
        }
    }

    public bool IsInWater()
    {
        return environmentContext != null && environmentContext.IsInEnvironment(EnvironmentType.Water);
    }

    public bool IsInFloodWater()
    {
        return hazardController != null && hazardController.IsPointInsideGlobalWaterBody(transform.position, 0.35f);
    }

    public bool IsBoatSupportedByFlood()
    {
        if (formRoot == null || formRoot.CurrentForm != PlayerFormType.Boat || hazardController == null)
        {
            return false;
        }

        if (!hazardController.TryGetGlobalWaterSurfaceY(out float waterSurfaceY))
        {
            return false;
        }

        float floodBoatSupportHeight = tuningConfig != null ? tuningConfig.EnvironmentRules.FloodBoatSupportHeight : 1.5f;
        return transform.position.y <= waterSurfaceY + floodBoatSupportHeight;
    }

    public bool IsInWaterEnvironment()
    {
        return IsInWater() || IsInFloodWater();
    }

    public bool IsInCliff()
    {
        return environmentContext != null && environmentContext.IsInEnvironment(EnvironmentType.Cliff);
    }

    public bool IsInBlizzard()
    {
        return environmentContext != null && environmentContext.IsInEnvironment(EnvironmentType.Blizzard);
    }

    public bool IsBoatSupportedSurface()
    {
        return IsInWater() || IsBoatSupportedByFlood() || IsInBlizzard();
    }

    public bool CanUseForm(PlayerFormType targetForm)
    {
        if (IsInCliff())
        {
            return targetForm != PlayerFormType.Boat;
        }

        if (targetForm == PlayerFormType.Plane && IsInBlizzard())
        {
            return false;
        }

        if (targetForm == PlayerFormType.Boat)
        {
            return IsBoatSupportedSurface() || CanSwitchBoatFromNearbyWater();
        }

        return true;
    }

    public bool IsPlaneBlockedByEnvironment()
    {
        return IsInBlizzard() && !IsInCliff();
    }

    private bool IsInBlizzardAsHuman()
    {
        return formRoot != null && formRoot.CurrentForm == PlayerFormType.Human && IsInBlizzard();
    }

    private bool CanSwitchBoatFromNearbyWater()
    {
        if (IsInFloodWater())
        {
            return true;
        }

        Vector2 origin = transform.position;
        origin += tuningConfig != null ? tuningConfig.EnvironmentRules.BoatSwitchCheckOffset : GameConstants.DefaultBoatSwitchCheckOffset;

        float boatSwitchCheckRadius = tuningConfig != null ? tuningConfig.EnvironmentRules.BoatSwitchCheckRadius : GameConstants.DefaultBoatSwitchCheckRadius;
        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, boatSwitchCheckRadius, boatSwitchResults);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = boatSwitchResults[i];
            if (hit == null)
            {
                continue;
            }

            if (WorldSemanticUtility.HasEnvironment(hit, EnvironmentType.Water))
            {
                return true;
            }
        }

        return false;
    }
}
