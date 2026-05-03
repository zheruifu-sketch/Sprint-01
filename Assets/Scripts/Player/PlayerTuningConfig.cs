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
        [SerializeField] private float humanMoveSpeed = 4f;
        [LabelText("汽车移动速度")]
        [SerializeField] private float carMoveSpeed = 7f;
        [LabelText("飞机移动速度")]
        [SerializeField] private float planeMoveSpeed = 6f;
        [LabelText("飞机垂直速度")]
        [SerializeField] private float planeVerticalSpeed = 5f;
        [LabelText("船移动速度")]
        [SerializeField] private float boatMoveSpeed = 4.5f;
        [LabelText("船漂浮高度偏移")]
        [SerializeField] private float boatFloatHeightOffset = 0.85f;
        [LabelText("船漂浮垂直速度")]
        [SerializeField] private float boatFloatVerticalSpeed = 8f;
        [LabelText("船漂浮吸附死区")]
        [SerializeField] private float boatFloatSnapDeadZone = 0.08f;
        [LabelText("船漂浮激活余量")]
        [SerializeField] private float boatFloatActivationMargin = 0.35f;
        [LabelText("默认前进倍率")]
        [SerializeField] private float defaultForwardMultiplier = 1.6f;
        [LabelText("自动前进加速倍率")]
        [SerializeField] private float forwardBoostMultiplier = GameConstants.DefaultForwardBoostMultiplier;
        [LabelText("自动前进减速倍率")]
        [SerializeField] private float forwardBrakeMultiplier = GameConstants.DefaultForwardBrakeMultiplier;
        [LabelText("加速前进每秒额外耗能")]
        [SerializeField] private float forwardBoostEnergyCostPerSecond = GameConstants.DefaultForwardBoostEnergyCostPerSecond;
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
        public float BoatFloatHeightOffset => boatFloatHeightOffset;
        public float BoatFloatVerticalSpeed => boatFloatVerticalSpeed;
        public float BoatFloatSnapDeadZone => boatFloatSnapDeadZone;
        public float BoatFloatActivationMargin => boatFloatActivationMargin;
        public float DefaultForwardMultiplier => Mathf.Max(0.1f, defaultForwardMultiplier);
        public float ForwardBoostMultiplier => forwardBoostMultiplier > 0f
            ? Mathf.Max(1f, forwardBoostMultiplier)
            : GameConstants.DefaultForwardBoostMultiplier;
        public float ForwardBrakeMultiplier => forwardBrakeMultiplier > 0f
            ? Mathf.Clamp(forwardBrakeMultiplier, 0.1f, 1f)
            : GameConstants.DefaultForwardBrakeMultiplier;
        public float ForwardBoostEnergyCostPerSecond => forwardBoostEnergyCostPerSecond > 0f
            ? forwardBoostEnergyCostPerSecond
            : GameConstants.DefaultForwardBoostEnergyCostPerSecond;
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
        [LabelText("变形冷却")]
        [SerializeField] private float transformCooldown = GameConstants.DefaultTransformCooldown;
        [LabelText("飞机受阻时强制切回人形")]
        [SerializeField] private bool forceHumanWhenPlaneBlocked = true;

        public float TransformCooldown => transformCooldown;
        public bool ForceHumanWhenPlaneBlocked => forceHumanWhenPlaneBlocked;
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
        [LabelText("洪水可支撑船的高度")]
        [SerializeField] private float floodBoatSupportHeight = 1.5f;

        public float BlizzardHumanSpeedMultiplier => blizzardHumanSpeedMultiplier;
        public Vector2 BoatSwitchCheckOffset => boatSwitchCheckOffset;
        public float BoatSwitchCheckRadius => boatSwitchCheckRadius;
        public float FloodBoatSupportHeight => floodBoatSupportHeight;
    }

    [Serializable]
    public class SurvivalSettings
    {
        [LabelText("最大生命值")]
        [SerializeField] private float maxHealth = GameConstants.DefaultMaxHealth;
        [LabelText("危险区域每秒伤害")]
        [SerializeField] private float hazardDamagePerSecond = GameConstants.DefaultHazardDamagePerSecond;
        [LabelText("最大能量")]
        [SerializeField] private float maxEnergy = GameConstants.DefaultMaxEnergy;
        [LabelText("汽车每秒能量消耗")]
        [SerializeField] private float carEnergyCostPerSecond = GameConstants.DefaultCarEnergyCostPerSecond;
        [LabelText("飞机每秒能量消耗")]
        [SerializeField] private float planeEnergyCostPerSecond = GameConstants.DefaultPlaneEnergyCostPerSecond;
        [LabelText("船每秒能量消耗")]
        [SerializeField] private float boatEnergyCostPerSecond = GameConstants.DefaultBoatEnergyCostPerSecond;
        [LabelText("每次变形能量消耗")]
        [SerializeField] private float transformEnergyCost = GameConstants.DefaultTransformEnergyCost;

        public float MaxHealth => Mathf.Max(1f, maxHealth);
        public float HazardDamagePerSecond => Mathf.Max(0f, hazardDamagePerSecond);
        public float MaxEnergy => Mathf.Max(1f, maxEnergy);
        public float CarEnergyCostPerSecond => Mathf.Max(0f, carEnergyCostPerSecond);
        public float PlaneEnergyCostPerSecond => Mathf.Max(0f, planeEnergyCostPerSecond);
        public float BoatEnergyCostPerSecond => Mathf.Max(0f, boatEnergyCostPerSecond);
        public float TransformEnergyCost => Mathf.Max(0f, transformEnergyCost);
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
