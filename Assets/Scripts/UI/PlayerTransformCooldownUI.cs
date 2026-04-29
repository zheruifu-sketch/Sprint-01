using UnityEngine;
using UnityEngine.UI;

public class PlayerTransformCooldownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerRuntimeContext runtimeContext;
    [SerializeField] private PlayerFormController formController;
    [SerializeField] private GameObject cooldownUiRoot;
    [SerializeField] private Image fillImage;

    private void Reset()
    {
        if (cooldownUiRoot == null)
        {
            cooldownUiRoot = gameObject;
        }

        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }
    }

    private void Awake()
    {
        TryAutoBind();
    }

    private void Update()
    {
        if (formController == null || fillImage == null)
        {
            return;
        }

        bool onCooldown = formController.IsTransformOnCooldown;
        if (onCooldown)
        {
            fillImage.fillAmount = formController.TransformCooldownNormalizedRemaining;
        }

        SetCooldownVisible(onCooldown);
    }

    private void TryAutoBind()
    {
        runtimeContext = runtimeContext != null ? runtimeContext : PlayerRuntimeContext.FindInScene();
        if (runtimeContext != null)
        {
            runtimeContext.RefreshReferences();
            formController = formController != null ? formController : runtimeContext.FormController;
        }

        if (formController == null)
        {
            formController = FindObjectOfType<PlayerFormController>();
        }

        if (cooldownUiRoot == null)
        {
            cooldownUiRoot = gameObject;
        }

        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }

        SetCooldownVisible(formController != null && formController.IsTransformOnCooldown);
    }

    private void SetCooldownVisible(bool visible)
    {
        if (cooldownUiRoot != null && cooldownUiRoot != gameObject)
        {
            if (cooldownUiRoot.activeSelf != visible)
            {
                cooldownUiRoot.SetActive(visible);
            }

            return;
        }

        if (fillImage != null && fillImage.enabled != visible)
        {
            fillImage.enabled = visible;
        }
    }
}
