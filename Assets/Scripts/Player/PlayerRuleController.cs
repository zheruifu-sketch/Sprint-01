using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerRuleController : MonoBehaviour
{
    [Header("References")]
    [LabelText("玩家形态根节点")]
    [SerializeField] private PlayerFormRoot formRoot;
    [LabelText("环境感知上下文")]
    [SerializeField] private PlayerEnvironmentContext environmentContext;
    [LabelText("玩家调参配置")]
    [SerializeField] private PlayerTuningConfig tuningConfig;

    public float HumanSpeedMultiplier => IsInBlizzardAsHuman() ? (tuningConfig != null ? tuningConfig.EnvironmentRules.BlizzardHumanSpeedMultiplier : 0.3f) : 1f;
    public float BlizzardSlowMultiplier => tuningConfig != null ? tuningConfig.EnvironmentRules.BlizzardHumanSpeedMultiplier : 0.3f;

    private void Reset()
    {
        CacheReferences();
        tuningConfig = PlayerTuningConfig.Load();
    }

    private void Awake()
    {
        CacheReferences();

        if (tuningConfig == null)
        {
            tuningConfig = PlayerTuningConfig.Load();
        }
    }

    private void CacheReferences()
    {
        formRoot = formRoot != null ? formRoot : GetComponent<PlayerFormRoot>();
        environmentContext = environmentContext != null ? environmentContext : GetComponent<PlayerEnvironmentContext>();
    }

    public bool IsInWater()
    {
        return environmentContext != null && environmentContext.IsInEnvironment(EnvironmentType.Water);
    }

    public bool IsInWaterEnvironment()
    {
        return IsInWater();
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
        return IsInWater() || IsInBlizzard();
    }

    public bool CanUseForm(PlayerFormType targetForm)
    {
        bool inOrApproachingWater = IsInOrApproachingEnvironment(EnvironmentType.Water);
        bool inOrApproachingCliff = IsInOrApproachingEnvironment(EnvironmentType.Cliff);
        bool inOrApproachingBlizzard = IsInOrApproachingEnvironment(EnvironmentType.Blizzard);

        if (targetForm == PlayerFormType.Boat)
        {
            return inOrApproachingWater || inOrApproachingBlizzard;
        }

        if (targetForm == PlayerFormType.Plane)
        {
            return !IsInBlizzard() || inOrApproachingCliff;
        }

        return true;
    }

    public bool IsPlaneBlockedByEnvironment()
    {
        return IsInBlizzard() && !IsInOrApproachingEnvironment(EnvironmentType.Cliff);
    }

    private bool IsInBlizzardAsHuman()
    {
        return formRoot != null && formRoot.CurrentForm == PlayerFormType.Human && IsInBlizzard();
    }

    private bool IsInOrApproachingEnvironment(EnvironmentType environmentType)
    {
        return environmentContext != null && environmentContext.IsInOrPreviewingEnvironment(environmentType);
    }
}
