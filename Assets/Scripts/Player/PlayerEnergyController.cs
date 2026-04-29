using System;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

public class PlayerEnergyController : MonoBehaviour
{
    [LabelText("玩家调参配置")]
    [SerializeField] private PlayerTuningConfig tuningConfig;

    public float MaxEnergy => tuningConfig != null ? tuningConfig.Survival.MaxEnergy : GameConstants.DefaultMaxEnergy;
    public float CurrentEnergy { get; private set; }

    public event Action<float, float> EnergyChanged;

    private void Awake()
    {
        if (tuningConfig == null)
        {
            tuningConfig = PlayerTuningConfig.Load();
        }

        CurrentEnergy = MaxEnergy;
        NotifyEnergyChanged();
    }

    public void ResetEnergy()
    {
        CurrentEnergy = MaxEnergy;
        NotifyEnergyChanged();
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

    public bool IsEmpty()
    {
        return CurrentEnergy <= 0f;
    }

    public bool IsFull()
    {
        return CurrentEnergy >= MaxEnergy;
    }

    public void RestoreEnergy(float amount)
    {
        if (amount <= 0f || IsFull())
        {
            return;
        }

        float previous = CurrentEnergy;
        CurrentEnergy = Mathf.Min(MaxEnergy, CurrentEnergy + amount);
        if (!Mathf.Approximately(previous, CurrentEnergy))
        {
            NotifyEnergyChanged();
        }
    }

    private float GetConsumeRate(PlayerFormType currentForm)
    {
        switch (currentForm)
        {
            case PlayerFormType.Car:
                return tuningConfig != null ? tuningConfig.Survival.CarEnergyCostPerSecond : GameConstants.DefaultCarEnergyCostPerSecond;
            case PlayerFormType.Plane:
                return tuningConfig != null ? tuningConfig.Survival.PlaneEnergyCostPerSecond : GameConstants.DefaultPlaneEnergyCostPerSecond;
            case PlayerFormType.Boat:
                return tuningConfig != null ? tuningConfig.Survival.BoatEnergyCostPerSecond : GameConstants.DefaultBoatEnergyCostPerSecond;
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

        float previous = CurrentEnergy;
        CurrentEnergy = Mathf.Max(0f, CurrentEnergy - amount);
        if (!Mathf.Approximately(previous, CurrentEnergy))
        {
            NotifyEnergyChanged();
        }
    }

    private void NotifyEnergyChanged()
    {
        EnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
    }
}
