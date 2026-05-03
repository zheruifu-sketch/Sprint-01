using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerHazardResolver : MonoBehaviour
{
    [Header("References")]
    [LabelText("玩家形态根节点")]
    [SerializeField] private PlayerFormRoot formRoot;
    [LabelText("环境规则控制器")]
    [SerializeField] private PlayerRuleController ruleController;

    [Header("Fall Death")]
    [LabelText("使用全局坠落死亡")]
    [SerializeField] private bool useGlobalFallDeath = true;
    [LabelText("悬崖地面高度")]
    [SerializeField] private float cliffGroundY = GameConstants.DefaultCliffGroundY;
    [LabelText("悬崖死亡高度")]
    [SerializeField] private float cliffDeathY = GameConstants.DefaultCliffDeathY;

    public bool UseGlobalFallDeath => useGlobalFallDeath;
    public float CliffGroundY => cliffGroundY;
    public float CliffDeathY => cliffDeathY;

    private void Reset()
    {
        CacheReferences();
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void CacheReferences()
    {
        formRoot = formRoot != null ? formRoot : GetComponent<PlayerFormRoot>();
        ruleController = ruleController != null ? ruleController : GetComponent<PlayerRuleController>();
    }

    public bool IsProtectedBoatState()
    {
        return false;
    }

    public bool TryGetActiveHazard(out FailureType failureType)
    {
        if (IsProtectedBoatState())
        {
            failureType = FailureType.None;
            return false;
        }

        if (useGlobalFallDeath && transform.position.y <= cliffDeathY)
        {
            failureType = FailureType.FellFromCliff;
            return true;
        }

        if (ruleController != null && formRoot != null && formRoot.CurrentForm != PlayerFormType.Boat && ruleController.IsInWater())
        {
            failureType = FailureType.FellIntoWater;
            return true;
        }

        bool isInCliffDanger = ruleController != null
                              && formRoot != null
                              && formRoot.CurrentForm != PlayerFormType.Plane
                              && ruleController.IsInCliff()
                              && transform.position.y <= cliffGroundY
                              && transform.position.y <= cliffDeathY;
        if (isInCliffDanger)
        {
            failureType = FailureType.FellFromCliff;
            return true;
        }

        if (ruleController != null
            && formRoot != null
            && formRoot.CurrentForm == PlayerFormType.Boat
            && !ruleController.IsBoatSupportedSurface())
        {
            failureType = FailureType.InvalidForm;
            return true;
        }

        failureType = FailureType.None;
        return false;
    }
}
