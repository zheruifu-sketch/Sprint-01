using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class FailResultPanelUI : PanelUIBase
{
    [Header("References")]
    [LabelText("标题文本")]
    [SerializeField] private TMP_Text titleText;
    [LabelText("说明文本")]
    [SerializeField] private TMP_Text descriptionText;
    [LabelText("确认按钮")]
    [SerializeField] private Button confirmButton;
    [LabelText("次级按钮")]
    [SerializeField] private Button secondaryButton;

    public event Action ConfirmRequested;
    public event Action SecondaryRequested;

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

    public void SetContent(string title, string description, string secondaryButtonLabel = null)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }

        SetButtonLabel(confirmButton, "Restart");

        bool showSecondaryButton = !string.IsNullOrWhiteSpace(secondaryButtonLabel);
        if (secondaryButton != null)
        {
            secondaryButton.gameObject.SetActive(showSecondaryButton);
        }

        if (showSecondaryButton)
        {
            SetButtonLabel(secondaryButton, secondaryButtonLabel);
        }
    }

    private void BindButton()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        if (secondaryButton != null)
        {
            secondaryButton.onClick.RemoveListener(HandleSecondaryClicked);
            secondaryButton.onClick.AddListener(HandleSecondaryClicked);
        }
    }

    private void HandleConfirmClicked()
    {
        SoundEffectPlayback.Play(SoundEffectId.Click);
        ConfirmRequested?.Invoke();
    }

    private void HandleSecondaryClicked()
    {
        SoundEffectPlayback.Play(SoundEffectId.Click);
        SecondaryRequested?.Invoke();
    }

    private void AutoBind()
    {
        titleText = titleText != null ? titleText : FindText("Card/Title");
        descriptionText = descriptionText != null ? descriptionText : FindText("Card/Description");
        confirmButton = confirmButton != null ? confirmButton : FindButton("Card/StartButton");
        secondaryButton = secondaryButton != null ? secondaryButton : FindButton("Card/ExitButton");
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
