using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class EnvironmentHintUI : HudUIBase
{
    [Serializable]
    private class ZoneHintEntry
    {
        [FormerlySerializedAs("zoneType")]
        [SerializeField] private EnvironmentType environmentType = EnvironmentType.None;
        [SerializeField] private string message = string.Empty;
        [SerializeField] private PlayerFormType recommendedForm = PlayerFormType.Human;
        [SerializeField] private bool suppressWhenAlreadyUsingRecommendedForm = true;
        [SerializeField] private bool showOnlyOnce = true;
        [SerializeField] private float duration = 3f;

        public ZoneHintEntry()
        {
        }

        public ZoneHintEntry(
            EnvironmentType environmentType,
            string message,
            PlayerFormType recommendedForm,
            bool suppressWhenAlreadyUsingRecommendedForm = true,
            bool showOnlyOnce = true,
            float duration = 3f)
        {
            this.environmentType = environmentType;
            this.message = message;
            this.recommendedForm = recommendedForm;
            this.suppressWhenAlreadyUsingRecommendedForm = suppressWhenAlreadyUsingRecommendedForm;
            this.showOnlyOnce = showOnlyOnce;
            this.duration = duration;
        }

        public EnvironmentType EnvironmentType => environmentType;
        public string Message => message;
        public PlayerFormType RecommendedForm => recommendedForm;
        public bool SuppressWhenAlreadyUsingRecommendedForm => suppressWhenAlreadyUsingRecommendedForm;
        public bool ShowOnlyOnce => showOnlyOnce;
        public float Duration => duration;
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

    [Header("Intro Hint")]
    [LabelText("启动时显示引导")]
    [SerializeField] private bool showIntroHintOnStart;
    [LabelText("默认引导文本")]
    [SerializeField] private string introHintMessage = string.Empty;
    [LabelText("引导延迟")]
    [SerializeField] private float introHintDelay = 0.6f;
    [LabelText("引导持续时间")]
    [SerializeField] private float introHintDuration = 2f;

    [Header("Zone Hints")]
    [LabelText("环境提示列表")]
    [SerializeField] private List<ZoneHintEntry> zoneHints = new List<ZoneHintEntry>
    {
        new ZoneHintEntry(),
        new ZoneHintEntry()
    };

    private readonly Dictionary<EnvironmentType, bool> previousZoneStates = new Dictionary<EnvironmentType, bool>();
    private readonly HashSet<EnvironmentType> shownZoneHints = new HashSet<EnvironmentType>();
    private float introHintTimer;
    private bool introHintQueued;

    protected override void Reset()
    {
        base.Reset();
        AutoBind();
        EnsureDefaultZoneHints();
    }

    protected override void Awake()
    {
        base.Awake();
        AutoBind();
        EnsureDefaultZoneHints();
        PrimeZoneStates();
        introHintTimer = introHintDelay;
        introHintQueued = showIntroHintOnStart && !string.IsNullOrWhiteSpace(introHintMessage);
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
    }

    [ContextMenu("Reset Zone Hints To Defaults")]
    private void ResetZoneHintsToDefaults()
    {
        zoneHints.Clear();
        zoneHints.Add(new ZoneHintEntry(EnvironmentType.Water, "Water ahead", PlayerFormType.Boat, true, true, 1.6f));
        zoneHints.Add(new ZoneHintEntry(EnvironmentType.Cliff, "Cliff ahead", PlayerFormType.Plane, true, true, 1.6f));
        zoneHints.Add(new ZoneHintEntry(EnvironmentType.Blizzard, "Blizzard ahead", PlayerFormType.Human, false, true, 1.6f));
        zoneHints.Add(new ZoneHintEntry(EnvironmentType.Obstacle, "Obstacle ahead", PlayerFormType.Human, false, true, 1.6f));
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
    }

    private void EnsureDefaultZoneHints()
    {
        if (zoneHints.Count > 0 && HasConfiguredZoneHint())
        {
            return;
        }

        ResetZoneHintsToDefaults();
    }

    private bool HasConfiguredZoneHint()
    {
        for (int i = 0; i < zoneHints.Count; i++)
        {
            ZoneHintEntry entry = zoneHints[i];
            if (entry != null && entry.EnvironmentType != EnvironmentType.None && !string.IsNullOrWhiteSpace(entry.Message))
            {
                return true;
            }
        }

        return false;
    }

    private void PrimeZoneStates()
    {
        previousZoneStates.Clear();
        for (int i = 0; i < zoneHints.Count; i++)
        {
            ZoneHintEntry entry = zoneHints[i];
            if (entry == null || entry.EnvironmentType == EnvironmentType.None)
            {
                continue;
            }

            previousZoneStates[entry.EnvironmentType] = IsPlayerInEnvironment(entry.EnvironmentType);
        }
    }

    private void UpdateIntroHint()
    {
        if (!introHintQueued)
        {
            return;
        }

        introHintTimer -= Time.deltaTime;
        if (introHintTimer > 0f)
        {
            return;
        }

        introHintQueued = false;
        ShowHint(ResolveIntroHintMessage(), introHintDuration);
    }

    private void UpdateZoneHints()
    {
        if (environmentContext == null)
        {
            return;
        }

        for (int i = 0; i < zoneHints.Count; i++)
        {
            ZoneHintEntry entry = zoneHints[i];
            if (entry == null || entry.EnvironmentType == EnvironmentType.None || string.IsNullOrWhiteSpace(entry.Message))
            {
                continue;
            }

            bool isInside = IsPlayerInEnvironment(entry.EnvironmentType);
            bool wasInside = previousZoneStates.TryGetValue(entry.EnvironmentType, out bool value) && value;

            if (isInside && !wasInside && ShouldShowZoneHint(entry))
            {
                ShowHint(entry.Message, entry.Duration);
                if (entry.ShowOnlyOnce)
                {
                    shownZoneHints.Add(entry.EnvironmentType);
                }
            }

            previousZoneStates[entry.EnvironmentType] = isInside;
        }
    }

    private bool ShouldShowZoneHint(ZoneHintEntry entry)
    {
        if (entry.ShowOnlyOnce && shownZoneHints.Contains(entry.EnvironmentType))
        {
            return false;
        }

        if (entry.SuppressWhenAlreadyUsingRecommendedForm &&
            playerFormRoot != null &&
            playerFormRoot.CurrentForm == entry.RecommendedForm)
        {
            return false;
        }

        return true;
    }

    private bool IsPlayerInEnvironment(EnvironmentType environmentType)
    {
        return environmentContext != null && environmentContext.IsInEnvironment(environmentType);
    }

    private void ShowHint(string message, float duration)
    {
        if (hintBarUi == null)
        {
            return;
        }

        hintBarUi.ShowHint(message, duration);
    }

    private string ResolveIntroHintMessage()
    {
        if (string.IsNullOrWhiteSpace(introHintMessage) || levelController == null)
        {
            return introHintMessage;
        }

        List<string> hotkeys = new List<string>();
        if (levelController.IsFormUnlocked(PlayerFormType.Human))
        {
            hotkeys.Add("1-Human");
        }

        if (levelController.IsFormUnlocked(PlayerFormType.Car))
        {
            hotkeys.Add("2-Car");
        }

        if (levelController.IsFormUnlocked(PlayerFormType.Plane))
        {
            hotkeys.Add("3-Plane");
        }

        if (levelController.IsFormUnlocked(PlayerFormType.Boat))
        {
            hotkeys.Add("4-Boat");
        }

        if (hotkeys.Count == 0)
        {
            return introHintMessage;
        }

        return $"Press {string.Join(" / ", hotkeys)} to transform";
    }
}
