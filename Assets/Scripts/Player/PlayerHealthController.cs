using System;
using UnityEngine;

public class PlayerHealthController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = GameConstants.DefaultMaxHealth;
    [SerializeField] private float hazardDamagePerSecond = GameConstants.DefaultHazardDamagePerSecond;

    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public float HazardDamagePerSecond => Mathf.Max(0f, hazardDamagePerSecond);
    public float CurrentHealth { get; private set; }

    public event Action<float, float> HealthChanged;

    private void Awake()
    {
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

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
