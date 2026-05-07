using System;
using System.Collections.Generic;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[CreateAssetMenu(fileName = "ManualLevelFlowConfig", menuName = "JumpGame/Manual Level Flow Config")]
public class ManualLevelFlowConfig : ScriptableObject
{
    [Serializable]
    public class ManualLevelDefinition
    {
        [LabelText("关卡名称")]
        [SerializeField] private string levelName = "Level";
        [LabelText("关卡说明")]
        [SerializeField] private string description = string.Empty;
        [LabelText("开始提示")]
        [SerializeField] private string startHint = string.Empty;
        [LabelText("通关提示")]
        [SerializeField] private string clearHint = string.Empty;
        [LabelText("解锁形态")]
        [SerializeField] private List<PlayerFormType> unlockedForms = new List<PlayerFormType>();

        public string LevelName => string.IsNullOrWhiteSpace(levelName) ? "Level" : levelName;
        public string Description => description;
        public string StartHint => startHint;
        public string ClearHint => clearHint;
        public List<PlayerFormType> UnlockedForms => unlockedForms;
    }

    [Header("Flow")]
    [LabelText("默认起始关卡")]
    [SerializeField] private int defaultStartingLevel = 1;
    [LabelText("关卡切换延迟")]
    [SerializeField] private float transitionDelay = 1.25f;
    [LabelText("启用调试热键")]
    [SerializeField] private bool enableDebugHotkeys = true;

    [Header("Levels")]
    [LabelText("手工关卡配置")]
    [SerializeField] private List<ManualLevelDefinition> levels = new List<ManualLevelDefinition>();

    private static ManualLevelFlowConfig cachedConfig;

    public int DefaultStartingLevel => Mathf.Max(1, defaultStartingLevel);
    public float TransitionDelay => Mathf.Max(0f, transitionDelay);
    public bool EnableDebugHotkeys => enableDebugHotkeys;
    public int LevelCount => levels != null ? levels.Count : 0;

    public static ManualLevelFlowConfig Load()
    {
        if (cachedConfig != null)
        {
            return cachedConfig;
        }

        cachedConfig = Resources.Load<ManualLevelFlowConfig>("GameConfig/ManualLevelFlowConfig");
        if (cachedConfig != null)
        {
            return cachedConfig;
        }

        // Manual mode now uses the simplified prefab-based flow asset.
        // Keep this fallback so the current Chinese-named Resources asset
        // remains valid even though the old GameConfig path is no longer used.
        cachedConfig = Resources.Load<ManualLevelFlowConfig>("关卡配置");
        return cachedConfig;
    }

    public ManualLevelDefinition GetLevel(int levelIndex)
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
        ManualLevelDefinition level = GetLevel(levelIndex);
        if (level == null || level.UnlockedForms == null || level.UnlockedForms.Count == 0)
        {
            return true;
        }

        return level.UnlockedForms.Contains(formType);
    }

    public PlayerFormType GetFallbackUnlockedForm(int levelIndex, PlayerFormType preferredForm = PlayerFormType.Human)
    {
        ManualLevelDefinition level = GetLevel(levelIndex);
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
