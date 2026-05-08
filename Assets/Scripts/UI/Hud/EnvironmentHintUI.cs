using System.Collections.Generic;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class EnvironmentHintUI : HudUIBase
{
    private struct HintRequest
    {
        public string Key;
        public string Message;
        public float Duration;
    }

    private struct SpecialRoadHintState
    {
        public SpecialRoadSegmentBase Segment;
        public bool IsActive;
    }

    [Header("References")]
    [LabelText("玩家运行时上下文")]
    [SerializeField] private PlayerRuntimeContext runtimeContext;
    [LabelText("玩家形态根节点")]
    [SerializeField] private PlayerFormRoot playerFormRoot;
    [LabelText("环境感知上下文")]
    [SerializeField] private PlayerEnvironmentContext environmentContext;
    [LabelText("提示条界面")]
    [SerializeField] private HintBarUI hintBarUi;
    [LabelText("关卡控制器")]
    [SerializeField] private GameLevelController levelController;
    [LabelText("提示配置")]
    [SerializeField] private GameMessageCatalog messageCatalog;

    [Header("Intro Hint")]
    [LabelText("引导延迟")]
    [SerializeField] private float introHintDelay = 0.6f;

    [Header("Hint Queue")]
    [LabelText("提示间隔")]
    [SerializeField] private float hintGapDuration = 0.15f;

    private readonly Dictionary<EnvironmentType, bool> previousZoneStates = new Dictionary<EnvironmentType, bool>();
    private readonly HashSet<EnvironmentType> shownZoneHints = new HashSet<EnvironmentType>();
    private readonly Dictionary<string, bool> previousSpecialRoadStates = new Dictionary<string, bool>();
    private readonly HashSet<string> shownSpecialRoadHints = new HashSet<string>();
    private readonly Dictionary<string, SpecialRoadHintState> specialRoadStates = new Dictionary<string, SpecialRoadHintState>();
    private readonly Queue<HintRequest> pendingHints = new Queue<HintRequest>();
    private readonly HashSet<string> queuedHintKeys = new HashSet<string>();
    private readonly Collider2D[] specialRoadResults = new Collider2D[24];
    private float introHintTimer;
    private float nextHintAvailableTime;
    private bool introHintQueued;
    private bool startHintsQueued;

    protected override void Reset()
    {
        base.Reset();
        AutoBind();
    }

    protected override void Awake()
    {
        base.Awake();
        AutoBind();
        PrimeZoneStates();
        introHintTimer = introHintDelay;
        introHintQueued = messageCatalog != null
                          && messageCatalog.ShowIntroHintOnStart
                          && !string.IsNullOrWhiteSpace(messageCatalog.IntroTransformHintTemplate);
    }

    public override void Initialize()
    {
        AutoBind();
        PrimeZoneStates();
    }

    private void Update()
    {
        UpdateIntroHint();
        UpdateZoneHints();
        UpdateSpecialRoadHints();
        FlushPendingHints();
    }

    private void AutoBind()
    {
        runtimeContext = runtimeContext != null ? runtimeContext : PlayerRuntimeContext.FindInScene();
        if (runtimeContext != null)
        {
            runtimeContext.RefreshReferences();
            playerFormRoot = playerFormRoot != null ? playerFormRoot : runtimeContext.FormRoot;
            environmentContext = environmentContext != null ? environmentContext : runtimeContext.EnvironmentContext;
        }

        if (playerFormRoot == null)
        {
            playerFormRoot = FindObjectOfType<PlayerFormRoot>();
        }

        if (environmentContext == null && playerFormRoot != null)
        {
            environmentContext = playerFormRoot.GetComponent<PlayerEnvironmentContext>();
        }

        if (hintBarUi == null)
        {
            UIManager uiManager = UIManager.Instance != null ? UIManager.Instance : FindObjectOfType<UIManager>(true);
            hintBarUi = uiManager != null ? uiManager.Get<HintBarUI>() : null;
            hintBarUi = hintBarUi != null ? hintBarUi : FindObjectOfType<HintBarUI>(true);
        }

        if (levelController == null)
        {
            levelController = FindObjectOfType<GameLevelController>();
        }

        if (messageCatalog == null)
        {
            messageCatalog = GameMessageCatalog.Load();
        }
    }

    private void PrimeZoneStates()
    {
        previousZoneStates.Clear();
        if (messageCatalog == null || messageCatalog.EnvironmentHints == null)
        {
            return;
        }

        for (int i = 0; i < messageCatalog.EnvironmentHints.Count; i++)
        {
            GameMessageCatalog.EnvironmentHintEntry entry = messageCatalog.EnvironmentHints[i];
            if (entry == null || entry.EnvironmentType == EnvironmentType.None)
            {
                continue;
            }

            previousZoneStates[entry.EnvironmentType] = false;
        }
    }

    private void UpdateIntroHint()
    {
        if (!introHintQueued || messageCatalog == null)
        {
            return;
        }

        introHintTimer -= Time.deltaTime;
        if (introHintTimer > 0f)
        {
            return;
        }

        introHintQueued = false;
        EnqueueHint("intro", ResolveIntroHintMessage(), messageCatalog.IntroHintDuration);
    }

    private void UpdateZoneHints()
    {
        if (environmentContext == null || messageCatalog == null || messageCatalog.EnvironmentHints == null)
        {
            return;
        }

        // Environment hints now pull from the shared catalog so the queue/display
        // logic stays here while all actual player-facing text lives in one asset.
        if (!startHintsQueued)
        {
            QueueCurrentEnvironmentHintsAtStart();
            startHintsQueued = true;
        }

        for (int i = 0; i < messageCatalog.EnvironmentHints.Count; i++)
        {
            GameMessageCatalog.EnvironmentHintEntry entry = messageCatalog.EnvironmentHints[i];
            if (entry == null || entry.EnvironmentType == EnvironmentType.None || string.IsNullOrWhiteSpace(entry.Message))
            {
                continue;
            }

            bool isInside = IsPlayerInEnvironment(entry.EnvironmentType);
            bool wasInside = previousZoneStates.TryGetValue(entry.EnvironmentType, out bool value) && value;

            if (isInside && !wasInside && ShouldShowZoneHint(entry))
            {
                EnqueueHint($"env:{entry.EnvironmentType}", entry.Message, entry.Duration);
                if (entry.ShowOnlyOnce)
                {
                    shownZoneHints.Add(entry.EnvironmentType);
                }
            }

            previousZoneStates[entry.EnvironmentType] = isInside;
        }
    }

    private bool ShouldShowZoneHint(GameMessageCatalog.EnvironmentHintEntry entry)
    {
        if (entry == null)
        {
            return false;
        }

        if (entry.ShowOnlyOnce && shownZoneHints.Contains(entry.EnvironmentType))
        {
            return false;
        }

        if (entry.SuppressWhenAlreadyUsingRecommendedForm
            && playerFormRoot != null
            && playerFormRoot.CurrentForm == entry.RecommendedForm)
        {
            return false;
        }

        return true;
    }

    private bool IsPlayerInEnvironment(EnvironmentType environmentType)
    {
        return environmentContext != null && environmentContext.IsInOrPreviewingEnvironment(environmentType);
    }

    private void QueueCurrentEnvironmentHintsAtStart()
    {
        if (messageCatalog == null || messageCatalog.EnvironmentHints == null)
        {
            return;
        }

        for (int i = 0; i < messageCatalog.EnvironmentHints.Count; i++)
        {
            GameMessageCatalog.EnvironmentHintEntry entry = messageCatalog.EnvironmentHints[i];
            if (entry == null || entry.EnvironmentType == EnvironmentType.None || string.IsNullOrWhiteSpace(entry.Message))
            {
                continue;
            }

            bool isVisible = IsPlayerInEnvironment(entry.EnvironmentType);
            previousZoneStates[entry.EnvironmentType] = isVisible;
            if (!isVisible || !ShouldShowZoneHint(entry))
            {
                continue;
            }

            EnqueueHint($"env:{entry.EnvironmentType}", entry.Message, entry.Duration);
            if (entry.ShowOnlyOnce)
            {
                shownZoneHints.Add(entry.EnvironmentType);
            }
        }
    }

    private void UpdateSpecialRoadHints()
    {
        if (playerFormRoot == null)
        {
            return;
        }

        CollectSpecialRoadStates();

        foreach (KeyValuePair<string, SpecialRoadHintState> pair in specialRoadStates)
        {
            string hintId = pair.Key;
            SpecialRoadHintState state = pair.Value;
            bool wasActive = previousSpecialRoadStates.TryGetValue(hintId, out bool value) && value;

            if (state.IsActive && !wasActive && ShouldShowSpecialRoadHint(state.Segment))
            {
                EnqueueHint($"special:{hintId}", state.Segment.FirstEncounterHintMessage, state.Segment.HintDuration);
                if (state.Segment.ShowHintOnlyOnce)
                {
                    shownSpecialRoadHints.Add(hintId);
                }
            }

            previousSpecialRoadStates[hintId] = state.IsActive;
        }

        List<string> knownHintIds = new List<string>(previousSpecialRoadStates.Keys);
        for (int i = 0; i < knownHintIds.Count; i++)
        {
            string hintId = knownHintIds[i];
            if (!specialRoadStates.ContainsKey(hintId))
            {
                previousSpecialRoadStates[hintId] = false;
            }
        }
    }

    private void CollectSpecialRoadStates()
    {
        specialRoadStates.Clear();

        Vector2 origin = playerFormRoot.transform.position;
        float radius =
            GameConstants.DefaultEnvironmentPreviewForwardDistance +
            GameConstants.DefaultEnvironmentPreviewVerticalTolerance;
        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, radius, specialRoadResults);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = specialRoadResults[i];
            if (hit == null)
            {
                continue;
            }

            SpecialRoadSegmentBase segment = hit.GetComponentInParent<SpecialRoadSegmentBase>();
            if (segment == null || !segment.HasValidHint)
            {
                continue;
            }

            bool isInside = hit.OverlapPoint(origin);
            bool isPreviewed = isInside || (segment.ShowHintWhenPreviewed && IsWithinPreviewRange(origin, hit));
            if (!isPreviewed)
            {
                continue;
            }

            string hintId = segment.HintId;
            if (string.IsNullOrWhiteSpace(hintId))
            {
                continue;
            }

            if (!specialRoadStates.TryGetValue(hintId, out SpecialRoadHintState existingState))
            {
                specialRoadStates[hintId] = new SpecialRoadHintState
                {
                    Segment = segment,
                    IsActive = true
                };
                continue;
            }

            if (!existingState.IsActive && (isInside || isPreviewed))
            {
                existingState.IsActive = true;
                specialRoadStates[hintId] = existingState;
            }
        }
    }

    private bool ShouldShowSpecialRoadHint(SpecialRoadSegmentBase segment)
    {
        if (segment == null || !segment.HasValidHint)
        {
            return false;
        }

        if (segment.ShowHintOnlyOnce && shownSpecialRoadHints.Contains(segment.HintId))
        {
            return false;
        }

        return true;
    }

    private static bool IsWithinPreviewRange(Vector2 origin, Collider2D collider)
    {
        Vector2 closestPoint = collider.ClosestPoint(origin);
        float horizontalDelta = closestPoint.x - origin.x;
        float verticalDelta = Mathf.Abs(closestPoint.y - origin.y);

        Bounds bounds = collider.bounds;
        float expandPercent = GameConstants.DefaultEnvironmentPreviewBoundsExpandPercent;
        float horizontalExpand = bounds.size.x * expandPercent;
        float verticalExpand = bounds.size.y * expandPercent;

        float backwardTolerance = GameConstants.DefaultEnvironmentPreviewBackwardTolerance + horizontalExpand;
        float forwardTolerance = GameConstants.DefaultEnvironmentPreviewForwardDistance + horizontalExpand;
        float verticalTolerance = GameConstants.DefaultEnvironmentPreviewVerticalTolerance + verticalExpand;

        return horizontalDelta >= -backwardTolerance
               && horizontalDelta <= forwardTolerance
               && verticalDelta <= verticalTolerance;
    }

    private void EnqueueHint(string key, string message, float duration)
    {
        if (hintBarUi == null || string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (queuedHintKeys.Contains(key))
        {
            return;
        }

        pendingHints.Enqueue(new HintRequest
        {
            Key = key,
            Message = message,
            Duration = Mathf.Max(0.5f, duration)
        });
        queuedHintKeys.Add(key);
    }

    private void FlushPendingHints()
    {
        if (hintBarUi == null || pendingHints.Count == 0)
        {
            return;
        }

        if (Time.time < nextHintAvailableTime)
        {
            return;
        }

        HintRequest request = pendingHints.Dequeue();
        queuedHintKeys.Remove(request.Key);
        hintBarUi.ShowHint(request.Message, request.Duration);
        nextHintAvailableTime = Time.time + request.Duration + Mathf.Max(0f, hintGapDuration);
    }

    private string ResolveIntroHintMessage()
    {
        if (messageCatalog == null || string.IsNullOrWhiteSpace(messageCatalog.IntroTransformHintTemplate) || levelController == null)
        {
            return messageCatalog != null ? messageCatalog.IntroTransformHintTemplate : string.Empty;
        }

        List<string> hotkeys = new List<string>();
        if (levelController.IsFormUnlocked(PlayerFormType.Human))
        {
            hotkeys.Add("1-人");
        }

        if (levelController.IsFormUnlocked(PlayerFormType.Car))
        {
            hotkeys.Add("2-车");
        }

        if (levelController.IsFormUnlocked(PlayerFormType.Plane))
        {
            hotkeys.Add("3-飞机");
        }

        if (levelController.IsFormUnlocked(PlayerFormType.Boat))
        {
            hotkeys.Add("4-船");
        }

        if (hotkeys.Count == 0)
        {
            return messageCatalog.IntroTransformHintTemplate;
        }

        return string.Format(messageCatalog.IntroTransformHintTemplate, string.Join(" / ", hotkeys));
    }
}
