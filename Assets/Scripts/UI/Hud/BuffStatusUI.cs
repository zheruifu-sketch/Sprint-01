using System.Collections.Generic;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class BuffStatusUI : HudUIBase
{
    [Header("References")]
    [LabelText("玩家运行时上下文")]
    [SerializeField] private PlayerRuntimeContext runtimeContext;
    [LabelText("Buff控制器")]
    [SerializeField] private PlayerBuffController buffController;
    [LabelText("槽位父节点")]
    [SerializeField] private Transform slotsRoot;
    [LabelText("Buff槽位预制体")]
    [SerializeField] private BuffStatusSlotUI slotPrefab;
    [LabelText("最大显示数量")]
    [SerializeField] private int maxVisibleSlots = 5;

    [Header("Colors")]
    [LabelText("护盾颜色")]
    [SerializeField] private Color shieldColor = new Color(0.3f, 0.8f, 1f, 1f);
    [LabelText("加速颜色")]
    [SerializeField] private Color speedBoostColor = new Color(0.4f, 1f, 0.45f, 1f);

    [Header("Icons")]
    [LabelText("护盾图标")]
    [SerializeField] private Sprite shieldIcon;
    [LabelText("加速图标")]
    [SerializeField] private Sprite speedBoostIcon;

    private readonly List<BuffStatusSlotUI> slotInstances = new List<BuffStatusSlotUI>(5);

    protected override void Reset()
    {
        base.Reset();
        AutoBind();
    }

    protected override void Awake()
    {
        base.Awake();
        AutoBind();
        Refresh();
    }

    private void OnEnable()
    {
        AutoBind();
        if (buffController != null)
        {
            buffController.BuffsChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (buffController != null)
        {
            buffController.BuffsChanged -= Refresh;
        }
    }

    private void Update()
    {
        Refresh();
    }

    public override void Initialize()
    {
        AutoBind();
        Refresh();
    }

    private void AutoBind()
    {
        runtimeContext = runtimeContext != null ? runtimeContext : PlayerRuntimeContext.FindInScene();
        if (runtimeContext != null)
        {
            runtimeContext.RefreshReferences();
            buffController = buffController != null ? buffController : runtimeContext.BuffController;
        }

        if (slotsRoot == null)
        {
            Transform slotsTransform = transform.Find("SlotsRoot");
            slotsRoot = slotsTransform != null ? slotsTransform : transform;
        }

        CollectSceneSlots();
    }

    private void CollectSceneSlots()
    {
        if (slotsRoot == null || slotInstances.Count > 0 || slotPrefab != null)
        {
            return;
        }

        BuffStatusSlotUI[] existingSlots = slotsRoot.GetComponentsInChildren<BuffStatusSlotUI>(true);
        for (int i = 0; i < existingSlots.Length; i++)
        {
            BuffStatusSlotUI slot = existingSlots[i];
            if (slot != null && !slotInstances.Contains(slot))
            {
                slotInstances.Add(slot);
            }
        }
    }

    private void EnsureSlotCount(int requiredCount)
    {
        if (slotsRoot == null || slotPrefab == null)
        {
            return;
        }

        while (slotInstances.Count < requiredCount)
        {
            BuffStatusSlotUI slotInstance = Instantiate(slotPrefab, slotsRoot);
            slotInstance.name = $"BuffSlot_{slotInstances.Count + 1}";
            slotInstances.Add(slotInstance);
        }
    }

    private void Refresh()
    {
        int visibleLimit = Mathf.Max(0, maxVisibleSlots);
        int activeCount = buffController != null ? Mathf.Min(buffController.ActiveBuffCount, visibleLimit) : 0;

        EnsureSlotCount(activeCount);

        for (int i = 0; i < slotInstances.Count; i++)
        {
            BuffStatusSlotUI slot = slotInstances[i];
            if (slot == null)
            {
                continue;
            }

            bool shouldShow = i < activeCount;
            if (slot.gameObject.activeSelf != shouldShow)
            {
                slot.gameObject.SetActive(shouldShow);
            }

            if (!shouldShow)
            {
                continue;
            }

            PlayerBuffController.BuffSnapshot snapshot = buffController.GetBuffSnapshot(i);
            slot.Apply(ResolveBuffIcon(snapshot.BuffType), ResolveBuffColor(snapshot.BuffType), snapshot.NormalizedRemaining);
        }
    }

    private Color ResolveBuffColor(PlayerBuffType buffType)
    {
        return buffType switch
        {
            PlayerBuffType.Shield => shieldColor,
            PlayerBuffType.SpeedBoost => speedBoostColor,
            _ => Color.white
        };
    }

    private Sprite ResolveBuffIcon(PlayerBuffType buffType)
    {
        return buffType switch
        {
            PlayerBuffType.Shield => shieldIcon,
            PlayerBuffType.SpeedBoost => speedBoostIcon,
            _ => null
        };
    }
}
