using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public abstract class PlayerCollectibleBase : MonoBehaviour
{
    [Header("Collectible")]
    [LabelText("触发器")]
    [SerializeField] protected Collider2D triggerCollider;
    [LabelText("拾取后销毁")]
    [SerializeField] private bool destroyOnCollect = true;

    private bool collected;

    protected virtual void Reset()
    {
        CacheCollectibleReferences();
    }

    protected virtual void Awake()
    {
        CacheCollectibleReferences();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other == null)
        {
            return;
        }

        PlayerFormRoot formRoot = other.GetComponentInParent<PlayerFormRoot>();
        if (formRoot == null)
        {
            return;
        }

        if (!CanCollect(formRoot.gameObject))
        {
            return;
        }

        if (!Collect(formRoot.gameObject))
        {
            return;
        }

        collected = true;
        SoundEffectPlayback.Play(SoundEffectId.Pickup);

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
    }

    protected virtual bool CanCollect(GameObject playerObject)
    {
        return playerObject != null;
    }

    protected abstract bool Collect(GameObject playerObject);

    protected void SetDestroyOnCollect(bool shouldDestroy)
    {
        destroyOnCollect = shouldDestroy;
    }

    protected static PlayerRuntimeContext ResolveRuntimeContext(GameObject playerObject)
    {
        return playerObject != null ? playerObject.GetComponent<PlayerRuntimeContext>() : null;
    }

    private void CacheCollectibleReferences()
    {
        triggerCollider = triggerCollider != null ? triggerCollider : GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }
}
