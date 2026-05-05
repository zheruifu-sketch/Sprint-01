using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[Serializable]
public class SegmentTemplateConfig
{
    [LabelText("名称")]
    [SerializeField] private string label = "Segment";
    [LabelText("预制体")]
    [SerializeField] private GameObject prefab;
    [LabelText("额外间距")]
    [SerializeField] private float extraSpacing;
    [LabelText("纵向偏移")]
    [SerializeField] private float yOffset;
    [LabelText("最少连续生成次数")]
    [SerializeField] private int minRepeatCount = 1;
    [LabelText("最多连续生成次数")]
    [SerializeField] private int maxRepeatCount = 1;
    [FormerlySerializedAs("zoneType")]
    [LabelText("环境类型")]
    [SerializeField] private EnvironmentType environmentType = EnvironmentType.None;

    public SegmentTemplateConfig()
    {
    }

    public SegmentTemplateConfig(
        string label,
        GameObject prefab,
        EnvironmentType environmentType,
        float extraSpacing = 0f,
        float yOffset = 0f,
        int minRepeatCount = 1,
        int maxRepeatCount = 1)
    {
        this.label = label;
        this.prefab = prefab;
        this.environmentType = environmentType;
        this.extraSpacing = extraSpacing;
        this.yOffset = yOffset;
        this.minRepeatCount = minRepeatCount;
        this.maxRepeatCount = maxRepeatCount;
    }

    public string Label => string.IsNullOrWhiteSpace(label) && prefab != null ? prefab.name : label;
    public GameObject Prefab => prefab;
    public float ExtraSpacing => extraSpacing;
    public float YOffset => yOffset;
    public int MinRepeatCount => Mathf.Max(1, minRepeatCount);
    public int MaxRepeatCount => Mathf.Max(MinRepeatCount, maxRepeatCount);
    public EnvironmentType EnvironmentType => environmentType;
}

[Serializable]
public class EnvironmentVariantSetConfig
{
    [LabelText("环境类型")]
    [SerializeField] private EnvironmentType environmentType = EnvironmentType.None;
    [LabelText("普通变体")]
    [SerializeField] private List<SegmentTemplateConfig> normalVariants = new List<SegmentTemplateConfig>();
    [LabelText("特殊事件变体")]
    [SerializeField] private List<SegmentTemplateConfig> specialVariants = new List<SegmentTemplateConfig>();
    [LabelText("特殊事件概率")]
    [SerializeField] private float specialVariantChance = 0.15f;

    public EnvironmentType EnvironmentType => environmentType;
    public List<SegmentTemplateConfig> NormalVariants => normalVariants;
    public List<SegmentTemplateConfig> SpecialVariants => specialVariants;
    public float SpecialVariantChance => Mathf.Clamp01(specialVariantChance);
}

[CreateAssetMenu(fileName = "SegmentVariantConfig", menuName = "JumpGame/Segment Variant Config")]
public class SegmentVariantConfig : ScriptableObject
{
    [LabelText("环境变体库")]
    [SerializeField] private List<EnvironmentVariantSetConfig> environmentVariants = new List<EnvironmentVariantSetConfig>();

    private static SegmentVariantConfig cachedConfig;

    public List<EnvironmentVariantSetConfig> EnvironmentVariants => environmentVariants;

    public static SegmentVariantConfig Load()
    {
        if (cachedConfig != null)
        {
            return cachedConfig;
        }

        cachedConfig = Resources.Load<SegmentVariantConfig>("GameConfig/SegmentVariantConfig");
        return cachedConfig;
    }
}
