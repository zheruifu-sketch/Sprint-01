using System.Collections;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelCardUI : HudUIBase
{
    [Header("References")]
    [LabelText("卡片根节点")]
    [SerializeField] private GameObject cardRoot;
    [LabelText("标题文本")]
    [SerializeField] private TMP_Text titleText;
    [LabelText("内容文本")]
    [SerializeField] private TMP_Text bodyText;

    private Coroutine hideCoroutine;

    public bool IsShowing => cardRoot != null && cardRoot.activeSelf;

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

    public void ShowCard(string title, string body, float duration)
    {
        AutoBind();
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (bodyText != null)
        {
            bodyText.text = body;
        }

        SetVisible(true);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        if (duration > 0f)
        {
            hideCoroutine = StartCoroutine(HideAfterDelay(duration));
        }
    }

    public void HideImmediate()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        SetVisible(false);
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        hideCoroutine = null;
        SetVisible(false);
    }

    private void AutoBind()
    {
        if (cardRoot == null)
        {
            Transform cardTransform = transform.Find("Card");
            cardRoot = cardTransform != null ? cardTransform.gameObject : null;
        }

        if (titleText == null && cardRoot != null)
        {
            Transform titleTransform = cardRoot.transform.Find("Title");
            titleText = titleTransform != null ? titleTransform.GetComponent<TMP_Text>() : null;
        }

        if (bodyText == null && cardRoot != null)
        {
            Transform bodyTransform = cardRoot.transform.Find("Body");
            bodyText = bodyTransform != null ? bodyTransform.GetComponent<TMP_Text>() : null;
        }
    }

    private void SetVisible(bool visible)
    {
        if (cardRoot == null || cardRoot.activeSelf == visible)
        {
            return;
        }

        cardRoot.SetActive(visible);
    }
}
