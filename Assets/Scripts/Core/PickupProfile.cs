using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[CreateAssetMenu(fileName = "PickupProfile", menuName = "JumpGame/Pickup Profile")]
public class PickupProfile : ScriptableObject
{
    // Deprecated for manual levels.
    // Hand-authored pickups now keep their own gameplay parameters on prefab scripts.
    [Header("Base")]
    [LabelText("启用")]
    [SerializeField] private bool enabled = true;
    [LabelText("显示名称")]
    [SerializeField] private string displayName = string.Empty;
    [LabelText("拾取物类型")]
    [SerializeField] private PickupType pickupType = PickupType.Health;
    [LabelText("拾取物预制体")]
    [SerializeField] private GameObject pickupPrefab;

    [Header("Effect")]
    [LabelText("效果数值")]
    [SerializeField] private float amount = 25f;
    [LabelText("满状态时不生成")]
    [SerializeField] private bool skipSpawnWhenStatIsFull = true;
    [LabelText("生成权重")]
    [SerializeField] private float weight = 1f;

    public bool Enabled => enabled;
    public string DisplayName => displayName;
    public PickupType PickupType => pickupType;
    public GameObject PickupPrefab => pickupPrefab;
    public float Amount => Mathf.Max(0f, amount);
    public bool SkipSpawnWhenStatIsFull => skipSpawnWhenStatIsFull;
    public float Weight => Mathf.Max(0.01f, weight);
}
