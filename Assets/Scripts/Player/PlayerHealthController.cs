using System;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

public class PlayerHealthController : MonoBehaviour
{
    [LabelText("玩家调参配置")]
    [SerializeField] private PlayerTuningConfig tuningConfig;
    [LabelText("Buff控制器")]
    [SerializeField] private PlayerBuffController buffController;

    public float MaxHealth => tuningConfig != null ? tuningConfig.Survival.MaxHealth : GameConstants.DefaultMaxHealth;
    public float HazardDamagePerSecond => tuningConfig != null ? tuningConfig.Survival.HazardDamagePerSecond : GameConstants.DefaultHazardDamagePerSecond;
    public float CurrentHealth { get; private set; }

    public event Action<float, float> HealthChanged;
    public event Action<float, float, float> DamageTaken;

    private void Awake()
    {
        if (tuningConfig == null)
        {
            tuningConfig = PlayerTuningConfig.Load();
        }

        buffController = buffController != null ? buffController : GetComponent<PlayerBuffController>();

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

        if (buffController != null && buffController.HasBuff(PlayerBuffType.Shield))
        {
            return;
        }

        float previous = CurrentHealth;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        if (!Mathf.Approximately(previous, CurrentHealth))
        {
            DamageTaken?.Invoke(CurrentHealth, MaxHealth, previous - CurrentHealth);
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
