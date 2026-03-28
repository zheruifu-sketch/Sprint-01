using UnityEngine;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerRuleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFormRoot formRoot;
    [SerializeField] private PlayerZoneSensor zoneSensor;

    [Header("Rules")]
    [SerializeField] private float blizzardHumanSpeedMultiplier = 0.3f;
    [SerializeField] private bool forceHumanWhenPlaneBlocked = true;
    [SerializeField] private Vector2 boatSwitchCheckOffset = GameConstants.DefaultBoatSwitchCheckOffset;
    [SerializeField] private float boatSwitchCheckRadius = GameConstants.DefaultBoatSwitchCheckRadius;

    private readonly Collider2D[] boatSwitchResults = new Collider2D[16];

    public float HumanSpeedMultiplier => IsInBlizzardAsHuman() ? blizzardHumanSpeedMultiplier : 1f;

    private void Reset()
    {
        formRoot = GetComponent<PlayerFormRoot>();
        zoneSensor = GetComponent<PlayerZoneSensor>();
    }

    private void Awake()
    {
        if (formRoot == null)
        {
            formRoot = GetComponent<PlayerFormRoot>();
        }
    }

    private void Update()
    {
        HandleFormHotkeys();
        ApplyForcedFormRules();
    }

    public bool IsInWater()
    {
        return zoneSensor != null && zoneSensor.IsInZone(ZoneType.Water);
    }

    public bool IsInCliff()
    {
        return zoneSensor != null && zoneSensor.IsInZone(ZoneType.Cliff);
    }

    public bool IsInBlizzard()
    {
        return zoneSensor != null && zoneSensor.IsInZone(ZoneType.Blizzard);
    }

    private void HandleFormHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TrySwitchForm(PlayerFormType.Human);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TrySwitchForm(PlayerFormType.Car);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TrySwitchForm(PlayerFormType.Plane);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TrySwitchForm(PlayerFormType.Boat);
        }
    }

    private void TrySwitchForm(PlayerFormType targetForm)
    {
        if (formRoot == null || !CanUseForm(targetForm))
        {
            return;
        }

        formRoot.SetForm(targetForm);
    }

    private bool CanUseForm(PlayerFormType targetForm)
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
            return IsInWater() || CanSwitchBoatFromNearbyWater();
        }

        return true;
    }

    private void ApplyForcedFormRules()
    {
        if (formRoot == null)
        {
            return;
        }

        if (formRoot.CurrentForm == PlayerFormType.Plane && IsInBlizzard() && !IsInCliff())
        {
            if (forceHumanWhenPlaneBlocked)
            {
                formRoot.SetForm(PlayerFormType.Human);
            }
        }
    }

    private bool IsInBlizzardAsHuman()
    {
        return formRoot != null && formRoot.CurrentForm == PlayerFormType.Human && IsInBlizzard();
    }

    private bool CanSwitchBoatFromNearbyWater()
    {
        Vector2 origin = transform.position;
        origin += boatSwitchCheckOffset;

        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, boatSwitchCheckRadius, boatSwitchResults);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = boatSwitchResults[i];
            if (hit == null)
            {
                continue;
            }

            ZoneDefinition zoneDefinition = hit.GetComponent<ZoneDefinition>();
            if (zoneDefinition != null && zoneDefinition.ZoneType == ZoneType.Water)
            {
                return true;
            }

            if (hit.CompareTag("Water"))
            {
                return true;
            }
        }

        return false;
    }
}
