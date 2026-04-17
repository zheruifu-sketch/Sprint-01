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

        Debug.Log($"[Health] ApplyHazardDamage form={GetCurrentFormLabel()} deltaTime={deltaTime:F3} dps={HazardDamagePerSecond:F2} damage={HazardDamagePerSecond * deltaTime:F3}", this);
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
            Debug.Log($"[Health] ApplyDamage form={GetCurrentFormLabel()} damage={damage:F3} health={previous:F3}->{CurrentHealth:F3}", this);
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

    private string GetCurrentFormLabel()
    {
        PlayerFormRoot formRoot = GetComponent<PlayerFormRoot>();
        return formRoot != null ? formRoot.CurrentForm.ToString() : "Unknown";
    }
}
