using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class PickupItem : MonoBehaviour
{
    [Header("Deprecated")]
    [LabelText("旧道具配置(仅旧随机系统使用)")]
    [SerializeField] private PickupProfile pickupProfile;
    private bool collected;

    public void Initialize(PickupProfile pickupProfile)
    {
        this.pickupProfile = pickupProfile;
    }

    private void Reset()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Deprecated pickup entry for the old procedural generator only.
        // Manual levels now use prefab-bound collectible scripts instead:
        // HealthPickupItem / FuelPickupItem / CheckpointItem.
        if (collected || pickupProfile == null || other == null)
        {
            return;
        }

        PlayerFormRoot formRoot = other.GetComponentInParent<PlayerFormRoot>();
        if (formRoot == null)
        {
            return;
        }

        if (!ApplyToPlayer(formRoot.gameObject))
        {
            return;
        }

        collected = true;
        SoundEffectPlayback.Play(SoundEffectId.Pickup);
        Destroy(gameObject);
    }

    private bool ApplyToPlayer(GameObject playerObject)
    {
        if (pickupProfile == null || playerObject == null)
        {
            return false;
        }

        PlayerRuntimeContext runtimeContext = playerObject.GetComponent<PlayerRuntimeContext>();
        switch (pickupProfile.PickupType)
        {
            case PickupType.Health:
                PlayerHealthController healthController = runtimeContext != null
                    ? runtimeContext.HealthController
                    : playerObject.GetComponent<PlayerHealthController>();
                if (healthController == null || healthController.IsDead() || healthController.IsFull())
                {
                    return false;
                }

                healthController.Heal(pickupProfile.Amount);
                return true;

            case PickupType.Fuel:
                PlayerFuelController fuelController = runtimeContext != null
                    ? runtimeContext.FuelController
                    : playerObject.GetComponent<PlayerFuelController>();
                if (fuelController == null || fuelController.IsFull())
                {
                    return false;
                }

                fuelController.RestoreFuel(pickupProfile.Amount);
                return true;
        }

        return false;
    }
}
