using System.Collections;
using System.Collections.Generic;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class CollapsingSnowSegment : SpecialRoadSegmentBase
{
    [System.Serializable]
    private class SnowPatchEntry
    {
        [LabelText("支撑碰撞体")]
        [SerializeField] private Collider2D supportCollider;
        [LabelText("视觉节点")]
        [SerializeField] private GameObject visualRoot;

        private List<Renderer> cachedRenderers;

        public Collider2D SupportCollider => supportCollider;
        public GameObject VisualRoot => visualRoot;

        public List<Renderer> GetRenderers()
        {
            if (cachedRenderers != null)
            {
                return cachedRenderers;
            }

            cachedRenderers = new List<Renderer>();
            if (visualRoot != null)
            {
                cachedRenderers.AddRange(visualRoot.GetComponentsInChildren<Renderer>(true));
            }

            return cachedRenderers;
        }

        public void ClearCache()
        {
            cachedRenderers = null;
        }
    }

    [Header("Collapse Timing")]
    [LabelText("塌陷块列表")]
    [SerializeField] private List<SnowPatchEntry> patches = new List<SnowPatchEntry>();
    [LabelText("塌陷延迟")]
    [SerializeField] private float collapseDelay = 0.12f;
    [LabelText("闪烁次数")]
    [SerializeField] private int blinkCount = 1;
    [LabelText("闪烁熄灭时长")]
    [SerializeField] private float blinkOffDuration = 0.06f;
    [LabelText("闪烁点亮时长")]
    [SerializeField] private float blinkOnDuration = 0.06f;

    [Header("State Control")]
    [LabelText("塌陷后隐藏视觉")]
    [SerializeField] private bool hideVisualOnCollapse = true;
    [LabelText("每块只触发一次")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private readonly HashSet<SnowPatchEntry> triggeredPatches = new HashSet<SnowPatchEntry>();
    private Transform playerTransform;
    private Collider2D[] playerColliders;

    private void Reset()
    {
        EnsureHintDefaults(
            "blizzard.collapse-snow",
            "Weak snow ahead. The surface gives way underfoot. Jump the collapse the moment it breaks.");
    }

    private void Awake()
    {
        EnsureHintDefaults(
            "blizzard.collapse-snow",
            "Weak snow ahead. The surface gives way underfoot. Jump the collapse the moment it breaks.");
        CachePlayerReferences();
        ClearRendererCaches();
    }

    private void OnEnable()
    {
        triggeredPatches.Clear();
        RestoreAllPatches();
    }

    private void Update()
    {
        CachePlayerReferences();
        if (playerTransform == null)
        {
            return;
        }

        for (int i = 0; i < patches.Count; i++)
        {
            SnowPatchEntry patch = patches[i];
            if (patch == null || patch.SupportCollider == null)
            {
                continue;
            }

            if (triggerOnlyOnce && triggeredPatches.Contains(patch))
            {
                continue;
            }

            Collider2D supportCollider = patch.SupportCollider;
            if (!supportCollider.enabled || !IsPlayerTouchingPatch(supportCollider))
            {
                continue;
            }

            triggeredPatches.Add(patch);
            StartCoroutine(CollapsePatchRoutine(patch));
        }
    }

    private void CachePlayerReferences()
    {
        if (playerTransform == null)
        {
            playerTransform = FindPlayerTransform();
        }

        if (playerColliders != null && playerColliders.Length > 0)
        {
            return;
        }

        PlayerFormRoot formRoot = playerTransform != null ? playerTransform.GetComponent<PlayerFormRoot>() : null;
        if (formRoot == null)
        {
            formRoot = Object.FindObjectOfType<PlayerFormRoot>();
        }

        if (formRoot == null)
        {
            playerColliders = System.Array.Empty<Collider2D>();
            return;
        }

        playerTransform = formRoot.transform;
        playerColliders = formRoot.GetComponentsInChildren<Collider2D>(true);
    }

    private bool IsPlayerTouchingPatch(Collider2D supportCollider)
    {
        if (supportCollider == null)
        {
            return false;
        }

        if (playerColliders != null)
        {
            for (int i = 0; i < playerColliders.Length; i++)
            {
                Collider2D playerCollider = playerColliders[i];
                if (playerCollider == null || !playerCollider.enabled)
                {
                    continue;
                }

                if (supportCollider.IsTouching(playerCollider))
                {
                    return true;
                }
            }
        }

        return supportCollider.OverlapPoint(playerTransform.position);
    }

    private IEnumerator CollapsePatchRoutine(SnowPatchEntry patch)
    {
        if (patch == null)
        {
            yield break;
        }

        if (collapseDelay > 0f)
        {
            yield return new WaitForSeconds(collapseDelay);
        }

        List<Renderer> renderers = patch.GetRenderers();
        for (int i = 0; i < Mathf.Max(0, blinkCount); i++)
        {
            SetRenderersVisible(renderers, false);
            if (blinkOffDuration > 0f)
            {
                yield return new WaitForSeconds(blinkOffDuration);
            }

            SetRenderersVisible(renderers, true);
            if (blinkOnDuration > 0f)
            {
                yield return new WaitForSeconds(blinkOnDuration);
            }
        }

        if (patch.SupportCollider != null)
        {
            patch.SupportCollider.enabled = false;
        }

        if (hideVisualOnCollapse)
        {
            SetRenderersVisible(renderers, false);
        }
    }

    private void RestoreAllPatches()
    {
        ClearRendererCaches();

        for (int i = 0; i < patches.Count; i++)
        {
            SnowPatchEntry patch = patches[i];
            if (patch == null)
            {
                continue;
            }

            if (patch.SupportCollider != null)
            {
                patch.SupportCollider.enabled = true;
            }

            SetRenderersVisible(patch.GetRenderers(), true);
        }
    }

    private void ClearRendererCaches()
    {
        for (int i = 0; i < patches.Count; i++)
        {
            SnowPatchEntry patch = patches[i];
            if (patch != null)
            {
                patch.ClearCache();
            }
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
            Renderer renderer = renderers[i];
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    private static Transform FindPlayerTransform()
    {
        PlayerRuntimeContext runtimeContext = PlayerRuntimeContext.FindInScene();
        if (runtimeContext != null && runtimeContext.FormRoot != null)
        {
            return runtimeContext.FormRoot.transform;
        }

        PlayerFormRoot formRoot = Object.FindObjectOfType<PlayerFormRoot>();
        return formRoot != null ? formRoot.transform : null;
    }
}
