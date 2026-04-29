using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[CreateAssetMenu(fileName = "GameProgressionConfig", menuName = "JumpGame/Game Progression Config")]
public class GameProgressionConfig : ScriptableObject
{
    [Serializable]
    public class ZoneGenerationRule
    {
        [FormerlySerializedAs("zoneType")]
        [LabelText("环境类型")]
        [SerializeField] private EnvironmentType environmentType = EnvironmentType.None;
        [LabelText("权重")]
        [SerializeField] private float weight = 1f;
        [LabelText("最少连续出现次数")]
        [SerializeField] private int minConsecutiveCount = 1;
        [LabelText("最多连续出现次数")]
        [SerializeField] private int maxConsecutiveCount = 2;
        [LabelText("是否可作为随机段首个路段")]
        [SerializeField] private bool canBeFirstRandomSegment = true;
        [FormerlySerializedAs("allowedPreviousZones")]
        [LabelText("允许接在这些环境后面")]
        [SerializeField] private List<EnvironmentType> allowedPreviousEnvironments = new List<EnvironmentType>();

        public EnvironmentType EnvironmentType => environmentType;
        public float Weight => Mathf.Max(0.01f, weight);
        public int MinConsecutiveCount => Mathf.Max(1, minConsecutiveCount);
        public int MaxConsecutiveCount => Mathf.Max(MinConsecutiveCount, maxConsecutiveCount);
        public bool CanBeFirstRandomSegment => canBeFirstRandomSegment;
        public List<EnvironmentType> AllowedPreviousEnvironments => allowedPreviousEnvironments;
    }

    [Serializable]
    public class LevelDefinition
    {
        [Serializable]
        public class PickupSpawnSettings
        {
            [LabelText("启用拾取物生成")]
            [SerializeField] private bool enabled = true;
            [LabelText("最小生成间距")]
            [SerializeField] private float minSpawnDistance = 14f;
            [LabelText("生成概率")]
            [SerializeField] private float spawnChance = 0.55f;
            [LabelText("场上最大拾取物数量")]
            [SerializeField] private int maxActivePickups = 3;
            [LabelText("距离玩家最小前方距离")]
            [SerializeField] private float minSpawnAheadDistance = 8f;
            [LabelText("距离玩家最大前方距离")]
            [SerializeField] private float maxSpawnAheadDistance = 18f;
            [LabelText("生成高度偏移")]
            [SerializeField] private float yOffset = 1.1f;
            [LabelText("可生成拾取物列表")]
            [SerializeField] private List<PickupProfile> profiles = new List<PickupProfile>();

            public bool Enabled => enabled;
            public float MinSpawnDistance => Mathf.Max(1f, minSpawnDistance);
            public float SpawnChance => Mathf.Clamp01(spawnChance);
            public int MaxActivePickups => Mathf.Max(0, maxActivePickups);
            public float MinSpawnAheadDistance => Mathf.Max(0f, minSpawnAheadDistance);
            public float MaxSpawnAheadDistance => Mathf.Max(MinSpawnAheadDistance, maxSpawnAheadDistance);
            public float YOffset => yOffset;
            public List<PickupProfile> Profiles => profiles;
        }

        [LabelText("关卡名称")]
        [SerializeField] private string levelName = "Level";
        [LabelText("关卡说明")]
        [SerializeField] private string description = string.Empty;
        [LabelText("目标距离")]
        [SerializeField] private float targetDistance = 45f;
        [LabelText("开始提示")]
        [SerializeField] private string startHint = string.Empty;
        [LabelText("通关提示")]
        [SerializeField] private string clearHint = string.Empty;
        [LabelText("开场公路重复次数")]
        [SerializeField] private int openingRoadRepeatCount = 3;
        [LabelText("解锁形态")]
        [SerializeField] private List<PlayerFormType> unlockedForms = new List<PlayerFormType>();
        [FormerlySerializedAs("allowedZones")]
        [LabelText("允许出现的环境")]
        [SerializeField] private List<EnvironmentType> allowedEnvironments = new List<EnvironmentType>();
        [LabelText("旧版环境生成规则")]
        [SerializeField] private List<ZoneGenerationRule> zoneGenerationRules = new List<ZoneGenerationRule>();
        [LabelText("关卡灾害")]
        [SerializeField] private List<HazardProfile> hazards = new List<HazardProfile>();
        [LabelText("拾取物生成")]
        [SerializeField] private PickupSpawnSettings pickups = new PickupSpawnSettings();

        public string LevelName => string.IsNullOrWhiteSpace(levelName) ? "Level" : levelName;
        public string Description => description;
        public float TargetDistance => Mathf.Max(1f, targetDistance);
        public string StartHint => startHint;
        public string ClearHint => clearHint;
        public int OpeningRoadRepeatCount => Mathf.Max(1, openingRoadRepeatCount);
        public List<PlayerFormType> UnlockedForms => unlockedForms;
        public List<EnvironmentType> AllowedEnvironments => allowedEnvironments;
        public List<ZoneGenerationRule> ZoneGenerationRules => zoneGenerationRules;
        public List<HazardProfile> Hazards => hazards;
        public PickupSpawnSettings Pickups => pickups;
    }

    [Header("Flow")]
    [LabelText("默认起始关卡")]
    [SerializeField] private int defaultStartingLevel = 1;
    [LabelText("关卡切换延迟")]
    [SerializeField] private float transitionDelay = 1.25f;
    [LabelText("启用调试热键")]
    [SerializeField] private bool enableDebugHotkeys = true;

    [Header("Levels")]
    [LabelText("关卡列表")]
    [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();

    private static GameProgressionConfig cachedConfig;

    public int DefaultStartingLevel => Mathf.Max(1, defaultStartingLevel);
    public float TransitionDelay => Mathf.Max(0f, transitionDelay);
    public bool EnableDebugHotkeys => enableDebugHotkeys;
    public int LevelCount => levels != null ? levels.Count : 0;

    public static GameProgressionConfig Load()
    {
        if (cachedConfig != null)
        {
            return cachedConfig;
        }

        cachedConfig = Resources.Load<GameProgressionConfig>("GameConfig/GameProgressionConfig");
        return cachedConfig;
    }

    public LevelDefinition GetLevel(int levelIndex)
    {
        if (levels == null || levels.Count == 0)
        {
            return null;
        }

        int clampedIndex = Mathf.Clamp(levelIndex, 0, levels.Count - 1);
        return levels[clampedIndex];
    }

    public bool IsFormUnlocked(int levelIndex, PlayerFormType formType)
    {
        LevelDefinition level = GetLevel(levelIndex);
        if (level == null || level.UnlockedForms == null || level.UnlockedForms.Count == 0)
        {
            return true;
        }

        return level.UnlockedForms.Contains(formType);
    }

    public bool IsEnvironmentAllowed(int levelIndex, EnvironmentType environmentType)
    {
        LevelDefinition level = GetLevel(levelIndex);
        if (level == null || level.AllowedEnvironments == null || level.AllowedEnvironments.Count == 0)
        {
            return true;
        }

        return level.AllowedEnvironments.Contains(environmentType);
    }

    public PlayerFormType GetFallbackUnlockedForm(int levelIndex, PlayerFormType preferredForm = PlayerFormType.Human)
    {
        LevelDefinition level = GetLevel(levelIndex);
        if (level == null || level.UnlockedForms == null || level.UnlockedForms.Count == 0)
        {
            return preferredForm;
        }

        if (level.UnlockedForms.Contains(preferredForm))
        {
            return preferredForm;
        }

        return level.UnlockedForms[0];
    }
}
