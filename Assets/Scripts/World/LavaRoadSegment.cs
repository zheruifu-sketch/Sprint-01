using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class LavaRoadSegment : SpecialRoadSegmentBase
{
    [Header("References")]
    [LabelText("岩浆触发器")]
    [SerializeField] private Collider2D lavaTrigger;

    private LavaRoadTriggerRelay triggerRelay;

    private void Reset()
    {
        EnsureHintId("road.lava");
        CacheReferences();
    }

    private void Awake()
    {
        EnsureHintId("road.lava");
        CacheReferences();
    }

    private void OnDestroy()
    {
        if (triggerRelay != null)
        {
            triggerRelay.SetOwner(null);
        }
    }

    public void HandleTriggerEnter(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        PlayerRespawnController respawnController = other.GetComponentInParent<PlayerRespawnController>();
        if (respawnController == null)
        {
            return;
        }

        respawnController.Respawn(FailureType.FellIntoLava);
    }

    private void CacheReferences()
    {
        lavaTrigger = lavaTrigger != null ? lavaTrigger : GetComponent<Collider2D>();
        if (lavaTrigger == null)
        {
            return;
        }

        lavaTrigger.isTrigger = true;

        // Only the explicitly assigned lava collider should kill the player.
        // This avoids parent objects or unrelated child colliders triggering lava death in midair.
        triggerRelay = lavaTrigger.GetComponent<LavaRoadTriggerRelay>();
        if (triggerRelay == null)
        {
            triggerRelay = lavaTrigger.gameObject.AddComponent<LavaRoadTriggerRelay>();
        }

        triggerRelay.SetOwner(this);
    }
}
