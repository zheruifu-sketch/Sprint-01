using System;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FixedActionButtonsUI : HudUIBase
{
    [Header("References")]
    [LabelText("说明按钮")]
    [SerializeField] private Button helpButton;
    [LabelText("重置关卡按钮")]
    [SerializeField] private Button resetLevelButton;

    public event Action HelpRequested;
    public event Action ResetLevelRequested;

    protected override void Reset()
    {
        base.Reset();
        AutoBind();
    }

    protected virtual void Awake()
    {
        AutoBind();
        BindButtons();
    }

    public override void Initialize()
    {
        AutoBind();
        BindButtons();
    }

    private void BindButtons()
    {
        if (helpButton != null)
        {
            helpButton.onClick.RemoveListener(HandleHelpClicked);
            helpButton.onClick.AddListener(HandleHelpClicked);
        }

        if (resetLevelButton != null)
        {
            resetLevelButton.onClick.RemoveListener(HandleResetLevelClicked);
            resetLevelButton.onClick.AddListener(HandleResetLevelClicked);
        }
    }

    private void HandleHelpClicked()
    {
        SoundEffectPlayback.Play(SoundEffectId.Click);
        HelpRequested?.Invoke();
    }

    private void HandleResetLevelClicked()
    {
        SoundEffectPlayback.Play(SoundEffectId.Click);
        ResetLevelRequested?.Invoke();
    }

    private void AutoBind()
    {
        helpButton = helpButton != null ? helpButton : FindButton("HelpButton");
        resetLevelButton = resetLevelButton != null ? resetLevelButton : FindButton("ResetLevelButton");
    }

    private Button FindButton(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<Button>() : null;
    }
}
