using UnityEngine;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerRuleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFormRoot formRoot;
    [SerializeField] private PlayerZoneSensor zoneSensor;

    [Header("Rules")]
    [SerializeField] private float blizzardHumanSpeedMultiplier = 0.3f;
    [SerializeField] private float transformCooldown = GameConstants.DefaultTransformCooldown;
    [SerializeField] private bool forceHumanWhenPlaneBlocked = true;
    [SerializeField] private Vector2 boatSwitchCheckOffset = GameConstants.DefaultBoatSwitchCheckOffset;
    [SerializeField] private float boatSwitchCheckRadius = GameConstants.DefaultBoatSwitchCheckRadius;

    private readonly Collider2D[] boatSwitchResults = new Collider2D[16];
    private float transformCooldownRemaining;

    public float HumanSpeedMultiplier => IsInBlizzardAsHuman() ? blizzardHumanSpeedMultiplier : 1f;
    public float BlizzardSlowMultiplier => blizzardHumanSpeedMultiplier;
    public bool IsTransformOnCooldown => transformCooldownRemaining > 0f;
    public float TransformCooldownNormalizedRemaining
    {
        get
        {
            if (transformCooldown <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(transformCooldownRemaining / transformCooldown);
        }
    }

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
        UpdateTransformCooldown();
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

    public bool IsBoatSupportedSurface()
    {
        return IsInWater() || IsInBlizzard();
    }

    private void HandleFormHotkeys()
    {
        if (IsTransformOnCooldown)
        {
            return;
        }

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

        if (formRoot.CurrentForm == targetForm)
        {
            return;
        }

        formRoot.SetForm(targetForm);
        StartTransformCooldown();
    }

    private void UpdateTransformCooldown()
    {
        if (transformCooldownRemaining <= 0f)
        {
            return;
        }

        transformCooldownRemaining -= Time.deltaTime;
        if (transformCooldownRemaining < 0f)
        {
            transformCooldownRemaining = 0f;
        }
    }

    private void StartTransformCooldown()
    {
        if (transformCooldown <= 0f)
        {
            transformCooldownRemaining = 0f;
            return;
        }

        transformCooldownRemaining = transformCooldown;
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
            return IsBoatSupportedSurface() || CanSwitchBoatFromNearbyWater();
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
