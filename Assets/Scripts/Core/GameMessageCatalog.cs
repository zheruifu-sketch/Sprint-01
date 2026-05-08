using System;
using System.Collections.Generic;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[CreateAssetMenu(fileName = "GameMessageCatalog", menuName = "JumpGame/Game Message Catalog")]
public class GameMessageCatalog : ScriptableObject
{
    // Unified player-facing message source.
    // Runtime hint systems should read text from here instead of scattering strings
    // across UI components, SOs, and special road scripts.
    [Serializable]
    public class FailureMessageEntry
    {
        [LabelText("失败类型")]
        [SerializeField] private FailureType failureType = FailureType.None;
        [LabelText("提示文本")]
        [TextArea(2, 4)]
        [SerializeField] private string message = string.Empty;

        public FailureType FailureType => failureType;
        public string Message => message;
    }

    [Serializable]
    public class EnvironmentHintEntry
    {
        [LabelText("环境类型")]
        [SerializeField] private EnvironmentType environmentType = EnvironmentType.None;
        [LabelText("提示文本")]
        [TextArea(2, 4)]
        [SerializeField] private string message = string.Empty;
        [LabelText("推荐形态")]
        [SerializeField] private PlayerFormType recommendedForm = PlayerFormType.Human;
        [LabelText("已是推荐形态时不提示")]
        [SerializeField] private bool suppressWhenAlreadyUsingRecommendedForm = true;
        [LabelText("首次仅提示一次")]
        [SerializeField] private bool showOnlyOnce = true;
        [LabelText("提示持续时长")]
        [SerializeField] private float duration = 1.8f;

        public EnvironmentType EnvironmentType => environmentType;
        public string Message => message;
        public PlayerFormType RecommendedForm => recommendedForm;
        public bool SuppressWhenAlreadyUsingRecommendedForm => suppressWhenAlreadyUsingRecommendedForm;
        public bool ShowOnlyOnce => showOnlyOnce;
        public float Duration => duration;
    }

    [Serializable]
    public class SpecialRoadHintEntry
    {
        [LabelText("提示标识")]
        [SerializeField] private string hintId = string.Empty;
        [LabelText("提示文本")]
        [TextArea(2, 4)]
        [SerializeField] private string message = string.Empty;
        [LabelText("提示持续时长")]
        [SerializeField] private float duration = 2.8f;
        [LabelText("预览到时即可提示")]
        [SerializeField] private bool showWhenPreviewed = true;
        [LabelText("首次仅提示一次")]
        [SerializeField] private bool showOnlyOnce = true;

        public string HintId => hintId;
        public string Message => message;
        public float Duration => Mathf.Max(0.5f, duration);
        public bool ShowWhenPreviewed => showWhenPreviewed;
        public bool ShowOnlyOnce => showOnlyOnce;
    }

    [Header("Intro")]
    [LabelText("显示开场引导")]
    [SerializeField] private bool showIntroHintOnStart = true;
    [LabelText("变形引导模板")]
    [TextArea(2, 4)]
    [SerializeField] private string introTransformHintTemplate = "按 {0} 切换形态";
    [LabelText("开场引导持续时长")]
    [SerializeField] private float introHintDuration = 2f;

    [Header("Failures")]
    [LabelText("失败提示列表")]
    [SerializeField] private List<FailureMessageEntry> failureMessages = new List<FailureMessageEntry>();

    [Header("Environment Hints")]
    [LabelText("环境提示列表")]
    [SerializeField] private List<EnvironmentHintEntry> environmentHints = new List<EnvironmentHintEntry>();

    [Header("Special Road Hints")]
    [LabelText("特殊路段提示列表")]
    [SerializeField] private List<SpecialRoadHintEntry> specialRoadHints = new List<SpecialRoadHintEntry>();

    private static GameMessageCatalog cachedCatalog;

    public bool ShowIntroHintOnStart => showIntroHintOnStart;
    public string IntroTransformHintTemplate => introTransformHintTemplate;
    public float IntroHintDuration => Mathf.Max(0.5f, introHintDuration);
    public IReadOnlyList<FailureMessageEntry> FailureMessages => failureMessages;
    public IReadOnlyList<EnvironmentHintEntry> EnvironmentHints => environmentHints;
    public IReadOnlyList<SpecialRoadHintEntry> SpecialRoadHints => specialRoadHints;

    public static GameMessageCatalog Load()
    {
        if (cachedCatalog != null)
        {
            return cachedCatalog;
        }

        cachedCatalog = Resources.Load<GameMessageCatalog>("GameConfig/GameMessageCatalog");
        if (cachedCatalog != null)
        {
            return cachedCatalog;
        }

        cachedCatalog = Resources.Load<GameMessageCatalog>("提示配置");
        return cachedCatalog;
    }

    public string GetFailureMessage(FailureType failureType, string fallbackMessage)
    {
        for (int i = 0; i < failureMessages.Count; i++)
        {
            FailureMessageEntry entry = failureMessages[i];
            if (entry != null && entry.FailureType == failureType && !string.IsNullOrWhiteSpace(entry.Message))
            {
                return entry.Message;
            }
        }

        return fallbackMessage;
    }

    public EnvironmentHintEntry GetEnvironmentHint(EnvironmentType environmentType)
    {
        for (int i = 0; i < environmentHints.Count; i++)
        {
            EnvironmentHintEntry entry = environmentHints[i];
            if (entry != null && entry.EnvironmentType == environmentType)
            {
                return entry;
            }
        }

        return null;
    }

    public SpecialRoadHintEntry GetSpecialRoadHint(string hintId)
    {
        if (string.IsNullOrWhiteSpace(hintId))
        {
            return null;
        }

        for (int i = 0; i < specialRoadHints.Count; i++)
        {
            SpecialRoadHintEntry entry = specialRoadHints[i];
            if (entry != null && string.Equals(entry.HintId, hintId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }
}
