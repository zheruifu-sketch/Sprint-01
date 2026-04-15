using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EndlessLevelGenerator : MonoBehaviour
{
    [Serializable]
    private class SegmentTemplate
    {
        [SerializeField] private string label = "Segment";
        [SerializeField] private GameObject prefab;
        [SerializeField] private float extraSpacing;
        [SerializeField] private float yOffset;
        [SerializeField] private ZoneType zoneType = ZoneType.None;

        public SegmentTemplate()
        {
        }

        public SegmentTemplate(string label, GameObject prefab, ZoneType zoneType, float extraSpacing = 0f, float yOffset = 0f)
        {
            this.label = label;
            this.prefab = prefab;
            this.zoneType = zoneType;
            this.extraSpacing = extraSpacing;
            this.yOffset = yOffset;
        }

        public string Label => string.IsNullOrWhiteSpace(label) && prefab != null ? prefab.name : label;
        public GameObject Prefab => prefab;
        public float ExtraSpacing => extraSpacing;
        public float YOffset => yOffset;
        public ZoneType ZoneType => zoneType;
    }

    [Serializable]
    private class OpeningSegment
    {
        [SerializeField] private SegmentTemplate template = new SegmentTemplate();
        [SerializeField] private int repeatCount = 1;

        public OpeningSegment()
        {
        }

        public OpeningSegment(SegmentTemplate template, int repeatCount)
        {
            this.template = template;
            this.repeatCount = repeatCount;
        }

        public SegmentTemplate Template => template;
        public int RepeatCount => Mathf.Max(1, repeatCount);
    }

    [Serializable]
    private class RandomSegmentRule
    {
        [SerializeField] private SegmentTemplate template = new SegmentTemplate();
        [SerializeField] private float weight = 1f;
        [SerializeField] private int minConsecutiveCount = 1;
        [SerializeField] private int maxConsecutiveCount = 2;
        [SerializeField] private bool canBeFirstRandomSegment = true;
        [SerializeField] private List<ZoneType> allowedPreviousZones = new List<ZoneType>();

        public RandomSegmentRule()
        {
        }

        public RandomSegmentRule(
            SegmentTemplate template,
            float weight,
            int minConsecutiveCount,
            int maxConsecutiveCount,
            bool canBeFirstRandomSegment = true,
            List<ZoneType> allowedPreviousZones = null)
        {
            this.template = template;
            this.weight = weight;
            this.minConsecutiveCount = minConsecutiveCount;
            this.maxConsecutiveCount = maxConsecutiveCount;
            this.canBeFirstRandomSegment = canBeFirstRandomSegment;
            if (allowedPreviousZones != null)
            {
                this.allowedPreviousZones = new List<ZoneType>(allowedPreviousZones);
            }
        }

        public SegmentTemplate Template => template;
        public float Weight => weight;
        public int MinConsecutiveCount => Mathf.Max(1, minConsecutiveCount);
        public int MaxConsecutiveCount => Mathf.Max(MinConsecutiveCount, maxConsecutiveCount);
        public bool CanBeFirstRandomSegment => canBeFirstRandomSegment;
        public List<ZoneType> AllowedPreviousZones => allowedPreviousZones;
    }

    private struct SegmentMeasurement
    {
        public float MinX;
        public float MaxX;
        public float Width;
    }

    private sealed class ActiveSegment
    {
        public GameObject Instance;
        public float RightEdgeX;
        public ZoneType ZoneType;
    }

    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform segmentParent;
    [SerializeField] private GameLevelController levelController;
    [SerializeField] private GameProgressionConfig progressionConfig;

    [Header("Spawn Range")]
    [SerializeField] private float initialLeftEdgeX;
    [SerializeField] private float spawnAheadDistance = 60f;
    [SerializeField] private float destroyBehindDistance = 35f;

    [Header("Startup")]
    [SerializeField] private bool clearChildrenOnPlay = true;
    [SerializeField] private List<OpeningSegment> openingSequence = new List<OpeningSegment>();

    [Header("Random Rules")]
    [SerializeField] private List<RandomSegmentRule> randomRules = new List<RandomSegmentRule>();
    [SerializeField] private int randomSeed = 230;
    [SerializeField] private bool randomizeSeedOnPlay;

    private readonly List<ActiveSegment> activeSegments = new List<ActiveSegment>();
    private readonly Dictionary<GameObject, SegmentMeasurement> measurementCache = new Dictionary<GameObject, SegmentMeasurement>();

    private System.Random rng;
    private float nextLeftEdgeX;
    private RandomSegmentRule lastRandomRule;
    private int lastRandomRuleRepeatCount;
    private bool openingSequenceGenerated;

    private void Reset()
    {
        target = FindPlayerTransform();
        segmentParent = transform;
        levelController = GameLevelController.GetOrCreateInstance();
        progressionConfig = GameProgressionConfig.Load();
        EnsureDefaultRules();
    }

    private void Awake()
    {
        if (segmentParent == null)
        {
            segmentParent = transform;
        }

        if (target == null)
        {
            target = FindPlayerTransform();
        }

        if (levelController == null)
        {
            levelController = GameLevelController.GetOrCreateInstance();
        }

        if (progressionConfig == null)
        {
            progressionConfig = GameProgressionConfig.Load();
        }

        EnsureDefaultRules();
        InitializeRuntimeState();
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
        if (target == null)
        {
            target = FindPlayerTransform();
            if (target == null)
            {
                return;
            }
        }

        EnsureSegmentsAhead();
        RecycleSegmentsBehind();
    }

    [ContextMenu("Reset Rules To Defaults")]
    private void ResetRulesToDefaults()
    {
        openingSequence.Clear();
        openingSequence.Add(CreateOpeningSegment("Start Road", LoadSegmentPrefab("公路"), ZoneType.Road, 3));

        randomRules.Clear();
        randomRules.Add(CreateRandomRule("Road", LoadSegmentPrefab("公路"), ZoneType.Road, 2.2f, 1, 3));
        randomRules.Add(CreateRandomRule("Water", LoadSegmentPrefab("水面"), ZoneType.Water, 1.2f, 1, 2));
        randomRules.Add(CreateRandomRule("Blizzard", LoadSegmentPrefab("雪地"), ZoneType.Blizzard, 1f, 1, 2));
        randomRules.Add(CreateRandomRule("Cliff", LoadSegmentPrefab("悬崖"), ZoneType.Cliff, 0.9f, 1, 2));
    }

    [ContextMenu("Clear Generated Segments")]
    private void ClearGeneratedSegments()
    {
        for (int i = activeSegments.Count - 1; i >= 0; i--)
        {
            GameObject instance = activeSegments[i].Instance;
            if (instance == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
        }

        activeSegments.Clear();
        measurementCache.Clear();
        nextLeftEdgeX = initialLeftEdgeX;
        openingSequenceGenerated = false;
        lastRandomRule = null;
        lastRandomRuleRepeatCount = 0;
    }

    private void InitializeRuntimeState()
    {
        if (randomizeSeedOnPlay)
        {
            randomSeed = Environment.TickCount;
        }

        rng = new System.Random(randomSeed);
        nextLeftEdgeX = initialLeftEdgeX;
        openingSequenceGenerated = false;
        lastRandomRule = null;
        lastRandomRuleRepeatCount = 0;

        ApplyConfiguredRules();

        if (clearChildrenOnPlay)
        {
            ClearChildSegments();
        }
    }

    private void ClearChildSegments()
    {
        Transform parent = segmentParent != null ? segmentParent : transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void EnsureSegmentsAhead()
    {
        float requiredRightEdge = target.position.x + spawnAheadDistance;

        if (!openingSequenceGenerated)
        {
            GenerateOpeningSequence();
        }

        int safetyCounter = 0;
        while (nextLeftEdgeX < requiredRightEdge && safetyCounter < 256)
        {
            RandomSegmentRule rule = ChooseNextRandomRule();
            if (rule == null || rule.Template == null || rule.Template.Prefab == null)
            {
                break;
            }

            SpawnTemplate(rule.Template);
            TrackRandomRuleUsage(rule);
            safetyCounter++;
        }
    }

    private void GenerateOpeningSequence()
    {
        for (int i = 0; i < openingSequence.Count; i++)
        {
            OpeningSegment openingSegment = openingSequence[i];
            if (openingSegment == null || openingSegment.Template == null || openingSegment.Template.Prefab == null)
            {
                continue;
            }

            if (!IsTemplateAllowedForCurrentLevel(openingSegment.Template))
            {
                continue;
            }

            for (int repeat = 0; repeat < openingSegment.RepeatCount; repeat++)
            {
                SpawnTemplate(openingSegment.Template);
            }
        }

        openingSequenceGenerated = true;
    }

    private void RecycleSegmentsBehind()
    {
        float recycleEdgeX = target.position.x - destroyBehindDistance;

        for (int i = activeSegments.Count - 1; i >= 0; i--)
        {
            ActiveSegment segment = activeSegments[i];
            if (segment.Instance == null)
            {
                activeSegments.RemoveAt(i);
                continue;
            }

            if (segment.RightEdgeX >= recycleEdgeX)
            {
                continue;
            }

            Destroy(segment.Instance);
            activeSegments.RemoveAt(i);
        }
    }

    private void SpawnTemplate(SegmentTemplate template)
    {
        GameObject instance = Instantiate(template.Prefab, Vector3.zero, Quaternion.identity, segmentParent);
        SegmentMeasurement measurement = MeasurePrefabInstance(template.Prefab, instance);
        float worldY = transform.position.y + template.YOffset;
        float worldZ = transform.position.z;

        Vector3 position = new Vector3(nextLeftEdgeX - measurement.MinX, worldY, worldZ);
        instance.transform.position = position;

        activeSegments.Add(new ActiveSegment
        {
            Instance = instance,
            RightEdgeX = nextLeftEdgeX + measurement.Width,
            ZoneType = ResolveZoneType(template)
        });

        nextLeftEdgeX += measurement.Width + template.ExtraSpacing;
    }

    private SegmentMeasurement MeasurePrefabInstance(GameObject prefab, GameObject instance)
    {
        if (prefab != null && measurementCache.TryGetValue(prefab, out SegmentMeasurement cached))
        {
            return cached;
        }

        SegmentMeasurement measured = MeasureInstanceFromRootCollider(instance);

        if (prefab != null)
        {
            measurementCache[prefab] = measured;
        }

        return measured;
    }

    private static SegmentMeasurement MeasureInstanceFromRootCollider(GameObject instance)
    {
        Collider2D rootCollider = instance.GetComponent<Collider2D>();
        if (rootCollider == null)
        {
            return new SegmentMeasurement
            {
                MinX = -0.5f,
                MaxX = 0.5f,
                Width = 1f
            };
        }

        float originX = instance.transform.position.x;
        Bounds bounds = rootCollider.bounds;
        float localMinX = bounds.min.x - originX;
        float localMaxX = bounds.max.x - originX;

        return new SegmentMeasurement
        {
            MinX = localMinX,
            MaxX = localMaxX,
            Width = Mathf.Max(0.01f, localMaxX - localMinX)
        };
    }

    private RandomSegmentRule ChooseNextRandomRule()
    {
        if (lastRandomRule != null && lastRandomRuleRepeatCount < lastRandomRule.MinConsecutiveCount)
        {
            return lastRandomRule;
        }

        List<RandomSegmentRule> candidates = CollectCandidates();
        if (candidates.Count == 0)
        {
            return null;
        }

        float totalWeight = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            totalWeight += Mathf.Max(0.01f, candidates[i].Weight);
        }

        float roll = (float)(rng.NextDouble() * totalWeight);
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= Mathf.Max(0.01f, candidates[i].Weight);
            if (roll <= 0f)
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Count - 1];
    }

    private List<RandomSegmentRule> CollectCandidates()
    {
        List<RandomSegmentRule> candidates = new List<RandomSegmentRule>();
        ZoneType previousZone = GetPreviousZoneType();
        bool hasPreviousRandomRule = lastRandomRule != null;

        for (int i = 0; i < randomRules.Count; i++)
        {
            RandomSegmentRule rule = randomRules[i];
            if (rule == null || rule.Template == null || rule.Template.Prefab == null)
            {
                continue;
            }

            if (!IsTemplateAllowedForCurrentLevel(rule.Template))
            {
                continue;
            }

            if (!hasPreviousRandomRule && !rule.CanBeFirstRandomSegment)
            {
                continue;
            }

            if (!AllowsPreviousZone(rule, previousZone))
            {
                continue;
            }

            if (rule == lastRandomRule && lastRandomRuleRepeatCount >= rule.MaxConsecutiveCount)
            {
                continue;
            }

            candidates.Add(rule);
        }

        if (candidates.Count > 0)
        {
            return candidates;
        }

        for (int i = 0; i < randomRules.Count; i++)
        {
            RandomSegmentRule rule = randomRules[i];
            if (rule == null || rule.Template == null || rule.Template.Prefab == null)
            {
                continue;
            }

            if (!IsTemplateAllowedForCurrentLevel(rule.Template))
            {
                continue;
            }

            if (rule == lastRandomRule && lastRandomRuleRepeatCount >= rule.MaxConsecutiveCount)
            {
                continue;
            }

            candidates.Add(rule);
        }

        return candidates;
    }

    private void TrackRandomRuleUsage(RandomSegmentRule chosenRule)
    {
        if (chosenRule == lastRandomRule)
        {
            lastRandomRuleRepeatCount++;
            return;
        }

        lastRandomRule = chosenRule;
        lastRandomRuleRepeatCount = 1;
    }

    private ZoneType GetPreviousZoneType()
    {
        if (activeSegments.Count == 0)
        {
            return ZoneType.None;
        }

        return activeSegments[activeSegments.Count - 1].ZoneType;
    }

    private static bool AllowsPreviousZone(RandomSegmentRule rule, ZoneType previousZone)
    {
        List<ZoneType> allowedPreviousZones = rule.AllowedPreviousZones;
        if (allowedPreviousZones == null || allowedPreviousZones.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < allowedPreviousZones.Count; i++)
        {
            if (allowedPreviousZones[i] == previousZone)
            {
                return true;
            }
        }

        return false;
    }

    private static Transform FindPlayerTransform()
    {
        PlayerFormRoot player = FindObjectOfType<PlayerFormRoot>();
        return player != null ? player.transform : null;
    }

    private void EnsureDefaultRules()
    {
        if (openingSequence.Count == 0 && randomRules.Count == 0)
        {
            ResetRulesToDefaults();
        }
    }

    private void ApplyConfiguredRules()
    {
        if (progressionConfig == null || levelController == null)
        {
            return;
        }

        GameProgressionConfig.LevelDefinition levelDefinition = progressionConfig.GetLevel(levelController.CurrentLevelIndex);
        if (levelDefinition == null)
        {
            return;
        }

        List<GameProgressionConfig.ZoneGenerationRule> configRules = levelDefinition.ZoneGenerationRules;
        if (configRules == null || configRules.Count == 0)
        {
            return;
        }

        openingSequence.Clear();
        GameObject roadPrefab = ResolvePrefabForZone(ZoneType.Road);
        if (roadPrefab != null)
        {
            openingSequence.Add(CreateOpeningSegment("Start Road", roadPrefab, ZoneType.Road, levelDefinition.OpeningRoadRepeatCount));
        }

        randomRules.Clear();
        for (int i = 0; i < configRules.Count; i++)
        {
            GameProgressionConfig.ZoneGenerationRule configRule = configRules[i];
            if (configRule == null)
            {
                continue;
            }

            GameObject prefab = ResolvePrefabForZone(configRule.ZoneType);
            if (prefab == null)
            {
                continue;
            }

            RandomSegmentRule rule = CreateRandomRule(
                configRule.ZoneType.ToString(),
                prefab,
                configRule.ZoneType,
                configRule.Weight,
                configRule.MinConsecutiveCount,
                configRule.MaxConsecutiveCount,
                configRule.CanBeFirstRandomSegment,
                configRule.AllowedPreviousZones);
            randomRules.Add(rule);
        }
    }

    private static OpeningSegment CreateOpeningSegment(string label, GameObject prefab, ZoneType zoneType, int repeatCount)
    {
        return new OpeningSegment(new SegmentTemplate(label, prefab, zoneType), repeatCount);
    }

    private static RandomSegmentRule CreateRandomRule(string label, GameObject prefab, ZoneType zoneType, float weight, int minRepeat, int maxRepeat)
    {
        return new RandomSegmentRule(new SegmentTemplate(label, prefab, zoneType), weight, minRepeat, maxRepeat);
    }

    private static RandomSegmentRule CreateRandomRule(
        string label,
        GameObject prefab,
        ZoneType zoneType,
        float weight,
        int minRepeat,
        int maxRepeat,
        bool canBeFirstRandomSegment,
        List<ZoneType> allowedPreviousZones)
    {
        return new RandomSegmentRule(
            new SegmentTemplate(label, prefab, zoneType),
            weight,
            minRepeat,
            maxRepeat,
            canBeFirstRandomSegment,
            allowedPreviousZones);
    }

    private GameObject LoadSegmentPrefab(string prefabName)
    {
        GameObject loaded = Resources.Load<GameObject>(prefabName);
        if (loaded != null)
        {
            return loaded;
        }

        string assetPath = $"Assets/Prefabs/环境/{prefabName}.prefab";
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
        return null;
#endif
    }

    private GameObject ResolvePrefabForZone(ZoneType zoneType)
    {
        return zoneType switch
        {
            ZoneType.Road => LoadSegmentPrefab("公路"),
            ZoneType.Water => LoadSegmentPrefab("水面"),
            ZoneType.Cliff => LoadSegmentPrefab("悬崖"),
            ZoneType.Blizzard => LoadSegmentPrefab("雪地"),
            _ => null
        };
    }

    private static ZoneType ResolveZoneType(SegmentTemplate template)
    {
        if (template.ZoneType != ZoneType.None)
        {
            return template.ZoneType;
        }

        if (template.Prefab == null)
        {
            return ZoneType.None;
        }

        ZoneDefinition zoneDefinition = template.Prefab.GetComponent<ZoneDefinition>();
        if (zoneDefinition != null)
        {
            return zoneDefinition.ZoneType;
        }

        return template.Prefab.tag switch
        {
            "Road" => ZoneType.Road,
            "Water" => ZoneType.Water,
            "Cliff" => ZoneType.Cliff,
            "Blizzard" => ZoneType.Blizzard,
            "Obstacle" => ZoneType.Obstacle,
            _ => ZoneType.None
        };
    }

    private bool IsTemplateAllowedForCurrentLevel(SegmentTemplate template)
    {
        if (levelController == null)
        {
            return true;
        }

        return levelController.IsZoneAllowed(ResolveZoneType(template));
    }

    private void HandleLevelChanged(int _)
    {
        ClearGeneratedSegments();
        ApplyConfiguredRules();
    }
}
