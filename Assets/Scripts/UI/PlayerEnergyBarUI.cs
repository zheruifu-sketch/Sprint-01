using UnityEngine;
using UnityEngine.UI;

public class PlayerEnergyBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerEnergyController energyController;
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
        if (energyController != null)
        {
            energyController.EnergyChanged += HandleEnergyChanged;
            HandleEnergyChanged(energyController.CurrentEnergy, energyController.MaxEnergy);
        }
    }

    private void OnDisable()
    {
        if (energyController != null)
        {
            energyController.EnergyChanged -= HandleEnergyChanged;
        }
    }

    private void TryAutoBind()
    {
        if (energyController == null)
        {
            energyController = FindObjectOfType<PlayerEnergyController>();
        }

        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }
    }

    private void HandleEnergyChanged(float currentEnergy, float maxEnergy)
    {
        if (fillImage == null)
        {
            return;
        }

        float normalized = maxEnergy > 0f ? currentEnergy / maxEnergy : 0f;
        fillImage.fillAmount = Mathf.Clamp01(normalized);
    }
}
