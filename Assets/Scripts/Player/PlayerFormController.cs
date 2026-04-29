using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerFormController : MonoBehaviour
{
    [Header("References")]
    [LabelText("玩家运行时上下文")]
    [SerializeField] private PlayerRuntimeContext runtimeContext;
    [LabelText("玩家形态根节点")]
    [SerializeField] private PlayerFormRoot formRoot;
    [LabelText("输入读取器")]
    [SerializeField] private PlayerInputReader inputReader;
    [LabelText("环境规则控制器")]
    [SerializeField] private PlayerRuleController ruleController;
    [LabelText("关卡控制器")]
    [SerializeField] private GameLevelController levelController;
    [LabelText("玩家调参配置")]
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
        runtimeContext = GetComponent<PlayerRuntimeContext>();
        SyncFromContext();
        levelController = GameLevelController.GetOrCreateInstance();
        tuningConfig = PlayerTuningConfig.Load();
    }

    private void Awake()
    {
        runtimeContext = runtimeContext != null ? runtimeContext : GetComponent<PlayerRuntimeContext>();
        SyncFromContext();

        if (levelController == null)
        {
            levelController = GameLevelController.GetOrCreateInstance();
        }

        if (tuningConfig == null)
        {
            tuningConfig = runtimeContext != null ? runtimeContext.TuningConfig : PlayerTuningConfig.Load();
        }
    }

    private void SyncFromContext()
    {
        if (runtimeContext == null)
        {
            return;
        }

        runtimeContext.RefreshReferences();
        formRoot = formRoot != null ? formRoot : runtimeContext.FormRoot;
        inputReader = inputReader != null ? inputReader : runtimeContext.InputReader;
        ruleController = ruleController != null ? ruleController : runtimeContext.RuleController;
        tuningConfig = tuningConfig != null ? tuningConfig : runtimeContext.TuningConfig;
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
