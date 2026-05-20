using System.Collections;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RunIntroFadePanelUI : PanelUIBase
{
    [Header("References")]
    [LabelText("遮罩根节点")]
    [SerializeField] private GameObject fadeRoot;
    [LabelText("透明组")]
    [SerializeField] private CanvasGroup canvasGroup;
    [LabelText("黑幕图片")]
    [SerializeField] private Image fadeImage;

    protected override void Reset()
    {
        base.Reset();
        AutoBind();
    }

    protected override void Awake()
    {
        base.Awake();
        AutoBind();
        HideImmediate();
    }

    public override void Initialize()
    {
        AutoBind();
        HideImmediate();
    }

    public void ShowBlack()
    {
        AutoBind();
        SetOverlayState(1f, true);
    }

    public void ShowTransparent()
    {
        AutoBind();
        SetOverlayState(0f, true);
    }

    public void HideImmediate()
    {
        AutoBind();
        SetOverlayState(0f, false);
    }

    public IEnumerator PlayFadeFromBlackRoutine(float holdDuration, float fadeDuration)
    {
        ShowBlack();

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetOverlayState(1f - progress, true);
            yield return null;
        }

        HideImmediate();
    }

    public IEnumerator PlayFadeToBlackRoutine(float fadeDuration)
    {
        ShowTransparent();

        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetOverlayState(progress, true);
            yield return null;
        }

        ShowBlack();
    }

    private void AutoBind()
    {
        if (fadeRoot == null)
        {
            fadeRoot = gameObject;
        }

        if (canvasGroup == null && fadeRoot != null)
        {
            canvasGroup = fadeRoot.GetComponent<CanvasGroup>();
        }

        if (fadeImage == null && fadeRoot != null)
        {
            fadeImage = fadeRoot.GetComponent<Image>();
        }
    }

    private void SetOverlayState(float alpha, bool visible)
    {
        if (fadeRoot == null)
        {
            return;
        }

        if (fadeRoot.activeSelf != visible)
        {
            fadeRoot.SetActive(visible);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
            fadeImage.raycastTarget = visible;
        }
    }
}
