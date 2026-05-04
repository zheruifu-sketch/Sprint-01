using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class EndlessLevelGenerator : MonoBehaviour
{
    [Serializable]
    private class OpeningSegment
    {
        [LabelText("路段模板")]
        [SerializeField] private SegmentTemplateConfig template = new SegmentTemplateConfig();
        [LabelText("重复次数")]
        [SerializeField] private int repeatCount = 1;

        public OpeningSegment()
        {
        }

        public OpeningSegment(SegmentTemplateConfig template, int repeatCount)
        {
            this.template = template;
            this.repeatCount = repeatCount;
        }

        public SegmentTemplateConfig Template => template;
        public int RepeatCount => Mathf.Max(1, repeatCount);
    }

    [Serializable]
    private class RandomSegmentRule
    {
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

        public RandomSegmentRule()
        {
        }

        public RandomSegmentRule(
            EnvironmentType environmentType,
            float weight,
            int minConsecutiveCount,
            int maxConsecutiveCount,
            bool canBeFirstRandomSegment = true,
            List<EnvironmentType> allowedPreviousEnvironments = null)
        {
            this.environmentType = environmentType;
            this.weight = weight;
            this.minConsecutiveCount = minConsecutiveCount;
            this.maxConsecutiveCount = maxConsecutiveCount;
            this.canBeFirstRandomSegment = canBeFirstRandomSegment;
            if (allowedPreviousEnvironments != null)
            {
                this.allowedPreviousEnvironments = new List<EnvironmentType>(allowedPreviousEnvironments);
            }
        }

        public EnvironmentType EnvironmentType => environmentType;
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
        public SegmentDescriptor Descriptor;
        public float LeftEdgeX;
        public float RightEdgeX;
        public EnvironmentType EnvironmentType;
    }

    [Header("References")]
    [LabelText("玩家目标")]
    [SerializeField] private Transform target;
    [LabelText("路段父节点")]
    [SerializeField] private Transform segmentParent;
    [LabelText("关卡控制器")]
    [SerializeField] private GameLevelController levelController;
    [LabelText("流程配置")]
    [SerializeField] private GameProgressionConfig progressionConfig;
    [LabelText("路段变体配置")]
    [SerializeField] private SegmentVariantConfig segmentVariantConfig;

    [Header("Spawn Range")]
    [LabelText("初始左边界 X")]
    [SerializeField] private float initialLeftEdgeX;
    [LabelText("向前生成距离")]
    [SerializeField] private float spawnAheadDistance = 60f;
    [LabelText("向后回收距离")]
    [SerializeField] private float destroyBehindDistance = 35f;

    [Header("Startup")]
    [LabelText("运行时清空已有子物体")]
    [SerializeField] private bool clearChildrenOnPlay = true;

    private readonly List<OpeningSegment> openingSequence = new List<OpeningSegment>();
    private readonly List<RandomSegmentRule> randomRules = new List<RandomSegmentRule>();

    [Header("Random Rules")]
    [LabelText("随机种子")]
    [SerializeField] private int randomSeed = 230;
    [LabelText("运行时随机化种子")]
    [SerializeField] private bool randomizeSeedOnPlay;

    private readonly List<ActiveSegment> activeSegments = new List<ActiveSegment>();
    private readonly Dictionary<GameObject, SegmentMeasurement> measurementCache = new Dictionary<GameObject, SegmentMeasurement>();
    private readonly List<Transform> anchorBuffer = new List<Transform>();

    private System.Random rng;
    private float nextLeftEdgeX;
    private bool openingSequenceGenerated;
    private RandomSegmentRule currentRunRule;
    private SegmentTemplateConfig currentRunTemplate;
    private int currentRunRemainingCount;
    private bool hasGeneratedRandomSegments;

    private void Reset()
    {
        target = FindPlayerTransform();
        segmentParent = transform;
        levelController = FindObjectOfType<GameLevelController>();
        progressionConfig = GameProgressionConfig.Load();
        segmentVariantConfig = SegmentVariantConfig.Load();
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
            levelController = FindObjectOfType<GameLevelController>();
        }

        if (progressionConfig == null)
        {
            progressionConfig = GameProgressionConfig.Load();
        }

        if (segmentVariantConfig == null)
        {
            segmentVariantConfig = SegmentVariantConfig.Load();
        }

        EnsureDefaultRules();
        InitializeRuntimeState();
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
        SegmentTemplateConfig openingRoadTemplate = ResolveOpeningTemplate(EnvironmentType.Road);
        if (openingRoadTemplate != null && openingRoadTemplate.Prefab != null)
        {
            openingSequence.Add(new OpeningSegment(openingRoadTemplate, 3));
        }

        randomRules.Clear();
        randomRules.Add(CreateRandomRule(EnvironmentType.Road, 2.2f, 1, 3));
        randomRules.Add(CreateRandomRule(EnvironmentType.Water, 1.2f, 1, 2));
        randomRules.Add(CreateRandomRule(EnvironmentType.Blizzard, 1f, 1, 2));
        randomRules.Add(CreateRandomRule(EnvironmentType.Cliff, 0.9f, 1, 2));
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
        currentRunRule = null;
        currentRunTemplate = null;
        currentRunRemainingCount = 0;
        hasGeneratedRandomSegments = false;
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
        currentRunRule = null;
        currentRunTemplate = null;
        currentRunRemainingCount = 0;
        hasGeneratedRandomSegments = false;

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
        EnsureSegmentsThrough(requiredRightEdge);
    }

    public void EnsureGeneratedToWorldX(float worldX)
    {
        float requiredRightEdge = Mathf.Max(worldX, target != null ? target.position.x + spawnAheadDistance : worldX);
        EnsureSegmentsThrough(requiredRightEdge);
    }

    private void EnsureSegmentsThrough(float requiredRightEdge)
    {
        if (!openingSequenceGenerated)
        {
            GenerateOpeningSequence();
        }

        int safetyCounter = 0;
        while (nextLeftEdgeX < requiredRightEdge && safetyCounter < 256)
        {
            if (!EnsureCurrentRun())
            {
                break;
            }

            SpawnTemplate(currentRunTemplate);
            currentRunRemainingCount--;
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

    private void SpawnTemplate(SegmentTemplateConfig template)
    {
        GameObject instance = Instantiate(template.Prefab, Vector3.zero, Quaternion.identity, segmentParent);
        SegmentMeasurement measurement = MeasurePrefabInstance(template.Prefab, instance);
        SegmentDescriptor descriptor = instance.GetComponent<SegmentDescriptor>();
        float worldY = transform.position.y + template.YOffset;
        float worldZ = transform.position.z;

        Vector3 position = new Vector3(nextLeftEdgeX - measurement.MinX, worldY, worldZ);
        instance.transform.position = position;

        activeSegments.Add(new ActiveSegment
        {
            Instance = instance,
            Descriptor = descriptor,
            LeftEdgeX = nextLeftEdgeX,
            RightEdgeX = nextLeftEdgeX + measurement.Width,
            EnvironmentType = ResolveEnvironmentType(template)
        });

        nextLeftEdgeX += measurement.Width + template.ExtraSpacing;
    }

    public bool TryGetRandomAnchorPoint(
        SegmentAnchorType anchorType,
        EnvironmentType environmentType,
        float minX,
        float maxX,
        float yOffset,
        out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        anchorBuffer.Clear();

        for (int i = 0; i < activeSegments.Count; i++)
        {
            ActiveSegment segment = activeSegments[i];
            if (segment == null || segment.Instance == null)
            {
                continue;
            }

            if (segment.EnvironmentType != environmentType)
            {
                continue;
            }

            if (segment.RightEdgeX < minX || segment.LeftEdgeX > maxX)
            {
                continue;
            }

            if (segment.Descriptor != null)
            {
                segment.Descriptor.CollectAnchorsInRange(anchorType, minX, maxX, anchorBuffer);
            }
        }

        if (anchorBuffer.Count > 0)
        {
            Transform selectedAnchor = anchorBuffer[UnityEngine.Random.Range(0, anchorBuffer.Count)];
            if (selectedAnchor != null)
            {
                spawnPosition = selectedAnchor.position + new Vector3(0f, yOffset, 0f);
                anchorBuffer.Clear();
                return true;
            }
        }

        anchorBuffer.Clear();

        List<ActiveSegment> fallbackSegments = new List<ActiveSegment>();
        for (int i = 0; i < activeSegments.Count; i++)
        {
            ActiveSegment segment = activeSegments[i];
            if (segment == null || segment.Instance == null)
            {
                continue;
            }

            if (segment.EnvironmentType != environmentType)
            {
                continue;
            }

            if (segment.RightEdgeX < minX || segment.LeftEdgeX > maxX)
            {
                continue;
            }

            fallbackSegments.Add(segment);
        }

        if (fallbackSegments.Count == 0)
        {
            return false;
        }

        ActiveSegment selectedSegment = fallbackSegments[UnityEngine.Random.Range(0, fallbackSegments.Count)];
        if (!TryMeasureSurfaceBounds(selectedSegment.Instance, out Bounds bounds))
        {
            return false;
        }

        float spawnMinX = Mathf.Max(minX, bounds.min.x + 0.75f);
        float spawnMaxX = Mathf.Min(maxX, bounds.max.x - 0.75f);
        if (spawnMaxX <= spawnMinX)
        {
            return false;
        }

        spawnPosition = new Vector3(
            UnityEngine.Random.Range(spawnMinX, spawnMaxX),
            bounds.max.y + yOffset,
            0f);
        return true;
    }

    public bool TryGetRandomPickupSpawnPoint(EnvironmentType environmentType, float minX, float maxX, float yOffset, out Vector3 spawnPosition)
    {
        return TryGetRandomAnchorPoint(SegmentAnchorType.Pickup, environmentType, minX, maxX, yOffset, out spawnPosition);
    }

    public bool TryGetClosestPickupSpawnPoint(EnvironmentType environmentType, float targetX, float maxOffsetX, float yOffset, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        float minX = targetX - Mathf.Max(0f, maxOffsetX);
        float maxX = targetX + Mathf.Max(0f, maxOffsetX);

        anchorBuffer.Clear();
        for (int i = 0; i < activeSegments.Count; i++)
        {
            ActiveSegment segment = activeSegments[i];
            if (segment == null || segment.Instance == null || segment.EnvironmentType != environmentType)
            {
                continue;
            }

            if (segment.RightEdgeX < minX || segment.LeftEdgeX > maxX)
            {
                continue;
            }

            if (segment.Descriptor != null)
            {
                segment.Descriptor.CollectAnchorsInRange(SegmentAnchorType.Pickup, minX, maxX, anchorBuffer);
            }
        }

        Transform closestAnchor = null;
        float closestDistance = float.MaxValue;
        for (int i = 0; i < anchorBuffer.Count; i++)
        {
            Transform anchor = anchorBuffer[i];
            if (anchor == null)
            {
                continue;
            }

            float distance = Mathf.Abs(anchor.position.x - targetX);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestAnchor = anchor;
            }
        }

        if (closestAnchor != null)
        {
            spawnPosition = closestAnchor.position + new Vector3(0f, yOffset, 0f);
            anchorBuffer.Clear();
            return true;
        }

        anchorBuffer.Clear();

        ActiveSegment fallbackSegment = null;
        float fallbackDistance = float.MaxValue;
        for (int i = 0; i < activeSegments.Count; i++)
        {
            ActiveSegment segment = activeSegments[i];
            if (segment == null || segment.Instance == null || segment.EnvironmentType != environmentType)
            {
                continue;
            }

            if (segment.RightEdgeX < minX || segment.LeftEdgeX > maxX)
            {
                continue;
            }

            float segmentDistance = Mathf.Abs(Mathf.Clamp(targetX, segment.LeftEdgeX, segment.RightEdgeX) - targetX);
            if (segmentDistance < fallbackDistance)
            {
                fallbackDistance = segmentDistance;
                fallbackSegment = segment;
            }
        }

        if (fallbackSegment == null || !TryMeasureSurfaceBounds(fallbackSegment.Instance, out Bounds bounds))
        {
            return false;
        }

        float spawnX = Mathf.Clamp(targetX, bounds.min.x + 0.75f, bounds.max.x - 0.75f);
        spawnPosition = new Vector3(spawnX, bounds.max.y + yOffset, 0f);
        return true;
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

    private static bool TryMeasureSurfaceBounds(GameObject instance, out Bounds bounds)
    {
        Collider2D zoneCollider = FindZoneCollider(instance);
        if (zoneCollider != null && zoneCollider.enabled)
        {
            bounds = zoneCollider.bounds;
            return true;
        }

        Collider2D[] colliders = instance.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            bounds = collider.bounds;
            return true;
        }

        bounds = default;
        return false;
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

    private bool EnsureCurrentRun()
    {
        if (currentRunRule != null && currentRunTemplate != null && currentRunRemainingCount > 0)
        {
            return true;
        }

        RandomSegmentRule rule = ChooseNextRandomRule();
        if (rule == null)
        {
            currentRunRule = null;
            currentRunTemplate = null;
            currentRunRemainingCount = 0;
            return false;
        }

        SegmentTemplateConfig template = ResolveRandomTemplate(rule.EnvironmentType);
        if (template == null || template.Prefab == null)
        {
            currentRunRule = null;
            currentRunTemplate = null;
            currentRunRemainingCount = 0;
            return false;
        }

        currentRunRule = rule;
        currentRunTemplate = template;
        currentRunRemainingCount = rng.Next(rule.MinConsecutiveCount, rule.MaxConsecutiveCount + 1);
        hasGeneratedRandomSegments = true;
        return true;
    }

    private RandomSegmentRule ChooseNextRandomRule()
    {
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

        for (int i = 0; i < randomRules.Count; i++)
        {
            RandomSegmentRule rule = randomRules[i];
            if (rule == null || !HasAvailableTemplate(rule.EnvironmentType))
            {
                continue;
            }

            if (!IsEnvironmentAllowedForCurrentLevel(rule.EnvironmentType))
            {
                continue;
            }

            if (!hasGeneratedRandomSegments && !rule.CanBeFirstRandomSegment)
            {
                continue;
            }

            if (!AllowsPreviousEnvironment(rule, previousEnvironment))
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
            if (rule == null || !HasAvailableTemplate(rule.EnvironmentType))
            {
                continue;
            }

            if (!IsEnvironmentAllowedForCurrentLevel(rule.EnvironmentType))
            {
                continue;
            }

            if (!hasGeneratedRandomSegments && !rule.CanBeFirstRandomSegment)
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
        PlayerRuntimeContext runtimeContext = PlayerRuntimeContext.FindInScene();
        if (runtimeContext != null && runtimeContext.FormRoot != null)
        {
            return runtimeContext.FormRoot.transform;
        }

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

        if (levelDefinition.ZoneGenerationRules == null || levelDefinition.ZoneGenerationRules.Count == 0)
        {
            return;
        }

        openingSequence.Clear();
        SegmentTemplateConfig openingRoadTemplate = ResolveOpeningTemplate(EnvironmentType.Road);
        if (openingRoadTemplate != null && openingRoadTemplate.Prefab != null)
        {
            openingSequence.Add(new OpeningSegment(openingRoadTemplate, levelDefinition.OpeningRoadRepeatCount));
        }

        randomRules.Clear();
        for (int i = 0; i < levelDefinition.ZoneGenerationRules.Count; i++)
        {
            GameProgressionConfig.ZoneGenerationRule configRule = levelDefinition.ZoneGenerationRules[i];
            if (configRule == null)
            {
                continue;
            }

            if (!HasAvailableTemplate(configRule.EnvironmentType))
            {
                continue;
            }

            RandomSegmentRule rule = CreateRandomRule(
                configRule.EnvironmentType,
                configRule.Weight,
                configRule.MinConsecutiveCount,
                configRule.MaxConsecutiveCount,
                configRule.CanBeFirstRandomSegment,
                configRule.AllowedPreviousEnvironments);
            randomRules.Add(rule);
        }
    }

    private static RandomSegmentRule CreateRandomRule(EnvironmentType environmentType, float weight, int minRepeat, int maxRepeat)
    {
        return new RandomSegmentRule(environmentType, weight, minRepeat, maxRepeat);
    }

    private static RandomSegmentRule CreateRandomRule(
        EnvironmentType environmentType,
        float weight,
        int minRepeat,
        int maxRepeat,
        bool canBeFirstRandomSegment,
        List<EnvironmentType> allowedPreviousEnvironments)
    {
        return new RandomSegmentRule(
            environmentType,
            weight,
            minRepeat,
            maxRepeat,
            canBeFirstRandomSegment,
            allowedPreviousEnvironments);
    }

    private SegmentTemplateConfig ResolveOpeningTemplate(EnvironmentType environmentType)
    {
        EnvironmentVariantSetConfig variantSet = FindVariantSet(environmentType);
        if (variantSet == null)
        {
            return null;
        }

        return GetFirstUsableTemplate(variantSet.NormalVariants) ?? GetFirstUsableTemplate(variantSet.SpecialVariants);
    }

    private static EnvironmentType ResolveEnvironmentType(SegmentTemplateConfig template)
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

    private bool IsTemplateAllowedForCurrentLevel(SegmentTemplateConfig template)
    {
        if (levelController == null)
        {
            return true;
        }

        return levelController.IsEnvironmentAllowed(ResolveEnvironmentType(template));
    }

    private bool IsEnvironmentAllowedForCurrentLevel(EnvironmentType environmentType)
    {
        if (levelController == null)
        {
            return true;
        }

        return levelController.IsEnvironmentAllowed(environmentType);
    }

    private void HandleLevelChanged(int _)
    {
        ClearGeneratedSegments();
        ApplyConfiguredRules();
    }

    private SegmentTemplateConfig ResolveRandomTemplate(EnvironmentType environmentType)
    {
        EnvironmentVariantSetConfig variantSet = FindVariantSet(environmentType);
        if (variantSet == null)
        {
            return null;
        }

        bool canUseSpecial = HasAnyUsableTemplate(variantSet.SpecialVariants);
        bool useSpecial = canUseSpecial && rng.NextDouble() < variantSet.SpecialVariantChance;
        List<SegmentTemplateConfig> source = useSpecial ? variantSet.SpecialVariants : variantSet.NormalVariants;

        SegmentTemplateConfig selected = GetRandomUsableTemplate(source);
        if (selected != null)
        {
            return selected;
        }

        source = useSpecial ? variantSet.NormalVariants : variantSet.SpecialVariants;
        return GetRandomUsableTemplate(source);
    }

    private EnvironmentVariantSetConfig FindVariantSet(EnvironmentType environmentType)
    {
        if (segmentVariantConfig == null || segmentVariantConfig.EnvironmentVariants == null)
        {
            return null;
        }

        for (int i = 0; i < segmentVariantConfig.EnvironmentVariants.Count; i++)
        {
            EnvironmentVariantSetConfig variantSet = segmentVariantConfig.EnvironmentVariants[i];
            if (variantSet != null && variantSet.EnvironmentType == environmentType)
            {
                return variantSet;
            }
        }

        return null;
    }

    private bool HasAvailableTemplate(EnvironmentType environmentType)
    {
        EnvironmentVariantSetConfig variantSet = FindVariantSet(environmentType);
        if (variantSet == null)
        {
            return false;
        }

        return HasAnyUsableTemplate(variantSet.NormalVariants) || HasAnyUsableTemplate(variantSet.SpecialVariants);
    }

    private static bool HasAnyUsableTemplate(List<SegmentTemplateConfig> templates)
    {
        return GetFirstUsableTemplate(templates) != null;
    }

    private static SegmentTemplateConfig GetFirstUsableTemplate(List<SegmentTemplateConfig> templates)
    {
        if (templates == null)
        {
            return null;
        }

        for (int i = 0; i < templates.Count; i++)
        {
            SegmentTemplateConfig template = templates[i];
            if (template != null && template.Prefab != null)
            {
                return template;
            }
        }

        return null;
    }

    private SegmentTemplateConfig GetRandomUsableTemplate(List<SegmentTemplateConfig> templates)
    {
        if (templates == null || templates.Count == 0)
        {
            return null;
        }

        List<SegmentTemplateConfig> candidates = new List<SegmentTemplateConfig>();
        for (int i = 0; i < templates.Count; i++)
        {
            SegmentTemplateConfig template = templates[i];
            if (template == null || template.Prefab == null)
            {
                continue;
            }

            if (!IsTemplateAllowedForCurrentLevel(template))
            {
                continue;
            }

            candidates.Add(template);
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[rng.Next(0, candidates.Count)];
    }

}
