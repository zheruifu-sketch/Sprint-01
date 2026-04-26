using UnityEngine;
using UnityEngine.UI;

public class PlayerTransformCooldownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerRuleController ruleController;
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
        if (ruleController == null || fillImage == null)
        {
            return;
        }

        bool onCooldown = ruleController.IsTransformOnCooldown;
        if (onCooldown)
        {
            fillImage.fillAmount = ruleController.TransformCooldownNormalizedRemaining;
        }

        SetCooldownVisible(onCooldown);
    }

    private void TryAutoBind()
    {
        if (ruleController == null)
        {
            ruleController = FindObjectOfType<PlayerRuleController>();
        }

        if (cooldownUiRoot == null)
        {
            cooldownUiRoot = gameObject;
        }

        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }

        SetCooldownVisible(ruleController != null && ruleController.IsTransformOnCooldown);
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
