using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class PlayerRuntimeContext : MonoBehaviour
{
    [Header("Core")]
    [LabelText("玩家形态根节点")]
    [SerializeField] private PlayerFormRoot formRoot;
    [LabelText("输入读取器")]
    [SerializeField] private PlayerInputReader inputReader;
    [LabelText("移动控制器")]
    [SerializeField] private PlayerMovementController movementController;
    [LabelText("形态控制器")]
    [SerializeField] private PlayerFormController formController;

    [Header("Rules")]
    [LabelText("环境规则控制器")]
    [SerializeField] private PlayerRuleController ruleController;
    [LabelText("环境感知上下文")]
    [SerializeField] private PlayerEnvironmentContext environmentContext;
    [LabelText("地面检测器")]
    [SerializeField] private PlayerGroundSensor groundSensor;
    [LabelText("危险解析器")]
    [SerializeField] private PlayerHazardResolver hazardResolver;

    [Header("Survival")]
    [LabelText("生命控制器")]
    [SerializeField] private PlayerHealthController healthController;
    [LabelText("燃油控制器")]
    [SerializeField] private PlayerFuelController fuelController;
    [LabelText("Buff控制器")]
    [SerializeField] private PlayerBuffController buffController;
    [LabelText("闪烁特效控制器")]
    [SerializeField] private PlayerFlashEffectController flashEffectController;
    [LabelText("重生控制器")]
    [SerializeField] private PlayerRespawnController respawnController;
    [LabelText("玩家调参配置")]
    [SerializeField] private PlayerTuningConfig tuningConfig;

    public PlayerFormRoot FormRoot => formRoot;
    public PlayerInputReader InputReader => inputReader;
    public PlayerMovementController MovementController => movementController;
    public PlayerFormController FormController => formController;
    public PlayerRuleController RuleController => ruleController;
    public PlayerEnvironmentContext EnvironmentContext => environmentContext;
    public PlayerGroundSensor GroundSensor => groundSensor;
    public PlayerHazardResolver HazardResolver => hazardResolver;
    public PlayerHealthController HealthController => healthController;
    public PlayerFuelController FuelController => fuelController;
    public PlayerBuffController BuffController => buffController;
    public PlayerFlashEffectController FlashEffectController => flashEffectController;
    public PlayerRespawnController RespawnController => respawnController;
    public GameSessionController SessionController => GameSessionController.Instance;
    public PlayerTuningConfig TuningConfig => tuningConfig;

    public static PlayerRuntimeContext FindInScene()
    {
        return FindObjectOfType<PlayerRuntimeContext>();
    }

    public static PlayerRuntimeContext ResolveFromComponent(Component component)
    {
        if (component == null)
        {
            return null;
        }

        return component.GetComponentInParent<PlayerRuntimeContext>();
    }

    private void Reset()
    {
        RefreshReferences();
    }

    private void Awake()
    {
        RefreshReferences();
    }

    public void RefreshReferences()
    {
        formRoot = formRoot != null ? formRoot : GetComponent<PlayerFormRoot>();
        inputReader = inputReader != null ? inputReader : GetComponent<PlayerInputReader>();
        movementController = movementController != null ? movementController : GetComponent<PlayerMovementController>();
        formController = formController != null ? formController : GetComponent<PlayerFormController>();
        ruleController = ruleController != null ? ruleController : GetComponent<PlayerRuleController>();
        environmentContext = environmentContext != null ? environmentContext : GetComponent<PlayerEnvironmentContext>();
        groundSensor = groundSensor != null ? groundSensor : GetComponent<PlayerGroundSensor>();
        hazardResolver = hazardResolver != null ? hazardResolver : GetComponent<PlayerHazardResolver>();
        healthController = healthController != null ? healthController : GetComponent<PlayerHealthController>();
        fuelController = fuelController != null ? fuelController : GetComponent<PlayerFuelController>();
        buffController = buffController != null ? buffController : GetComponent<PlayerBuffController>();
        flashEffectController = flashEffectController != null ? flashEffectController : GetComponent<PlayerFlashEffectController>();
        respawnController = respawnController != null ? respawnController : GetComponent<PlayerRespawnController>();
        tuningConfig = tuningConfig != null ? tuningConfig : PlayerTuningConfig.Load();
    }
}
