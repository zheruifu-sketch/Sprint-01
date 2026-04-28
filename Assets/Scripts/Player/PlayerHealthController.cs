using System;
using UnityEngine;

public class PlayerHealthController : MonoBehaviour
{
    [SerializeField] private PlayerTuningConfig tuningConfig;

    public float MaxHealth => tuningConfig != null ? tuningConfig.Survival.MaxHealth : GameConstants.DefaultMaxHealth;
    public float HazardDamagePerSecond => tuningConfig != null ? tuningConfig.Survival.HazardDamagePerSecond : GameConstants.DefaultHazardDamagePerSecond;
    public float CurrentHealth { get; private set; }

    public event Action<float, float> HealthChanged;

    private void Awake()
    {
        if (tuningConfig == null)
        {
            tuningConfig = PlayerTuningConfig.Load();
        }

        CurrentHealth = MaxHealth;
        NotifyHealthChanged();
    }

    public void ResetHealth()
    {
        CurrentHealth = MaxHealth;
        NotifyHealthChanged();
    }

    public void ApplyHazardDamage(float deltaTime)
    {
        if (deltaTime <= 0f || IsDead())
        {
            return;
        }

        ApplyDamage(HazardDamagePerSecond * deltaTime);
    }

    public void ApplyDamage(float damage)
    {
        if (damage <= 0f || IsDead())
        {
            return;
        }

        float previous = CurrentHealth;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        if (!Mathf.Approximately(previous, CurrentHealth))
        {
            NotifyHealthChanged();
        }
    }

    public bool IsDead()
    {
        return CurrentHealth <= 0f;
    }

    public bool IsFull()
    {
        return CurrentHealth >= MaxHealth;
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead() || IsFull())
        {
            return;
        }

        float previous = CurrentHealth;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        if (!Mathf.Approximately(previous, CurrentHealth))
        {
            NotifyHealthChanged();
        }
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
