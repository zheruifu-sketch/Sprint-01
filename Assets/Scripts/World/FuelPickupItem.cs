using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class FuelPickupItem : PlayerCollectibleBase
{
    [Header("Fuel Pickup")]
    [LabelText("恢复燃料")]
    [SerializeField] private float fuelAmount = 25f;

    protected override bool CanCollect(GameObject playerObject)
    {
        PlayerFuelController fuelController = ResolveFuelController(playerObject);
        return fuelController != null;
    }

    protected override bool Collect(GameObject playerObject)
    {
        PlayerFuelController fuelController = ResolveFuelController(playerObject);
        if (fuelController == null)
        {
            return false;
        }

        fuelController.RestoreFuel(fuelAmount);
        GameSessionController.Instance?.AddPickupScore(10);
        return true;
    }

    private static PlayerFuelController ResolveFuelController(GameObject playerObject)
    {
        PlayerRuntimeContext runtimeContext = ResolveRuntimeContext(playerObject);
        return runtimeContext != null
            ? runtimeContext.FuelController
            : playerObject.GetComponent<PlayerFuelController>();
    }
}
