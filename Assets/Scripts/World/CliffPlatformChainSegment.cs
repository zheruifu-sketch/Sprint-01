using System.Collections;
using System.Collections.Generic;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class CliffPlatformChainSegment : SpecialRoadSegmentBase
{
    [System.Serializable]
    private class PlatformEntry
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
    [LabelText("平台列表")]
    [SerializeField] private List<PlatformEntry> platforms = new List<PlatformEntry>();
    [LabelText("掉落前延迟")]
    [SerializeField] private float collapseDelay = 0.22f;
    [LabelText("闪烁次数")]
    [SerializeField] private int blinkCount = 1;
    [LabelText("闪烁熄灭时长")]
    [SerializeField] private float blinkOffDuration = 0.05f;
    [LabelText("闪烁点亮时长")]
    [SerializeField] private float blinkOnDuration = 0.05f;

    [Header("State Control")]
    [LabelText("掉落后隐藏视觉")]
    [SerializeField] private bool hideVisualOnCollapse = true;
    [LabelText("每块只触发一次")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private readonly HashSet<PlatformEntry> triggeredPlatforms = new HashSet<PlatformEntry>();
    private Transform playerTransform;
    private Collider2D[] playerColliders;

    private void Reset()
    {
        EnsureHintDefaults(
            "cliff.platform-chain",
            "Falling platforms ahead. Each foothold vanishes moments after contact. Keep jumping forward.");
    }

    private void Awake()
    {
        EnsureHintDefaults(
            "cliff.platform-chain",
            "Falling platforms ahead. Each foothold vanishes moments after contact. Keep jumping forward.");
        CachePlayerReferences();
        ClearRendererCaches();
    }

    private void OnEnable()
    {
        triggeredPlatforms.Clear();
        RestoreAllPlatforms();
    }

    private void Update()
    {
        CachePlayerReferences();
        if (playerTransform == null)
        {
            return;
        }

        for (int i = 0; i < platforms.Count; i++)
        {
            PlatformEntry platform = platforms[i];
            if (platform == null || platform.SupportCollider == null)
            {
                continue;
            }

            if (triggerOnlyOnce && triggeredPlatforms.Contains(platform))
            {
                continue;
            }

            Collider2D supportCollider = platform.SupportCollider;
            if (!supportCollider.enabled || !IsPlayerTouchingPlatform(supportCollider))
            {
                continue;
            }

            triggeredPlatforms.Add(platform);
            StartCoroutine(CollapsePlatformRoutine(platform));
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

    private bool IsPlayerTouchingPlatform(Collider2D supportCollider)
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

    private IEnumerator CollapsePlatformRoutine(PlatformEntry platform)
    {
        if (platform == null)
        {
            yield break;
        }

        if (collapseDelay > 0f)
        {
            yield return new WaitForSeconds(collapseDelay);
        }

        List<Renderer> renderers = platform.GetRenderers();
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

        if (platform.SupportCollider != null)
        {
            platform.SupportCollider.enabled = false;
        }

        if (hideVisualOnCollapse)
        {
            SetRenderersVisible(renderers, false);
        }
    }

    private void RestoreAllPlatforms()
    {
        ClearRendererCaches();

        for (int i = 0; i < platforms.Count; i++)
        {
            PlatformEntry platform = platforms[i];
            if (platform == null)
            {
                continue;
            }

            if (platform.SupportCollider != null)
            {
                platform.SupportCollider.enabled = true;
            }

            SetRenderersVisible(platform.GetRenderers(), true);
        }
    }

    private void ClearRendererCaches()
    {
        for (int i = 0; i < platforms.Count; i++)
        {
            PlatformEntry platform = platforms[i];
            if (platform != null)
            {
                platform.ClearCache();
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
