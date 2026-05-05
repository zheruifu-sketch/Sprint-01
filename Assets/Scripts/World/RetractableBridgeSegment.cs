using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class RetractableBridgeSegment : SpecialRoadSegmentBase
{
    [Header("Cycle Timing")]
    [LabelText("开场等待时长")]
    [SerializeField] private float initialDelay = 0f;
    [LabelText("桥面伸出停留时长")]
    [SerializeField] private float extendedHoldDuration = 1.5f;
    [LabelText("桥面收回停留时长")]
    [SerializeField] private float retractedHoldDuration = 1.2f;
    [LabelText("单次伸缩时长")]
    [SerializeField] private float moveDuration = 0.8f;

    [Header("Bridge Motion")]
    [LabelText("初始是否为伸出状态")]
    [SerializeField] private bool startExtended = true;
    [LabelText("收回位置标记")]
    [SerializeField] private Transform retractedPoint;
    [LabelText("伸出位置标记")]
    [SerializeField] private Transform extendedPoint;
    [LabelText("伸缩曲线")]
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Activation")]
    [LabelText("运行时自动循环")]
    [SerializeField] private bool autoCycleOnPlay = true;
    [LabelText("首次接触玩家后开始循环")]
    [SerializeField] private bool startCycleOnPlayerEnter;

    [Header("References")]
    [LabelText("桥面根节点")]
    [SerializeField] private Transform bridgeRoot;
    [LabelText("控制的渲染器")]
    [SerializeField] private List<Renderer> controlledRenderers = new List<Renderer>();
    [LabelText("控制的碰撞体")]
    [SerializeField] private List<Collider2D> controlledColliders = new List<Collider2D>();

    private Coroutine cycleCoroutine;
    private bool hasStartedCycle;
    private Vector3 initialRetractedLocalPosition;
    private Vector3 initialExtendedLocalPosition;

    private void Reset()
    {
        EnsureHintDefaults(
            "road.retractable-bridge",
            "Retractable bridge ahead. Watch the cycle and move when the bridge extends.");
        bridgeRoot = transform;
        ResolveMotionMarkers();
        CacheMotionPositions();
        CollectTargets();
    }

    private void Awake()
    {
        EnsureHintDefaults(
            "road.retractable-bridge",
            "Retractable bridge ahead. Watch the cycle and move when the bridge extends.");
        if (bridgeRoot == null)
        {
            bridgeRoot = transform;
        }

        ResolveMotionMarkers();
        CacheMotionPositions();
        CollectTargets();
        ApplyStateImmediately(startExtended ? 1f : 0f);
    }

    private void OnEnable()
    {
        ApplyStateImmediately(startExtended ? 1f : 0f);
        hasStartedCycle = false;

        if (autoCycleOnPlay && !startCycleOnPlayerEnter)
        {
            StartCycleIfNeeded();
        }
    }

    private void OnDisable()
    {
        if (cycleCoroutine != null)
        {
            StopCoroutine(cycleCoroutine);
            cycleCoroutine = null;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryStartOnPlayer(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryStartOnPlayer(other);
    }

    private void TryStartOnPlayer(Component other)
    {
        if (!startCycleOnPlayerEnter || hasStartedCycle)
        {
            return;
        }

        if (other == null || other.GetComponentInParent<PlayerFormRoot>() == null)
        {
            return;
        }

        StartCycleIfNeeded();
    }

    private void StartCycleIfNeeded()
    {
        if (hasStartedCycle)
        {
            return;
        }

        hasStartedCycle = true;
        if (cycleCoroutine != null)
        {
            StopCoroutine(cycleCoroutine);
        }

        cycleCoroutine = StartCoroutine(CycleRoutine());
    }

    private IEnumerator CycleRoutine()
    {
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        bool isExtended = startExtended;
        ApplyStateImmediately(isExtended ? 1f : 0f);

        while (true)
        {
            float holdDuration = isExtended ? extendedHoldDuration : retractedHoldDuration;
            if (holdDuration > 0f)
            {
                yield return new WaitForSeconds(holdDuration);
            }

            float from = isExtended ? 1f : 0f;
            float to = isExtended ? 0f : 1f;
            yield return AnimateBridge(from, to);
            isExtended = !isExtended;
        }
    }

    private IEnumerator AnimateBridge(float fromProgress, float toProgress)
    {
        float duration = Mathf.Max(0.01f, moveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float linearProgress = Mathf.Clamp01(elapsed / duration);
            float curveProgress = moveCurve != null ? moveCurve.Evaluate(linearProgress) : linearProgress;
            float bridgeProgress = Mathf.LerpUnclamped(fromProgress, toProgress, curveProgress);
            ApplyStateImmediately(bridgeProgress);
            yield return null;
        }

        ApplyStateImmediately(toProgress);
    }

    private void ApplyStateImmediately(float extendedProgress)
    {
        if (bridgeRoot == null)
        {
            return;
        }

        CacheMotionPositions();
        float clampedProgress = Mathf.Clamp01(extendedProgress);
        bridgeRoot.localPosition = Vector3.LerpUnclamped(initialRetractedLocalPosition, initialExtendedLocalPosition, clampedProgress);
    }

    private void ResolveMotionMarkers()
    {
        if (bridgeRoot == null)
        {
            bridgeRoot = transform;
        }

        if (retractedPoint == null)
        {
            retractedPoint = FindChildByName("收位置", "收回位置", "RetractedPoint", "Retracted");
        }

        if (extendedPoint == null)
        {
            extendedPoint = FindChildByName("出位置", "伸出位置", "ExtendedPoint", "Extended");
        }
    }

    private void CacheMotionPositions()
    {
        if (bridgeRoot == null)
        {
            return;
        }

        initialRetractedLocalPosition = retractedPoint != null
            ? bridgeRoot.parent.InverseTransformPoint(retractedPoint.position)
            : bridgeRoot.localPosition;
        initialExtendedLocalPosition = extendedPoint != null
            ? bridgeRoot.parent.InverseTransformPoint(extendedPoint.position)
            : bridgeRoot.localPosition;
    }

    private Transform FindChildByName(params string[] candidates)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform child = transforms[i];
            if (child == null || child == transform)
            {
                continue;
            }

            for (int nameIndex = 0; nameIndex < candidates.Length; nameIndex++)
            {
                if (child.name == candidates[nameIndex])
                {
                    return child;
                }
            }
        }

        return null;
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

        controlledRenderers.Clear();
        controlledColliders.Clear();

        Transform targetRoot = bridgeRoot != null ? bridgeRoot : transform;
        controlledRenderers.AddRange(targetRoot.GetComponentsInChildren<Renderer>(true));
        controlledColliders.AddRange(targetRoot.GetComponentsInChildren<Collider2D>(true));
    }

    private void SetRenderersVisible(bool visible)
    {
        for (int i = 0; i < controlledRenderers.Count; i++)
        {
            Renderer cachedRenderer = controlledRenderers[i];
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
