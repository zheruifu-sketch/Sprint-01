using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerFormController : MonoBehaviour
{
    [Header("References")]
    [LabelText("玩家形态根节点")]
    [SerializeField] private PlayerFormRoot formRoot;
    [LabelText("输入读取器")]
    [SerializeField] private PlayerInputReader inputReader;
    [LabelText("环境规则控制器")]
    [SerializeField] private PlayerRuleController ruleController;
    [LabelText("燃油控制器")]
    [SerializeField] private PlayerFuelController fuelController;
    [LabelText("Buff控制器")]
    [SerializeField] private PlayerBuffController buffController;
    [LabelText("关卡控制器")]
    [SerializeField] private GameLevelController levelController;
    [LabelText("玩家调参配置")]
    [SerializeField] private PlayerTuningConfig tuningConfig;

    private void Reset()
    {
        CacheReferences();
        levelController = FindObjectOfType<GameLevelController>();
        tuningConfig = PlayerTuningConfig.Load();
    }

    private void Awake()
    {
        CacheReferences();

        if (levelController == null)
        {
            levelController = FindObjectOfType<GameLevelController>();
        }

        if (tuningConfig == null)
        {
            tuningConfig = PlayerTuningConfig.Load();
        }
    }

    private void CacheReferences()
    {
        formRoot = formRoot != null ? formRoot : GetComponent<PlayerFormRoot>();
        inputReader = inputReader != null ? inputReader : GetComponent<PlayerInputReader>();
        ruleController = ruleController != null ? ruleController : GetComponent<PlayerRuleController>();
        fuelController = fuelController != null ? fuelController : GetComponent<PlayerFuelController>();
        buffController = buffController != null ? buffController : GetComponent<PlayerBuffController>();
    }

    private void OnEnable()
    {
        if (levelController == null)
        {
            levelController = FindObjectOfType<GameLevelController>();
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
        HandleRequestedForm();
        ApplyForcedFormRules();
    }

    private void HandleRequestedForm()
    {
        if (inputReader == null || !inputReader.RequestedFormThisFrame.HasValue)
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
        SoundEffectPlayback.Play(SoundEffectId.Transform);
        if (fuelController != null)
        {
            fuelController.ConsumeForTransform();
        }

        ApplyTransformBuff();
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

    private void ApplyTransformBuff()
    {
        if (buffController == null)
        {
            return;
        }

        float duration = tuningConfig != null
            ? tuningConfig.Form.TransformInvulnerabilityDuration
            : GameConstants.DefaultTransformInvulnerabilityDuration;
        if (duration <= 0f)
        {
            return;
        }

        buffController.ApplyTimedBuff(PlayerBuffType.Shield, duration);
    }
}
