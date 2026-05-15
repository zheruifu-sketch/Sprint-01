using TMPro;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelProgressUI : HudUIBase
{
    [Header("References")]
    [LabelText("关卡标题文本")]
    [SerializeField] private TMP_Text levelText;
    [LabelText("计时文本")]
    [SerializeField] private TMP_Text timerText;

    protected override void Reset()
    {
        base.Reset();
        AutoBind();
    }

    public override void Initialize()
    {
        AutoBind();
    }

    private void Update()
    {
        GameSessionController sessionController = GameSessionController.Instance;
        if (timerText != null && sessionController != null)
        {
            timerText.text = FormatElapsedTime(sessionController.LevelElapsedTime);
        }
    }

    public void SetLevelText(string value)
    {
        if (levelText != null)
        {
            levelText.text = value;
        }
    }

    public void SetTimerText(string value)
    {
        if (timerText != null)
        {
            timerText.text = value;
        }
    }

    private void AutoBind()
    {
        levelText = levelText != null ? levelText : FindText("LevelText");
        timerText = timerText != null ? timerText : FindText("TimerText");
    }

    private TMP_Text FindText(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static string FormatElapsedTime(float elapsedTime)
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        float seconds = elapsedTime - minutes * 60f;
        return $"{minutes:00}:{seconds:00.000}";
    }
}
