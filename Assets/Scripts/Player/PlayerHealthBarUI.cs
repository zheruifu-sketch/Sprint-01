using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerHealthController))]
public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealthController healthController;

    [Header("HUD")]
    [SerializeField] private Vector2 anchorMin = new Vector2(0.03f, 0.93f);
    [SerializeField] private Vector2 anchorMax = new Vector2(0.33f, 0.98f);

    private Image fillImage;

    private void Reset()
    {
        healthController = GetComponent<PlayerHealthController>();
    }

    private void Awake()
    {
        if (healthController == null)
        {
            healthController = GetComponent<PlayerHealthController>();
        }

        EnsureUi();
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

    private void EnsureUi()
    {
        Canvas canvas = null;
        GameObject existingHudCanvas = GameObject.Find("HUD Canvas");
        if (existingHudCanvas != null)
        {
            canvas = existingHudCanvas.GetComponent<Canvas>();
        }

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject barRoot = new GameObject("Health Bar", typeof(RectTransform), typeof(Image));
        barRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = barRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = anchorMin;
        rootRect.anchorMax = anchorMax;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image background = barRoot.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(barRoot.transform, false);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color(0.21f, 0.78f, 0.24f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
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
