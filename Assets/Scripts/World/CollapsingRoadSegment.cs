using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class CollapsingRoadSegment : SpecialRoadSegmentBase
{
    [Header("Collapse Timing")]
    [LabelText("最短坍塌延迟")]
    [SerializeField] private float minCollapseDelay = 3f;
    [LabelText("最长坍塌延迟")]
    [SerializeField] private float maxCollapseDelay = 5f;
    [LabelText("最少闪烁次数")]
    [SerializeField] private int minBlinkCount = 2;
    [LabelText("最多闪烁次数")]
    [SerializeField] private int maxBlinkCount = 3;
    [LabelText("闪烁熄灭时长")]
    [SerializeField] private float blinkOffDuration = 0.12f;
    [LabelText("闪烁点亮时长")]
    [SerializeField] private float blinkOnDuration = 0.12f;

    [Header("References")]
    [LabelText("控制的渲染器")]
    [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>();
    [LabelText("控制的碰撞体")]
    [SerializeField] private List<Collider2D> targetColliders = new List<Collider2D>();

    private bool collapseStarted;

    private void Reset()
    {
        EnsureHintDefaults(
            "road.collapse",
            "Collapsing road ahead. It will break seconds after you touch it. Accelerate through.");
        CollectTargets();
    }

    private void Awake()
    {
        EnsureHintDefaults(
            "road.collapse",
            "Collapsing road ahead. It will break seconds after you touch it. Accelerate through.");
        CollectTargets();
        SetRenderersVisible(true);
        SetCollidersEnabled(true);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryStartCollapse(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryStartCollapse(other);
    }

    private void TryStartCollapse(Component other)
    {
        if (collapseStarted || other == null)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerFormRoot>() == null)
        {
            return;
        }

        collapseStarted = true;
        StartCoroutine(CollapseRoutine());
    }

    private IEnumerator CollapseRoutine()
    {
        float collapseDelay = Random.Range(minCollapseDelay, maxCollapseDelay);
        int blinkCount = Random.Range(minBlinkCount, maxBlinkCount + 1);
        float blinkPhaseDuration = blinkCount * (blinkOffDuration + blinkOnDuration);
        float steadyDuration = Mathf.Max(0f, collapseDelay - blinkPhaseDuration);

        if (steadyDuration > 0f)
        {
            yield return new WaitForSeconds(steadyDuration);
        }

        for (int i = 0; i < blinkCount; i++)
        {
            SetRenderersVisible(false);
            yield return new WaitForSeconds(blinkOffDuration);
            SetRenderersVisible(true);
            yield return new WaitForSeconds(blinkOnDuration);
        }

        SetRenderersVisible(false);
        SetCollidersEnabled(false);
    }

    private void CollectTargets()
    {
        if (targetRenderers == null)
        {
            targetRenderers = new List<Renderer>();
        }

        if (targetColliders == null)
        {
            targetColliders = new List<Collider2D>();
        }

        targetRenderers.Clear();
        targetColliders.Clear();

        targetRenderers.AddRange(GetComponentsInChildren<Renderer>(true));
        targetColliders.AddRange(GetComponentsInChildren<Collider2D>(true));
    }

    private void SetRenderersVisible(bool visible)
    {
        for (int i = 0; i < targetRenderers.Count; i++)
        {
            Renderer cachedRenderer = targetRenderers[i];
            if (cachedRenderer != null)
            {
                cachedRenderer.enabled = visible;
            }
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        for (int i = 0; i < targetColliders.Count; i++)
        {
            Collider2D cachedCollider = targetColliders[i];
            if (cachedCollider != null)
            {
                cachedCollider.enabled = enabled;
            }
        }
    }
}
