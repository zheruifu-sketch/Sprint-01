using System;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[CreateAssetMenu(fileName = "PlayerTuningConfig", menuName = "JumpGame/Player Tuning Config")]
public class PlayerTuningConfig : ScriptableObject
{
    [Serializable]
    public class MovementSettings
    {
        [LabelText("人形移动速度")]
        [SerializeField] private float humanMoveSpeed = 5f;
        [LabelText("汽车移动速度")]
        [SerializeField] private float carMoveSpeed = 9f;
        [LabelText("飞机移动速度")]
        [SerializeField] private float planeMoveSpeed = 8f;
        [LabelText("飞机垂直速度")]
        [SerializeField] private float planeVerticalSpeed = 7f;
        [LabelText("船移动速度")]
        [SerializeField] private float boatMoveSpeed = 6f;
        [LabelText("默认前进倍率")]
        [SerializeField] private float defaultForwardMultiplier = 1.6f;
        [LabelText("自动前进加速倍率")]
        [SerializeField] private float forwardBoostMultiplier = GameConstants.DefaultForwardBoostMultiplier;
        [LabelText("自动前进减速倍率")]
        [SerializeField] private float forwardBrakeMultiplier = GameConstants.DefaultForwardBrakeMultiplier;
        [LabelText("加速前进每秒额外耗油")]
        [SerializeField] private float forwardBoostFuelCostPerSecond = GameConstants.DefaultForwardBoostFuelCostPerSecond;
        [LabelText("人形跳跃力度")]
        [SerializeField] private float humanJumpForce = 9f;
        [LabelText("人形重力")]
        [SerializeField] private float humanGravityScale = 3f;
        [LabelText("汽车重力")]
        [SerializeField] private float carGravityScale = 4f;
        [LabelText("飞机重力")]
        [SerializeField] private float planeGravityScale = 0f;
        [LabelText("船重力")]
        [SerializeField] private float boatGravityScale = 3f;
        [LabelText("跳跃缓冲时间")]
        [SerializeField] private float jumpBufferTime = GameConstants.DefaultJumpBufferTime;
        [LabelText("土狼时间")]
        [SerializeField] private float coyoteTime = GameConstants.DefaultCoyoteTime;
        [LabelText("最大连续跳跃次数")]
        [SerializeField] private int maxHumanJumpCount = 2;
        [LabelText("落地判定纵向速度阈值")]
        [SerializeField] private float groundedVelocityThreshold = 0.1f;
        [LabelText("水平加速度")]
        [SerializeField] private float horizontalAcceleration = 45f;
        [LabelText("水平减速度")]
        [SerializeField] private float horizontalDeceleration = 60f;
        [LabelText("空中控制倍率")]
        [SerializeField] private float airControlMultiplier = 0.55f;
        [LabelText("短跳倍率")]
        [SerializeField] private float shortJumpMultiplier = 0.45f;

        public float HumanMoveSpeed => humanMoveSpeed;
        public float CarMoveSpeed => carMoveSpeed;
        public float PlaneMoveSpeed => planeMoveSpeed;
        public float PlaneVerticalSpeed => planeVerticalSpeed;
        public float BoatMoveSpeed => boatMoveSpeed;
        public float DefaultForwardMultiplier => Mathf.Max(0.1f, defaultForwardMultiplier);
        public float ForwardBoostMultiplier => forwardBoostMultiplier > 0f
            ? Mathf.Max(1f, forwardBoostMultiplier)
            : GameConstants.DefaultForwardBoostMultiplier;
        public float ForwardBrakeMultiplier => forwardBrakeMultiplier > 0f
            ? Mathf.Clamp(forwardBrakeMultiplier, 0.1f, 1f)
            : GameConstants.DefaultForwardBrakeMultiplier;
        public float ForwardBoostFuelCostPerSecond => forwardBoostFuelCostPerSecond > 0f
            ? forwardBoostFuelCostPerSecond
            : GameConstants.DefaultForwardBoostFuelCostPerSecond;
        public float HumanJumpForce => humanJumpForce;
        public float HumanGravityScale => humanGravityScale;
        public float CarGravityScale => carGravityScale;
        public float PlaneGravityScale => planeGravityScale;
        public float BoatGravityScale => boatGravityScale;
        public float JumpBufferTime => jumpBufferTime;
        public float CoyoteTime => coyoteTime;
        public int MaxHumanJumpCount => Mathf.Max(1, maxHumanJumpCount);
        public float GroundedVelocityThreshold => groundedVelocityThreshold;
        public float HorizontalAcceleration => Mathf.Max(0.01f, horizontalAcceleration);
        public float HorizontalDeceleration => Mathf.Max(0.01f, horizontalDeceleration);
        public float AirControlMultiplier => Mathf.Clamp01(airControlMultiplier);
        public float ShortJumpMultiplier => Mathf.Clamp(shortJumpMultiplier, 0.1f, 1f);
    }

    [Serializable]
    public class FormSettings
    {
        [LabelText("飞机受阻时强制切回人形")]
        [SerializeField] private bool forceHumanWhenPlaneBlocked = true;
        [LabelText("通用变形无敌时长")]
        [SerializeField] private float transformInvulnerabilityDuration = GameConstants.DefaultTransformInvulnerabilityDuration;

        public bool ForceHumanWhenPlaneBlocked => forceHumanWhenPlaneBlocked;
        public float TransformInvulnerabilityDuration => Mathf.Max(0f, transformInvulnerabilityDuration);
    }

    [Serializable]
    public class EnvironmentRuleSettings
    {
        [LabelText("暴雪中人形速度倍率")]
        [SerializeField] private float blizzardHumanSpeedMultiplier = 0.3f;
        [LabelText("切船检测偏移")]
        [SerializeField] private Vector2 boatSwitchCheckOffset = GameConstants.DefaultBoatSwitchCheckOffset;
        [LabelText("切船检测半径")]
        [SerializeField] private float boatSwitchCheckRadius = GameConstants.DefaultBoatSwitchCheckRadius;

        public float BlizzardHumanSpeedMultiplier => blizzardHumanSpeedMultiplier;
        public Vector2 BoatSwitchCheckOffset => boatSwitchCheckOffset;
        public float BoatSwitchCheckRadius => boatSwitchCheckRadius;
    }

    [Serializable]
    public class SurvivalSettings
    {
        [LabelText("最大生命值")]
        [SerializeField] private float maxHealth = GameConstants.DefaultMaxHealth;
        [LabelText("危险区域每秒伤害")]
        [SerializeField] private float hazardDamagePerSecond = GameConstants.DefaultHazardDamagePerSecond;
        [LabelText("最大燃油")]
        [SerializeField] private float maxFuel = GameConstants.DefaultMaxFuel;
        [LabelText("汽车每秒耗油")]
        [SerializeField] private float carFuelCostPerSecond = GameConstants.DefaultCarFuelCostPerSecond;
        [LabelText("飞机每秒耗油")]
        [SerializeField] private float planeFuelCostPerSecond = GameConstants.DefaultPlaneFuelCostPerSecond;
        [LabelText("船每秒耗油")]
        [SerializeField] private float boatFuelCostPerSecond = GameConstants.DefaultBoatFuelCostPerSecond;
        [LabelText("每次变形耗油")]
        [SerializeField] private float transformFuelCost = GameConstants.DefaultTransformFuelCost;

        public float MaxHealth => Mathf.Max(1f, maxHealth);
        public float HazardDamagePerSecond => Mathf.Max(0f, hazardDamagePerSecond);
        public float MaxFuel => Mathf.Max(1f, maxFuel);
        public float CarFuelCostPerSecond => Mathf.Max(0f, carFuelCostPerSecond);
        public float PlaneFuelCostPerSecond => Mathf.Max(0f, planeFuelCostPerSecond);
        public float BoatFuelCostPerSecond => Mathf.Max(0f, boatFuelCostPerSecond);
        public float TransformFuelCost => Mathf.Max(0f, transformFuelCost);
    }

    [LabelText("移动参数")]
    [SerializeField] private MovementSettings movement = new MovementSettings();
    [LabelText("形态参数")]
    [SerializeField] private FormSettings form = new FormSettings();
    [LabelText("环境规则参数")]
    [SerializeField] private EnvironmentRuleSettings environmentRules = new EnvironmentRuleSettings();
    [LabelText("生存参数")]
    [SerializeField] private SurvivalSettings survival = new SurvivalSettings();

    private static PlayerTuningConfig cachedConfig;

    public MovementSettings Movement => movement;
    public FormSettings Form => form;
    public EnvironmentRuleSettings EnvironmentRules => environmentRules;
    public SurvivalSettings Survival => survival;

    public static PlayerTuningConfig Load()
    {
        if (cachedConfig != null)
        {
            return cachedConfig;
        }

        cachedConfig = Resources.Load<PlayerTuningConfig>("GameConfig/PlayerTuningConfig");
        return cachedConfig;
    }
}
