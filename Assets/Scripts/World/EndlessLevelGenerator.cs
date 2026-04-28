using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("zoneType")]
        [SerializeField] private EnvironmentType environmentType = EnvironmentType.None;

        public SegmentTemplate()
        {
        }

        public SegmentTemplate(string label, GameObject prefab, EnvironmentType environmentType, float extraSpacing = 0f, float yOffset = 0f)
        {
            this.label = label;
            this.prefab = prefab;
            this.environmentType = environmentType;
            this.extraSpacing = extraSpacing;
            this.yOffset = yOffset;
        }

        public string Label => string.IsNullOrWhiteSpace(label) && prefab != null ? prefab.name : label;
        public GameObject Prefab => prefab;
        public float ExtraSpacing => extraSpacing;
        public float YOffset => yOffset;
        public EnvironmentType EnvironmentType => environmentType;
    }

    [Serializable]
    private class EnvironmentSegmentEntry
    {
        [SerializeField] private EnvironmentType environmentType = EnvironmentType.None;
        [SerializeField] private GameObject prefab;

        public EnvironmentSegmentEntry()
        {
        }

        public EnvironmentSegmentEntry(EnvironmentType environmentType, GameObject prefab)
        {
            this.environmentType = environmentType;
            this.prefab = prefab;
        }

        public EnvironmentType EnvironmentType => environmentType;
        public GameObject Prefab => prefab;
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
        [FormerlySerializedAs("allowedPreviousZones")]
        [SerializeField] private List<EnvironmentType> allowedPreviousEnvironments = new List<EnvironmentType>();

        public RandomSegmentRule()
        {
        }

        public RandomSegmentRule(
            SegmentTemplate template,
            float weight,
            int minConsecutiveCount,
            int maxConsecutiveCount,
            bool canBeFirstRandomSegment = true,
            List<EnvironmentType> allowedPreviousEnvironments = null)
        {
            this.template = template;
            this.weight = weight;
            this.minConsecutiveCount = minConsecutiveCount;
            this.maxConsecutiveCount = maxConsecutiveCount;
            this.canBeFirstRandomSegment = canBeFirstRandomSegment;
            if (allowedPreviousEnvironments != null)
            {
                this.allowedPreviousEnvironments = new List<EnvironmentType>(allowedPreviousEnvironments);
            }
        }

        public SegmentTemplate Template => template;
        public float Weight => weight;
        public int MinConsecutiveCount => Mathf.Max(1, minConsecutiveCount);
        public int MaxConsecutiveCount => Mathf.Max(MinConsecutiveCount, maxConsecutiveCount);
        public bool CanBeFirstRandomSegment => canBeFirstRandomSegment;
        public List<EnvironmentType> AllowedPreviousEnvironments => allowedPreviousEnvironments;
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
        public EnvironmentType EnvironmentType;
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

    [Header("Segment Library")]
    [SerializeField] private List<EnvironmentSegmentEntry> segmentLibrary = new List<EnvironmentSegmentEntry>();

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
        EnsureDefaultSegmentLibrary();
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

        EnsureDefaultSegmentLibrary();
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
        openingSequence.Add(CreateOpeningSegment("Start Road", LoadSegmentPrefab("公路"), EnvironmentType.Road, 3));

        randomRules.Clear();
        randomRules.Add(CreateRandomRule("Road", LoadSegmentPrefab("公路"), EnvironmentType.Road, 2.2f, 1, 3));
        randomRules.Add(CreateRandomRule("Water", LoadSegmentPrefab("水面"), EnvironmentType.Water, 1.2f, 1, 2));
        randomRules.Add(CreateRandomRule("Blizzard", LoadSegmentPrefab("雪地"), EnvironmentType.Blizzard, 1f, 1, 2));
        randomRules.Add(CreateRandomRule("Cliff", LoadSegmentPrefab("悬崖"), EnvironmentType.Cliff, 0.9f, 1, 2));
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
            EnvironmentType = ResolveEnvironmentType(template)
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
        Collider2D zoneCollider = FindZoneCollider(instance);
        if (zoneCollider != null && zoneCollider.enabled)
        {
            return MeasureFromCollider(instance.transform.position.x, zoneCollider);
        }

        Collider2D[] colliders = instance.GetComponentsInChildren<Collider2D>(true);
        if (colliders == null || colliders.Length == 0)
        {
            return CreateFallbackMeasurement();
        }

        float originX = instance.transform.position.x;
        bool found = false;
        float localMinX = 0f;
        float localMaxX = 0f;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            Bounds bounds = collider.bounds;
            float colliderMinX = bounds.min.x - originX;
            float colliderMaxX = bounds.max.x - originX;

            if (!found)
            {
                localMinX = colliderMinX;
                localMaxX = colliderMaxX;
                found = true;
            }
            else
            {
                localMinX = Mathf.Min(localMinX, colliderMinX);
                localMaxX = Mathf.Max(localMaxX, colliderMaxX);
            }
        }

        if (!found)
        {
            return CreateFallbackMeasurement();
        }

        return new SegmentMeasurement
        {
            MinX = localMinX,
            MaxX = localMaxX,
            Width = Mathf.Max(0.01f, localMaxX - localMinX)
        };
    }

    private static Collider2D FindZoneCollider(GameObject instance)
    {
        ZoneDefinition zoneDefinition = instance.GetComponentInChildren<ZoneDefinition>(true);
        return zoneDefinition != null ? zoneDefinition.GetComponent<Collider2D>() : null;
    }

    private static SegmentMeasurement MeasureFromCollider(float originX, Collider2D collider)
    {
        Bounds bounds = collider.bounds;
        float localMinX = bounds.min.x - originX;
        float localMaxX = bounds.max.x - originX;

        return new SegmentMeasurement
        {
            MinX = localMinX,
            MaxX = localMaxX,
            Width = Mathf.Max(0.01f, localMaxX - localMinX)
        };
    }

    private static SegmentMeasurement CreateFallbackMeasurement()
    {
        return new SegmentMeasurement
        {
            MinX = -0.5f,
            MaxX = 0.5f,
            Width = 1f
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
        EnvironmentType previousEnvironment = GetPreviousEnvironmentType();
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

            if (!AllowsPreviousEnvironment(rule, previousEnvironment))
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

            if (!hasPreviousRandomRule && !rule.CanBeFirstRandomSegment)
            {
                continue;
            }

            if (!AllowsPreviousEnvironment(rule, previousEnvironment))
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

    private EnvironmentType GetPreviousEnvironmentType()
    {
        if (activeSegments.Count == 0)
        {
            return EnvironmentType.None;
        }

        return activeSegments[activeSegments.Count - 1].EnvironmentType;
    }

    private static bool AllowsPreviousEnvironment(RandomSegmentRule rule, EnvironmentType previousEnvironment)
    {
        List<EnvironmentType> allowedPreviousEnvironments = rule.AllowedPreviousEnvironments;
        if (allowedPreviousEnvironments == null || allowedPreviousEnvironments.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < allowedPreviousEnvironments.Count; i++)
        {
            if (allowedPreviousEnvironments[i] == previousEnvironment)
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

    private void EnsureDefaultSegmentLibrary()
    {
        if (segmentLibrary.Count > 0 && HasConfiguredSegmentLibrary())
        {
            return;
        }

        segmentLibrary.Clear();
        AddSegmentLibraryEntry(EnvironmentType.Road, "公路");
        AddSegmentLibraryEntry(EnvironmentType.Water, "水面");
        AddSegmentLibraryEntry(EnvironmentType.Cliff, "悬崖");
        AddSegmentLibraryEntry(EnvironmentType.Blizzard, "雪地");
    }

    private bool HasConfiguredSegmentLibrary()
    {
        for (int i = 0; i < segmentLibrary.Count; i++)
        {
            EnvironmentSegmentEntry entry = segmentLibrary[i];
            if (entry != null && entry.EnvironmentType != EnvironmentType.None && entry.Prefab != null)
            {
                return true;
            }
        }

        return false;
    }

    private void AddSegmentLibraryEntry(EnvironmentType environmentType, string prefabName)
    {
        GameObject prefab = LoadSegmentPrefab(prefabName);
        if (prefab == null)
        {
            return;
        }

        segmentLibrary.Add(new EnvironmentSegmentEntry(environmentType, prefab));
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

        List<LevelPatternDefinition.EnvironmentGenerationRule> configRules = new List<LevelPatternDefinition.EnvironmentGenerationRule>(levelDefinition.EnumerateEnvironmentRules());
        if (configRules.Count == 0 && levelDefinition.ZoneGenerationRules != null)
        {
            for (int i = 0; i < levelDefinition.ZoneGenerationRules.Count; i++)
            {
                GameProgressionConfig.ZoneGenerationRule legacyRule = levelDefinition.ZoneGenerationRules[i];
                if (legacyRule == null)
                {
                    continue;
                }

                configRules.Add(new LevelPatternDefinition.EnvironmentGenerationRule(
                    legacyRule.EnvironmentType,
                    legacyRule.Weight,
                    legacyRule.MinConsecutiveCount,
                    legacyRule.MaxConsecutiveCount,
                    legacyRule.CanBeFirstRandomSegment,
                    legacyRule.AllowedPreviousEnvironments));
            }
        }

        if (configRules.Count == 0)
        {
            return;
        }

        openingSequence.Clear();
        GameObject roadPrefab = ResolvePrefabForEnvironment(EnvironmentType.Road);
        if (roadPrefab != null)
        {
            openingSequence.Add(CreateOpeningSegment("Start Road", roadPrefab, EnvironmentType.Road, levelDefinition.OpeningRoadRepeatCount));
        }

        randomRules.Clear();
        for (int i = 0; i < configRules.Count; i++)
        {
            LevelPatternDefinition.EnvironmentGenerationRule configRule = configRules[i];
            if (configRule == null)
            {
                continue;
            }

            GameObject prefab = ResolvePrefabForEnvironment(configRule.EnvironmentType);
            if (prefab == null)
            {
                continue;
            }

            RandomSegmentRule rule = CreateRandomRule(
                configRule.EnvironmentType.ToString(),
                prefab,
                configRule.EnvironmentType,
                configRule.Weight,
                configRule.MinConsecutiveCount,
                configRule.MaxConsecutiveCount,
                configRule.CanBeFirstRandomSegment,
                configRule.AllowedPreviousEnvironments);
            randomRules.Add(rule);
        }
    }

    private static OpeningSegment CreateOpeningSegment(string label, GameObject prefab, EnvironmentType environmentType, int repeatCount)
    {
        return new OpeningSegment(new SegmentTemplate(label, prefab, environmentType), repeatCount);
    }

    private static RandomSegmentRule CreateRandomRule(string label, GameObject prefab, EnvironmentType environmentType, float weight, int minRepeat, int maxRepeat)
    {
        return new RandomSegmentRule(new SegmentTemplate(label, prefab, environmentType), weight, minRepeat, maxRepeat);
    }

    private static RandomSegmentRule CreateRandomRule(
        string label,
        GameObject prefab,
        EnvironmentType environmentType,
        float weight,
        int minRepeat,
        int maxRepeat,
        bool canBeFirstRandomSegment,
        List<EnvironmentType> allowedPreviousEnvironments)
    {
        return new RandomSegmentRule(
            new SegmentTemplate(label, prefab, environmentType),
            weight,
            minRepeat,
            maxRepeat,
            canBeFirstRandomSegment,
            allowedPreviousEnvironments);
    }

    private GameObject LoadSegmentPrefab(string prefabName)
    {
        GameObject loaded = Resources.Load<GameObject>($"Segments/{prefabName}");
        if (loaded != null)
        {
            return loaded;
        }

        loaded = Resources.Load<GameObject>(prefabName);
        if (loaded != null)
        {
            return loaded;
        }

#if UNITY_EDITOR
        string assetPath = $"Assets/Resources/Segments/{prefabName}.prefab";
        GameObject editorLoaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (editorLoaded != null)
        {
            return editorLoaded;
        }

        assetPath = $"Assets/Prefabs/环境/{prefabName}.prefab";
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
        return null;
#endif
    }

    private GameObject ResolvePrefabForEnvironment(EnvironmentType environmentType)
    {
        for (int i = 0; i < segmentLibrary.Count; i++)
        {
            EnvironmentSegmentEntry entry = segmentLibrary[i];
            if (entry == null || entry.EnvironmentType != environmentType || entry.Prefab == null)
            {
                continue;
            }

            return entry.Prefab;
        }

        return null;
    }

    private static EnvironmentType ResolveEnvironmentType(SegmentTemplate template)
    {
        if (template.EnvironmentType != EnvironmentType.None)
        {
            return template.EnvironmentType;
        }

        if (template.Prefab == null)
        {
            return EnvironmentType.None;
        }

        SegmentDescriptor segmentDescriptor = template.Prefab.GetComponent<SegmentDescriptor>();
        if (segmentDescriptor != null && segmentDescriptor.PrimaryEnvironment != EnvironmentType.None)
        {
            return segmentDescriptor.PrimaryEnvironment;
        }

        Collider2D collider = template.Prefab.GetComponentInChildren<Collider2D>(true);
        if (WorldSemanticUtility.TryResolveEnvironment(collider, out EnvironmentType environmentType))
        {
            return environmentType;
        }

        return WorldSemanticUtility.ResolveEnvironmentFromTag(template.Prefab.tag);
    }

    private bool IsTemplateAllowedForCurrentLevel(SegmentTemplate template)
    {
        if (levelController == null)
        {
            return true;
        }

        return levelController.IsEnvironmentAllowed(ResolveEnvironmentType(template));
    }

    private void HandleLevelChanged(int _)
    {
        ClearGeneratedSegments();
        ApplyConfiguredRules();
    }

}
