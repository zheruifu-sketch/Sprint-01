using TMPro;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class RespawnCountdownPanelUI : PanelUIBase
{
    [Header("References")]
    [LabelText("倒计时文本")]
    [SerializeField] private TMP_Text countdownText;

    protected override void Reset()
    {
        base.Reset();
        AutoBind();
    }

    protected override void Awake()
    {
        base.Awake();
        AutoBind();
    }

    public override void Initialize()
    {
        AutoBind();
    }

    public void SetCountdownText(string value)
    {
        if (countdownText != null)
        {
            countdownText.text = value;
        }
    }

    private void AutoBind()
    {
        countdownText = countdownText != null ? countdownText : FindText("CountdownText");
    }

    private TMP_Text FindText(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }
}
