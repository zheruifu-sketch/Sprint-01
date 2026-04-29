using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerRuntimeContext runtimeContext;
    [SerializeField] private PlayerHealthController healthController;
    [SerializeField] private Image fillImage;

    private void Reset()
    {
        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }
    }

    private void Awake()
    {
        TryAutoBind();
    }

    private void OnEnable()
    {
        if (healthController != null)
        {
            healthController.HealthChanged += HandleHealthChanged;
            HandleHealthChanged(healthController.CurrentHealth, healthController.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (healthController != null)
        {
            healthController.HealthChanged -= HandleHealthChanged;
        }
    }

    private void TryAutoBind()
    {
        runtimeContext = runtimeContext != null ? runtimeContext : PlayerRuntimeContext.FindInScene();
        if (runtimeContext != null)
        {
            runtimeContext.RefreshReferences();
            healthController = healthController != null ? healthController : runtimeContext.HealthController;
        }

        if (healthController == null)
        {
            healthController = FindObjectOfType<PlayerHealthController>();
        }

        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        if (fillImage == null)
        {
            return;
        }

        float normalized = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        fillImage.fillAmount = Mathf.Clamp01(normalized);
    }
}
