using UnityEngine;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerRuleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFormRoot formRoot;
    [SerializeField] private PlayerZoneSensor zoneSensor;
    [SerializeField] private GameLevelController levelController;
    [SerializeField] private GameSessionController sessionController;
    [SerializeField] private LevelHazardController hazardController;

    [Header("Rules")]
    [SerializeField] private float blizzardHumanSpeedMultiplier = 0.3f;
    [SerializeField] private float transformCooldown = GameConstants.DefaultTransformCooldown;
    [SerializeField] private bool forceHumanWhenPlaneBlocked = true;
    [SerializeField] private Vector2 boatSwitchCheckOffset = GameConstants.DefaultBoatSwitchCheckOffset;
    [SerializeField] private float boatSwitchCheckRadius = GameConstants.DefaultBoatSwitchCheckRadius;
    [SerializeField] private float floodBoatSupportHeight = 1.5f;

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
        levelController = GameLevelController.GetOrCreateInstance();
        sessionController = FindObjectOfType<GameSessionController>();
        hazardController = FindObjectOfType<LevelHazardController>();
    }

    private void Awake()
    {
        if (formRoot == null)
        {
            formRoot = GetComponent<PlayerFormRoot>();
        }

        if (levelController == null)
        {
            levelController = GameLevelController.GetOrCreateInstance();
        }

        if (sessionController == null)
        {
            sessionController = GameSessionController.GetOrCreate();
        }

        if (hazardController == null)
        {
            hazardController = LevelHazardController.GetOrCreateInstance();
        }
    }

    private void OnEnable()
    {
        if (levelController == null)
        {
            levelController = GameLevelController.GetOrCreateInstance();
        }

        if (levelController != null)
        {
            levelController.LevelChanged += HandleLevelChanged;
        }
    }

    private void OnDisable()
    {
        if (levelController != null)
        {
            levelController.LevelChanged -= HandleLevelChanged;
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
        return zoneSensor != null && zoneSensor.IsInEnvironment(EnvironmentType.Water);
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

        return transform.position.y <= waterSurfaceY + floodBoatSupportHeight;
    }

    public bool IsInWaterEnvironment()
    {
        return IsInWater() || IsInFloodWater();
    }

    public bool IsInCliff()
    {
        return zoneSensor != null && zoneSensor.IsInEnvironment(EnvironmentType.Cliff);
    }

    public bool IsInBlizzard()
    {
        return zoneSensor != null && zoneSensor.IsInEnvironment(EnvironmentType.Blizzard);
    }

    public bool IsBoatSupportedSurface()
    {
        return IsInWater() || IsBoatSupportedByFlood() || IsInBlizzard();
    }

    private void HandleFormHotkeys()
    {
        if (sessionController == null || !sessionController.HasActiveRun)
        {
            return;
        }

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
        if (levelController != null && !levelController.IsFormUnlocked(targetForm))
        {
            return false;
        }

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

        if (levelController != null && !levelController.IsFormUnlocked(formRoot.CurrentForm))
        {
            formRoot.SetForm(levelController.GetFallbackUnlockedForm());
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
        if (IsInFloodWater())
        {
            return true;
        }

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

            if (WorldSemanticUtility.HasEnvironment(hit, EnvironmentType.Water))
            {
                return true;
            }
        }

        return false;
    }

    private void HandleLevelChanged(int _)
    {
        if (formRoot == null || levelController == null)
        {
            return;
        }

        if (!levelController.IsFormUnlocked(formRoot.CurrentForm))
        {
            formRoot.SetForm(levelController.GetFallbackUnlockedForm());
        }
    }
}
