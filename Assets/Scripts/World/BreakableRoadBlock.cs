using System.Collections;
using System.Collections.Generic;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class BreakableRoadBlock : SpecialRoadSegmentBase
{
    [Header("Break Requirement")]
    [LabelText("必须为汽车形态")]
    [SerializeField] private bool requireCarForm = true;
    [LabelText("必须按住 Shift 加速")]
    [SerializeField] private bool requireSprint = true;
    [LabelText("最小撞开速度")]
    [SerializeField] private float requiredForwardSpeed = 12f;

    [Header("Break Result")]
    [LabelText("撞开后隐藏表现")]
    [SerializeField] private bool hideRenderersOnBreak = true;
    [LabelText("撞开后禁用碰撞")]
    [SerializeField] private bool disableCollidersOnBreak = true;
    [LabelText("撞开后延迟关闭碰撞")]
    [SerializeField] private float colliderDisableDelay = 0.2f;
    [LabelText("撞开闪烁次数")]
    [SerializeField] private int blinkCountOnBreak = 2;
    [LabelText("闪烁熄灭时长")]
    [SerializeField] private float blinkOffDuration = 0.05f;
    [LabelText("闪烁点亮时长")]
    [SerializeField] private float blinkOnDuration = 0.05f;
    [LabelText("仅能撞开一次")]
    [SerializeField] private bool breakOnlyOnce = true;

    [Header("References")]
    [LabelText("控制的渲染器")]
    [SerializeField] private List<Renderer> controlledRenderers = new List<Renderer>();
    [LabelText("控制的碰撞体")]
    [SerializeField] private List<Collider2D> controlledColliders = new List<Collider2D>();

    private bool broken;
    private bool breaking;
    private readonly List<BreakableRoadBlockColliderRelay> colliderRelays = new List<BreakableRoadBlockColliderRelay>();

    private void Reset()
    {
        CollectTargets();
    }

    private void Awake()
    {
        CollectTargets();
        AttachColliderRelays();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < colliderRelays.Count; i++)
        {
            BreakableRoadBlockColliderRelay relay = colliderRelays[i];
            if (relay != null)
            {
                relay.SetOwner(null);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryBreak(
            collision != null ? collision.collider : null,
            collision != null ? Mathf.Abs(collision.relativeVelocity.x) : -1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryBreak(other, -1f);
    }

    private void TryBreak(Collider2D other, float overrideForwardSpeed)
    {
        if (breaking || (broken && breakOnlyOnce))
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        PlayerFormRoot formRoot = other.GetComponentInParent<PlayerFormRoot>();
        if (formRoot == null)
        {
            return;
        }

        if (requireCarForm && formRoot.CurrentForm != PlayerFormType.Car)
        {
            return;
        }

        PlayerInputReader inputReader = formRoot.GetComponent<PlayerInputReader>();
        if (requireSprint && (inputReader == null || !inputReader.IsSprintHeld))
        {
            return;
        }

        Rigidbody2D playerRigidbody = formRoot.PlayerRigidbody;
        float forwardSpeed = overrideForwardSpeed >= 0f
            ? overrideForwardSpeed
            : (playerRigidbody != null ? Mathf.Abs(playerRigidbody.velocity.x) : 0f);
        if (forwardSpeed < requiredForwardSpeed)
        {
            return;
        }

        BreakBlock();
    }

    private void BreakBlock()
    {
        if (breaking)
        {
            return;
        }

        breaking = true;
        broken = true;
        StartCoroutine(BreakRoutine());
    }

    private IEnumerator BreakRoutine()
    {
        if (disableCollidersOnBreak && colliderDisableDelay <= 0f)
        {
            SetCollidersEnabled(false);
        }

        if (blinkCountOnBreak > 0)
        {
            for (int i = 0; i < blinkCountOnBreak; i++)
            {
                SetRenderersVisible(false);
                if (blinkOffDuration > 0f)
                {
                    yield return new WaitForSeconds(blinkOffDuration);
                }

                SetRenderersVisible(true);
                if (blinkOnDuration > 0f)
                {
                    yield return new WaitForSeconds(blinkOnDuration);
                }
            }
        }

        if (disableCollidersOnBreak && colliderDisableDelay > 0f)
        {
            yield return new WaitForSeconds(colliderDisableDelay);
            SetCollidersEnabled(false);
        }

        if (hideRenderersOnBreak)
        {
            SetRenderersVisible(false);
        }
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

        controlledRenderers.AddRange(GetComponentsInChildren<Renderer>(true));
        controlledColliders.AddRange(GetComponentsInChildren<Collider2D>(true));
        AttachColliderRelays();
    }

    private void AttachColliderRelays()
    {
        colliderRelays.Clear();

        // This block often keeps its actual hit colliders on child objects.
        // The parent gameplay script still needs those collision callbacks, so
        // every controlled collider gets a tiny relay that forwards back here.
        for (int i = 0; i < controlledColliders.Count; i++)
        {
            Collider2D cachedCollider = controlledColliders[i];
            if (cachedCollider == null)
            {
                continue;
            }

            BreakableRoadBlockColliderRelay relay = cachedCollider.GetComponent<BreakableRoadBlockColliderRelay>();
            if (relay == null)
            {
                relay = cachedCollider.gameObject.AddComponent<BreakableRoadBlockColliderRelay>();
            }

            relay.SetOwner(this);
            colliderRelays.Add(relay);
        }
    }

    public void HandleRelayCollision(Collider2D other, float overrideForwardSpeed)
    {
        TryBreak(other, overrideForwardSpeed);
    }
}
