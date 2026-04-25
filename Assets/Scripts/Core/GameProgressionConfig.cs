using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GameProgressionConfig", menuName = "JumpGame/Game Progression Config")]
public class GameProgressionConfig : ScriptableObject
{
    [Serializable]
    public class ZoneGenerationRule
    {
        [FormerlySerializedAs("zoneType")]
        [SerializeField] private EnvironmentType environmentType = EnvironmentType.None;
        [SerializeField] private float weight = 1f;
        [SerializeField] private int minConsecutiveCount = 1;
        [SerializeField] private int maxConsecutiveCount = 2;
        [SerializeField] private bool canBeFirstRandomSegment = true;
        [FormerlySerializedAs("allowedPreviousZones")]
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
            [SerializeField] private bool enabled = true;
            [SerializeField] private float minSpawnDistance = 14f;
            [SerializeField] private float spawnChance = 0.55f;
            [SerializeField] private int maxActivePickups = 3;
            [SerializeField] private float minSpawnAheadDistance = 8f;
            [SerializeField] private float maxSpawnAheadDistance = 18f;
            [SerializeField] private float yOffset = 1.1f;
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

        [SerializeField] private string levelName = "Level";
        [SerializeField] private string description = string.Empty;
        [SerializeField] private float targetDistance = 45f;
        [SerializeField] private string startHint = string.Empty;
        [SerializeField] private string clearHint = string.Empty;
        [SerializeField] private int openingRoadRepeatCount = 3;
        [SerializeField] private LevelPatternDefinition patternDefinition;
        [SerializeField] private List<PlayerFormType> unlockedForms = new List<PlayerFormType>();
        [FormerlySerializedAs("allowedZones")]
        [SerializeField] private List<EnvironmentType> allowedEnvironments = new List<EnvironmentType>();
        [SerializeField] private List<ZoneGenerationRule> zoneGenerationRules = new List<ZoneGenerationRule>();
        [SerializeField] private List<HazardProfile> hazards = new List<HazardProfile>();
        [SerializeField] private PickupSpawnSettings pickups = new PickupSpawnSettings();

        public string LevelName => string.IsNullOrWhiteSpace(levelName) ? "Level" : levelName;
        public string Description => description;
        public float TargetDistance => Mathf.Max(1f, targetDistance);
        public string StartHint => startHint;
        public string ClearHint => clearHint;
        public int OpeningRoadRepeatCount => patternDefinition != null
            ? patternDefinition.OpeningRoadRepeatCount
            : Mathf.Max(1, openingRoadRepeatCount);
        public LevelPatternDefinition PatternDefinition => patternDefinition;
        public List<PlayerFormType> UnlockedForms => unlockedForms;
        public List<EnvironmentType> AllowedEnvironments => allowedEnvironments;
        public List<ZoneGenerationRule> ZoneGenerationRules => zoneGenerationRules;
        public List<HazardProfile> Hazards => hazards;
        public PickupSpawnSettings Pickups => pickups;

        public IEnumerable<LevelPatternDefinition.EnvironmentGenerationRule> EnumerateEnvironmentRules()
        {
            if (patternDefinition != null && patternDefinition.EnvironmentRules != null && patternDefinition.EnvironmentRules.Count > 0)
            {
                return patternDefinition.EnvironmentRules;
            }

            return Array.Empty<LevelPatternDefinition.EnvironmentGenerationRule>();
        }
    }

    [Header("Flow")]
    [SerializeField] private int defaultStartingLevel = 1;
    [SerializeField] private float transitionDelay = 1.25f;
    [SerializeField] private bool enableDebugHotkeys = true;

    [Header("Levels")]
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
