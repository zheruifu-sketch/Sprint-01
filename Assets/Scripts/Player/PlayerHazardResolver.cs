using UnityEngine;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerHazardResolver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFormRoot formRoot;
    [SerializeField] private PlayerRuleController ruleController;

    [Header("Fall Death")]
    [SerializeField] private bool useGlobalFallDeath = true;
    [SerializeField] private float cliffGroundY = GameConstants.DefaultCliffGroundY;
    [SerializeField] private float cliffDeathY = GameConstants.DefaultCliffDeathY;

    public bool UseGlobalFallDeath => useGlobalFallDeath;
    public float CliffGroundY => cliffGroundY;
    public float CliffDeathY => cliffDeathY;

    private void Reset()
    {
        formRoot = GetComponent<PlayerFormRoot>();
        ruleController = GetComponent<PlayerRuleController>();
    }

    private void Awake()
    {
        if (formRoot == null)
        {
            formRoot = GetComponent<PlayerFormRoot>();
        }

        if (ruleController == null)
        {
            ruleController = GetComponent<PlayerRuleController>();
        }
    }

    public bool IsProtectedBoatState()
    {
        return ruleController != null
               && formRoot != null
               && formRoot.CurrentForm == PlayerFormType.Boat
               && ruleController.IsBoatSupportedByFlood();
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
