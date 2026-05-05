using System.Collections.Generic;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class BlizzardVisibilitySegment : SpecialRoadSegmentBase
{
    [Header("Visibility Pulse")]
    [LabelText("最小透明度")]
    [SerializeField] private float minAlpha = 0.15f;
    [LabelText("最大透明度")]
    [SerializeField] private float maxAlpha = 0.72f;
    [LabelText("透明度脉动速度")]
    [SerializeField] private float alphaPulseSpeed = 1.7f;
    [LabelText("最小缩放")]
    [SerializeField] private Vector3 minScale = new Vector3(0.92f, 0.92f, 1f);
    [LabelText("最大缩放")]
    [SerializeField] private Vector3 maxScale = new Vector3(1.12f, 1.08f, 1f);
    [LabelText("缩放脉动速度")]
    [SerializeField] private float scalePulseSpeed = 1.1f;

    [Header("References")]
    [LabelText("暴风雪表现根节点")]
    [SerializeField] private Transform blizzardVisualRoot;
    [LabelText("控制的精灵")]
    [SerializeField] private List<SpriteRenderer> overlayRenderers = new List<SpriteRenderer>();

    private readonly Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    private Vector3 initialLocalScale = Vector3.one;

    private void Reset()
    {
        EnsureHintDefaults(
            "blizzard.visibility",
            "Whiteout ahead. Visibility surges in the blizzard. Keep your line and read the road late.");
        blizzardVisualRoot = transform;
        CollectTargets();
        CaptureInitialState();
    }

    private void Awake()
    {
        EnsureHintDefaults(
            "blizzard.visibility",
            "Whiteout ahead. Visibility surges in the blizzard. Keep your line and read the road late.");

        if (blizzardVisualRoot == null)
        {
            blizzardVisualRoot = transform;
        }

        CollectTargets();
        CaptureInitialState();
        ApplyVisualState(minAlpha);
    }

    private void OnEnable()
    {
        CaptureInitialState();
        ApplyVisualState(minAlpha);
    }

    private void Update()
    {
        float pulseAlpha = Mathf.Lerp(
            minAlpha,
            maxAlpha,
            Mathf.InverseLerp(-1f, 1f, Mathf.Sin(Time.time * Mathf.Max(0.01f, alphaPulseSpeed) * Mathf.PI * 2f)));
        float pulseScale = Mathf.InverseLerp(
            -1f,
            1f,
            Mathf.Sin(Time.time * Mathf.Max(0.01f, scalePulseSpeed) * Mathf.PI * 2f));

        ApplyVisualState(pulseAlpha);

        if (blizzardVisualRoot != null)
        {
            Vector3 targetScale = Vector3.LerpUnclamped(minScale, maxScale, pulseScale);
            blizzardVisualRoot.localScale = Vector3.Scale(initialLocalScale, targetScale);
        }
    }

    private void CaptureInitialState()
    {
        if (blizzardVisualRoot != null)
        {
            initialLocalScale = blizzardVisualRoot.localScale;
        }

        originalColors.Clear();
        for (int i = 0; i < overlayRenderers.Count; i++)
        {
            SpriteRenderer renderer = overlayRenderers[i];
            if (renderer != null && !originalColors.ContainsKey(renderer))
            {
                originalColors.Add(renderer, renderer.color);
            }
        }
    }

    private void CollectTargets()
    {
        if (overlayRenderers == null)
        {
            overlayRenderers = new List<SpriteRenderer>();
        }

        overlayRenderers.Clear();
        Transform root = blizzardVisualRoot != null ? blizzardVisualRoot : transform;
        overlayRenderers.AddRange(root.GetComponentsInChildren<SpriteRenderer>(true));
    }

    private void ApplyVisualState(float alpha)
    {
        float clampedAlpha = Mathf.Clamp01(alpha);

        for (int i = 0; i < overlayRenderers.Count; i++)
        {
            SpriteRenderer renderer = overlayRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!originalColors.TryGetValue(renderer, out Color baseColor))
            {
                baseColor = renderer.color;
                originalColors[renderer] = baseColor;
            }

            Color targetColor = baseColor;
            targetColor.a = baseColor.a * clampedAlpha;
            renderer.color = targetColor;
            renderer.enabled = true;
        }
    }
}
