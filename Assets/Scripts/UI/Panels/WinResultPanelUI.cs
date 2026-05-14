using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class WinResultPanelUI : PanelUIBase
{
    [Header("References")]
    [LabelText("标题文本")]
    [SerializeField] private TMP_Text titleText;
    [LabelText("说明文本")]
    [SerializeField] private TMP_Text descriptionText;
    [LabelText("下一关按钮")]
    [SerializeField] private Button nextLevelButton;
    [LabelText("返回首页按钮")]
    [SerializeField] private Button homeButton;

    public event Action NextLevelRequested;
    public event Action HomeRequested;

    protected override void Reset()
    {
        base.Reset();
        AutoBind();
    }

    protected override void Awake()
    {
        base.Awake();
        AutoBind();
        BindButtons();
    }

    public override void Initialize()
    {
        AutoBind();
        BindButtons();
    }

    public void SetContent(string title, string description, bool canGoToNextLevel)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }

        SetButtonLabel(nextLevelButton, "Next Level");
        SetButtonLabel(homeButton, "Back to Home");

        if (nextLevelButton != null)
        {
            nextLevelButton.interactable = canGoToNextLevel;
        }
    }

    private void BindButtons()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveListener(HandleNextLevelClicked);
            nextLevelButton.onClick.AddListener(HandleNextLevelClicked);
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(HandleHomeClicked);
            homeButton.onClick.AddListener(HandleHomeClicked);
        }
    }

    private void HandleNextLevelClicked()
    {
        if (nextLevelButton != null && !nextLevelButton.interactable)
        {
            return;
        }

        SoundEffectPlayback.Play(SoundEffectId.Click);
        NextLevelRequested?.Invoke();
    }

    private void HandleHomeClicked()
    {
        SoundEffectPlayback.Play(SoundEffectId.Click);
        HomeRequested?.Invoke();
    }

    private void AutoBind()
    {
        titleText = titleText != null ? titleText : FindText("Card/Title");
        descriptionText = descriptionText != null ? descriptionText : FindText("Card/Description");
        nextLevelButton = nextLevelButton != null ? nextLevelButton : FindButton("Card/NextLevelButton");
        homeButton = homeButton != null ? homeButton : FindButton("Card/HomeButton");
    }

    private TMP_Text FindText(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private Button FindButton(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private void SetButtonLabel(Button button, string text)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = text;
        }
    }
}
