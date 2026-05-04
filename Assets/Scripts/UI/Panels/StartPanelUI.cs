using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class StartPanelUI : PanelUIBase
{
    [Header("References")]
    [LabelText("标题文本")]
    [SerializeField] private TMP_Text titleText;
    [LabelText("说明文本")]
    [SerializeField] private TMP_Text descriptionText;
    [LabelText("开始按钮")]
    [SerializeField] private Button startButton;
    [LabelText("按钮文本")]
    [SerializeField] private TMP_Text startButtonText;
    [LabelText("说明按钮")]
    [SerializeField] private Button helpButton;
    [LabelText("说明按钮文本")]
    [SerializeField] private TMP_Text helpButtonText;

    public event Action StartRequested;
    public event Action HelpRequested;

    protected override void Reset()
    {
        base.Reset();
        AutoBind();
    }

    protected override void Awake()
    {
        base.Awake();
        AutoBind();
        BindButton();
    }

    public override void Initialize()
    {
        AutoBind();
        BindButton();
    }

    public void SetContent(string title, string description, string buttonLabel)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }

        if (startButtonText != null)
        {
            startButtonText.text = buttonLabel;
        }
    }

    private void BindButton()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartClicked);
            startButton.onClick.AddListener(HandleStartClicked);
        }

        if (helpButton != null)
        {
            helpButton.onClick.RemoveListener(HandleHelpClicked);
            helpButton.onClick.AddListener(HandleHelpClicked);
        }
    }

    private void HandleStartClicked()
    {
        SoundEffectPlayback.Play(SoundEffectId.Click);
        StartRequested?.Invoke();
    }

    private void HandleHelpClicked()
    {
        SoundEffectPlayback.Play(SoundEffectId.Click);
        HelpRequested?.Invoke();
    }

    private void AutoBind()
    {
        titleText = titleText != null ? titleText : FindText("Card/Title", "Title");
        descriptionText = descriptionText != null ? descriptionText : FindText("Card/Description", "Description");
        startButton = startButton != null ? startButton : FindButton("Card/StartButton", "StartButton");
        startButtonText = startButtonText != null ? startButtonText : FindText("Card/StartButton/Label", "StartButton/Label");
        helpButton = helpButton != null ? helpButton : FindButton("Card/HelpButton", "HelpButton");
        helpButtonText = helpButtonText != null ? helpButtonText : FindText("Card/HelpButton/Label", "HelpButton/Label");
    }

    private TMP_Text FindText(params string[] paths)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            Transform child = transform.Find(paths[i]);
            if (child != null)
            {
                TMP_Text text = child.GetComponent<TMP_Text>();
                if (text != null)
                {
                    return text;
                }
            }
        }

        return null;
    }

    private Button FindButton(params string[] paths)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            Transform child = transform.Find(paths[i]);
            if (child != null)
            {
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    return button;
                }
            }
        }

        return null;
    }
}
