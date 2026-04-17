using System;
using UnityEngine;

public class PlayerEnergyController : MonoBehaviour
{
    [Header("Energy")]
    [SerializeField] private float maxEnergy = GameConstants.DefaultMaxEnergy;
    [SerializeField] private float carEnergyCostPerSecond = GameConstants.DefaultCarEnergyCostPerSecond;
    [SerializeField] private float planeEnergyCostPerSecond = GameConstants.DefaultPlaneEnergyCostPerSecond;
    [SerializeField] private float boatEnergyCostPerSecond = GameConstants.DefaultBoatEnergyCostPerSecond;

    public float MaxEnergy => Mathf.Max(1f, maxEnergy);
    public float CurrentEnergy { get; private set; }

    public event Action<float, float> EnergyChanged;

    private void Awake()
    {
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
                return Mathf.Max(0f, carEnergyCostPerSecond);
            case PlayerFormType.Plane:
                return Mathf.Max(0f, planeEnergyCostPerSecond);
            case PlayerFormType.Boat:
                return Mathf.Max(0f, boatEnergyCostPerSecond);
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
