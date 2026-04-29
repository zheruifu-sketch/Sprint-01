using UnityEngine;

[DisallowMultipleComponent]
public class PickupItem : MonoBehaviour
{
    private PickupProfile pickupProfile;
    private bool collected;

    public void Initialize(PickupProfile pickupProfile)
    {
        this.pickupProfile = pickupProfile;
        EnsureTriggerCollider();
    }

    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
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

            case PickupType.Energy:
                PlayerEnergyController energyController = runtimeContext != null
                    ? runtimeContext.EnergyController
                    : playerObject.GetComponent<PlayerEnergyController>();
                if (energyController == null || energyController.IsFull())
                {
                    return false;
                }

                energyController.RestoreEnergy(pickupProfile.Amount);
                return true;
        }

        return false;
    }

    private void EnsureTriggerCollider()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider == null)
        {
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = 2.75f;
            circleCollider.isTrigger = true;
            triggerCollider = circleCollider;
        }

        triggerCollider.isTrigger = true;
    }
}
