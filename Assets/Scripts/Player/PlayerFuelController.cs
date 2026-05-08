using System;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

public class PlayerFuelController : MonoBehaviour
{
    [LabelText("玩家调参配置")]
    [SerializeField] private PlayerTuningConfig tuningConfig;

    public float MaxFuel => tuningConfig != null ? tuningConfig.Survival.MaxFuel : GameConstants.DefaultMaxFuel;
    public float CurrentFuel { get; private set; }

    public event Action<float, float> FuelChanged;

    private void Awake()
    {
        if (tuningConfig == null)
        {
            tuningConfig = PlayerTuningConfig.Load();
        }

        CurrentFuel = MaxFuel;
        NotifyFuelChanged();
    }

    public void ResetFuel()
    {
        CurrentFuel = MaxFuel;
        NotifyFuelChanged();
    }

    public void SetFuelDirect(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0f, MaxFuel);
        if (Mathf.Approximately(CurrentFuel, clampedValue))
        {
            return;
        }

        CurrentFuel = clampedValue;
        NotifyFuelChanged();
    }

    public void ConsumeByForm(PlayerFormType currentForm, float deltaTime)
    {
        if (deltaTime <= 0f || IsEmpty())
        {
            return;
        }

        float consumeRate = GetConsumeRate(currentForm);
        if (consumeRate <= 0f)
        {
            return;
        }

        Consume(consumeRate * deltaTime);
    }

    public void ConsumeForTransform()
    {
        float transformCost = tuningConfig != null ? tuningConfig.Survival.TransformFuelCost : GameConstants.DefaultTransformFuelCost;
        if (transformCost <= 0f)
        {
            return;
        }

        Consume(transformCost);
    }

    public void ConsumeSprintExtraByForm(PlayerFormType currentForm, float deltaTime)
    {
        if (deltaTime <= 0f || IsEmpty())
        {
            return;
        }

        float consumeRate = GetConsumeRate(currentForm);
        if (consumeRate <= 0f)
        {
            return;
        }

        Consume(consumeRate * deltaTime);
    }

    public bool IsEmpty()
    {
        return CurrentFuel <= 0f;
    }

    public bool IsFull()
    {
        return CurrentFuel >= MaxFuel;
    }

    public void RestoreFuel(float amount)
    {
        if (amount <= 0f || IsFull())
        {
            return;
        }

        float previous = CurrentFuel;
        CurrentFuel = Mathf.Min(MaxFuel, CurrentFuel + amount);
        if (!Mathf.Approximately(previous, CurrentFuel))
        {
            NotifyFuelChanged();
        }
    }

    private float GetConsumeRate(PlayerFormType currentForm)
    {
        switch (currentForm)
        {
            case PlayerFormType.Car:
                return tuningConfig != null ? tuningConfig.Survival.CarFuelCostPerSecond : GameConstants.DefaultCarFuelCostPerSecond;
            case PlayerFormType.Plane:
                return tuningConfig != null ? tuningConfig.Survival.PlaneFuelCostPerSecond : GameConstants.DefaultPlaneFuelCostPerSecond;
            case PlayerFormType.Boat:
                return tuningConfig != null ? tuningConfig.Survival.BoatFuelCostPerSecond : GameConstants.DefaultBoatFuelCostPerSecond;
            default:
                return 0f;
        }
    }

    private void Consume(float amount)
    {
        if (amount <= 0f || IsEmpty())
        {
            return;
        }

        float previous = CurrentFuel;
        CurrentFuel = Mathf.Max(0f, CurrentFuel - amount);
        if (!Mathf.Approximately(previous, CurrentFuel))
        {
            NotifyFuelChanged();
        }
    }

    private void NotifyFuelChanged()
    {
        FuelChanged?.Invoke(CurrentFuel, MaxFuel);
    }
}
