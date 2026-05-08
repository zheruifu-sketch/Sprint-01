using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class HealthPickupItem : PlayerCollectibleBase
{
    [Header("Health Pickup")]
    [LabelText("恢复血量")]
    [SerializeField] private float healAmount = 25f;

    protected override bool CanCollect(GameObject playerObject)
    {
        PlayerHealthController healthController = ResolveHealthController(playerObject);
        return healthController != null && !healthController.IsDead() && !healthController.IsFull();
    }

    protected override bool Collect(GameObject playerObject)
    {
        PlayerHealthController healthController = ResolveHealthController(playerObject);
        if (healthController == null || healthController.IsDead() || healthController.IsFull())
        {
            return false;
        }

        healthController.Heal(healAmount);
        return true;
    }

    private static PlayerHealthController ResolveHealthController(GameObject playerObject)
    {
        PlayerRuntimeContext runtimeContext = ResolveRuntimeContext(playerObject);
        return runtimeContext != null
            ? runtimeContext.HealthController
            : playerObject.GetComponent<PlayerHealthController>();
    }
}
