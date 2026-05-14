using TMPro;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelProgressUI : HudUIBase
{
    [Header("References")]
    [LabelText("关卡标题文本")]
    [SerializeField] private TMP_Text levelText;

    protected override void Reset()
    {
        base.Reset();
        AutoBind();
    }

    public override void Initialize()
    {
        AutoBind();
    }

    public void SetLevelText(string value)
    {
        if (levelText != null)
        {
            levelText.text = value;
        }
    }

    private void AutoBind()
    {
        levelText = levelText != null ? levelText : FindText("LevelText");
    }

    private TMP_Text FindText(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }
}
