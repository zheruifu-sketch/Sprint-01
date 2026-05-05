using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class SinkingRoadSegment : SpecialRoadSegmentBase
{
    [Header("Sink Timing")]
    [LabelText("最短下沉延迟")]
    [SerializeField] private float minSinkDelay = 1.5f;
    [LabelText("最长下沉延迟")]
    [SerializeField] private float maxSinkDelay = 2.5f;
    [LabelText("最少闪烁次数")]
    [SerializeField] private int minBlinkCount = 2;
    [LabelText("最多闪烁次数")]
    [SerializeField] private int maxBlinkCount = 4;
    [LabelText("闪烁熄灭时长")]
    [SerializeField] private float blinkOffDuration = 0.12f;
    [LabelText("闪烁点亮时长")]
    [SerializeField] private float blinkOnDuration = 0.12f;
    [LabelText("下沉时长")]
    [SerializeField] private float sinkDuration = 1.2f;

    [Header("Sink Motion")]
    [LabelText("下沉位移")]
    [SerializeField] private Vector3 sinkOffset = new Vector3(0f, -3.5f, 0f);
    [LabelText("下沉曲线")]
    [SerializeField] private AnimationCurve sinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("State Control")]
    [LabelText("开始下沉时关闭碰撞")]
    [SerializeField] private bool disableCollidersOnSinkStart;
    [LabelText("下沉结束后关闭碰撞")]
    [SerializeField] private bool disableCollidersOnComplete = true;
    [LabelText("下沉结束后隐藏表现")]
    [SerializeField] private bool hideRenderersOnComplete;
    [LabelText("仅触发一次")]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("References")]
    [LabelText("下沉根节点")]
    [SerializeField] private Transform sinkingRoot;
    [LabelText("闪烁表现")]
    [SerializeField] private List<Renderer> warningRenderers = new List<Renderer>();
    [LabelText("控制的渲染器")]
    [SerializeField] private List<Renderer> controlledRenderers = new List<Renderer>();
    [LabelText("控制的碰撞体")]
    [SerializeField] private List<Collider2D> controlledColliders = new List<Collider2D>();

    private Vector3 initialLocalPosition;
    private bool initialStateCaptured;
    private bool hasTriggered;
    private bool isRunning;

    private void Reset()
    {
        EnsureHintDefaults(
            "road.sinking",
            "Sinking road ahead. Once triggered, the lane drops fast. Cross without stopping.");
        sinkingRoot = transform;
        CollectTargets();
    }

    private void Awake()
    {
        EnsureHintDefaults(
            "road.sinking",
            "Sinking road ahead. Once triggered, the lane drops fast. Cross without stopping.");
        if (sinkingRoot == null)
        {
            sinkingRoot = transform;
        }

        CollectTargets();
        if (!Application.isPlaying)
        {
            CaptureInitialState();
        }

        RestoreInitialState(!Application.isPlaying);
    }

    private void Start()
    {
        EnsureHintDefaults(
            "road.sinking",
            "Sinking road ahead. Once triggered, the lane drops fast. Cross without stopping.");
        CaptureInitialState();
        RestoreInitialState(true);
    }

    private void OnEnable()
    {
        if (sinkingRoot == null)
        {
            sinkingRoot = transform;
        }

        RestoreInitialState(initialStateCaptured);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryStartSink(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryStartSink(other);
    }

    private void TryStartSink(Component other)
    {
        if (isRunning)
        {
            return;
        }

        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        if (other == null || other.GetComponentInParent<PlayerFormRoot>() == null)
        {
            return;
        }

        hasTriggered = true;
        isRunning = true;
        StartCoroutine(SinkRoutine());
    }

    private IEnumerator SinkRoutine()
    {
        CaptureInitialState();

        float sinkDelay = Random.Range(minSinkDelay, maxSinkDelay);
        int blinkCount = Random.Range(minBlinkCount, maxBlinkCount + 1);
        float blinkPhaseDuration = blinkCount * (blinkOffDuration + blinkOnDuration);
        float steadyDuration = Mathf.Max(0f, sinkDelay - blinkPhaseDuration);

        if (steadyDuration > 0f)
        {
            yield return new WaitForSeconds(steadyDuration);
        }

        List<Renderer> blinkTargets = warningRenderers != null && warningRenderers.Count > 0
            ? warningRenderers
            : controlledRenderers;

        for (int i = 0; i < blinkCount; i++)
        {
            SetRenderersVisible(blinkTargets, false);
            yield return new WaitForSeconds(blinkOffDuration);
            SetRenderersVisible(blinkTargets, true);
            yield return new WaitForSeconds(blinkOnDuration);
        }

        if (disableCollidersOnSinkStart)
        {
            SetCollidersEnabled(false);
        }

        Vector3 startLocalPosition = initialLocalPosition;
        Vector3 targetLocalPosition = initialLocalPosition + sinkOffset;
        float duration = Mathf.Max(0.01f, sinkDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float curveValue = sinkCurve != null ? sinkCurve.Evaluate(progress) : progress;
            sinkingRoot.localPosition = Vector3.LerpUnclamped(startLocalPosition, targetLocalPosition, curveValue);
            yield return null;
        }

        sinkingRoot.localPosition = targetLocalPosition;

        if (disableCollidersOnComplete)
        {
            SetCollidersEnabled(false);
        }

        if (hideRenderersOnComplete)
        {
            SetRenderersVisible(controlledRenderers, false);
        }

        isRunning = false;
    }

    private void CaptureInitialState()
    {
        if (sinkingRoot == null)
        {
            return;
        }

        initialLocalPosition = sinkingRoot.localPosition;
        initialStateCaptured = true;
    }

    private void RestoreInitialState(bool restorePosition)
    {
        StopAllCoroutines();
        isRunning = false;

        if (restorePosition && sinkingRoot != null)
        {
            sinkingRoot.localPosition = initialLocalPosition;
        }

        SetRenderersVisible(controlledRenderers, true);
        SetRenderersVisible(warningRenderers, true);
        SetCollidersEnabled(true);
    }

    private void CollectTargets()
    {
        if (controlledRenderers == null)
        {
            controlledRenderers = new List<Renderer>();
        }

        if (controlledColliders == null)
        {
            controlledColliders = new List<Collider2D>();
        }

        if (warningRenderers == null)
        {
            warningRenderers = new List<Renderer>();
        }

        controlledRenderers.Clear();
        controlledColliders.Clear();

        Transform targetRoot = sinkingRoot != null ? sinkingRoot : transform;
        controlledRenderers.AddRange(targetRoot.GetComponentsInChildren<Renderer>(true));
        controlledColliders.AddRange(targetRoot.GetComponentsInChildren<Collider2D>(true));

        if (warningRenderers.Count == 0)
        {
            warningRenderers.AddRange(controlledRenderers);
        }
    }

    private static void SetRenderersVisible(List<Renderer> renderers, bool visible)
    {
        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer cachedRenderer = renderers[i];
            if (cachedRenderer != null)
            {
                cachedRenderer.enabled = visible;
            }
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        for (int i = 0; i < controlledColliders.Count; i++)
        {
            Collider2D cachedCollider = controlledColliders[i];
            if (cachedCollider != null)
            {
                cachedCollider.enabled = enabled;
            }
        }
    }
}
