using UnityEngine;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerFormController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFormRoot formRoot;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerRuleController ruleController;
    [SerializeField] private GameLevelController levelController;
    [SerializeField] private PlayerTuningConfig tuningConfig;

    private float transformCooldownRemaining;

    public bool IsTransformOnCooldown => transformCooldownRemaining > 0f;
    public float TransformCooldownNormalizedRemaining
    {
        get
        {
            float cooldown = tuningConfig != null ? tuningConfig.Form.TransformCooldown : GameConstants.DefaultTransformCooldown;
            if (cooldown <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(transformCooldownRemaining / cooldown);
        }
    }

    private void Reset()
    {
        formRoot = GetComponent<PlayerFormRoot>();
        inputReader = GetComponent<PlayerInputReader>();
        ruleController = GetComponent<PlayerRuleController>();
        levelController = GameLevelController.GetOrCreateInstance();
        tuningConfig = PlayerTuningConfig.Load();
    }

    private void Awake()
    {
        if (formRoot == null)
        {
            formRoot = GetComponent<PlayerFormRoot>();
        }

        if (inputReader == null)
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        if (ruleController == null)
        {
            ruleController = GetComponent<PlayerRuleController>();
        }

        if (levelController == null)
        {
            levelController = GameLevelController.GetOrCreateInstance();
        }

        if (tuningConfig == null)
        {
            tuningConfig = PlayerTuningConfig.Load();
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
        HandleRequestedForm();
        ApplyForcedFormRules();
    }

    private void HandleRequestedForm()
    {
        if (inputReader == null || !inputReader.RequestedFormThisFrame.HasValue)
        {
            return;
        }

        if (IsTransformOnCooldown)
        {
            return;
        }

        TrySwitchForm(inputReader.RequestedFormThisFrame.Value);
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
        float cooldown = tuningConfig != null ? tuningConfig.Form.TransformCooldown : GameConstants.DefaultTransformCooldown;
        if (cooldown <= 0f)
        {
            transformCooldownRemaining = 0f;
            return;
        }

        transformCooldownRemaining = cooldown;
    }

    private bool CanUseForm(PlayerFormType targetForm)
    {
        if (levelController != null && !levelController.IsFormUnlocked(targetForm))
        {
            return false;
        }

        if (ruleController != null)
        {
            return ruleController.CanUseForm(targetForm);
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

        bool forceHumanWhenPlaneBlocked = tuningConfig != null ? tuningConfig.Form.ForceHumanWhenPlaneBlocked : true;
        if (!forceHumanWhenPlaneBlocked || ruleController == null)
        {
            return;
        }

        if (formRoot.CurrentForm == PlayerFormType.Plane && ruleController.IsPlaneBlockedByEnvironment())
        {
            formRoot.SetForm(PlayerFormType.Human);
        }
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
