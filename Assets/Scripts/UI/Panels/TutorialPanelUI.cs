using System;
using TMPro;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialPanelUI : PanelUIBase
{
    private enum TutorialTab
    {
        Basics = 0,
        Forms = 1,
        Routes = 2
    }

    [Header("References")]
    [LabelText("关闭按钮")]
    [SerializeField] private Button closeButton;
    [LabelText("基础标签按钮")]
    [SerializeField] private Button basicsTabButton;
    [LabelText("形态标签按钮")]
    [SerializeField] private Button formsTabButton;
    [LabelText("路段标签按钮")]
    [SerializeField] private Button routesTabButton;
    [LabelText("基础标签底图")]
    [SerializeField] private Image basicsTabImage;
    [LabelText("形态标签底图")]
    [SerializeField] private Image formsTabImage;
    [LabelText("路段标签底图")]
    [SerializeField] private Image routesTabImage;
    [LabelText("基础内容")]
    [SerializeField] private GameObject basicsContent;
    [LabelText("形态内容")]
    [SerializeField] private GameObject formsContent;
    [LabelText("路段内容")]
    [SerializeField] private GameObject routesContent;

    [Header("Style")]
    [LabelText("选中标签颜色")]
    [SerializeField] private Color selectedTabColor = new Color(0.93f, 0.79f, 0.47f, 1f);
    [LabelText("未选中标签颜色")]
    [SerializeField] private Color idleTabColor = Color.white;

    public event Action CloseRequested;

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
        ApplyTab(TutorialTab.Basics);
    }

    public override void Show()
    {
        base.Show();
        ApplyTab(TutorialTab.Basics);
    }

    private void BindButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        if (basicsTabButton != null)
        {
            basicsTabButton.onClick.RemoveListener(HandleBasicsTabClicked);
            basicsTabButton.onClick.AddListener(HandleBasicsTabClicked);
        }

        if (formsTabButton != null)
        {
            formsTabButton.onClick.RemoveListener(HandleFormsTabClicked);
            formsTabButton.onClick.AddListener(HandleFormsTabClicked);
        }

        if (routesTabButton != null)
        {
            routesTabButton.onClick.RemoveListener(HandleRoutesTabClicked);
            routesTabButton.onClick.AddListener(HandleRoutesTabClicked);
        }
    }

    private void HandleCloseClicked()
    {
        SoundEffectPlayback.Play(SoundEffectId.Click);
        CloseRequested?.Invoke();
    }

    private void HandleBasicsTabClicked()
    {
        SoundEffectPlayback.Play(SoundEffectId.Click);
        ApplyTab(TutorialTab.Basics);
    }

    private void HandleFormsTabClicked()
    {
        SoundEffectPlayback.Play(SoundEffectId.Click);
        ApplyTab(TutorialTab.Forms);
    }

    private void HandleRoutesTabClicked()
    {
        SoundEffectPlayback.Play(SoundEffectId.Click);
        ApplyTab(TutorialTab.Routes);
    }

    private void ApplyTab(TutorialTab tab)
    {
        SetTabContentVisible(basicsContent, tab == TutorialTab.Basics);
        SetTabContentVisible(formsContent, tab == TutorialTab.Forms);
        SetTabContentVisible(routesContent, tab == TutorialTab.Routes);

        SetTabImageColor(basicsTabImage, tab == TutorialTab.Basics);
        SetTabImageColor(formsTabImage, tab == TutorialTab.Forms);
        SetTabImageColor(routesTabImage, tab == TutorialTab.Routes);
    }

    private void SetTabContentVisible(GameObject target, bool visible)
    {
        if (target != null)
        {
            target.SetActive(visible);
        }
    }

    private void SetTabImageColor(Image target, bool isSelected)
    {
        if (target != null)
        {
            target.color = isSelected ? selectedTabColor : idleTabColor;
        }
    }

    private void AutoBind()
    {
        closeButton = closeButton != null ? closeButton : FindButton("Panel/CloseButton");
        basicsTabButton = basicsTabButton != null ? basicsTabButton : FindButton("Panel/Tabs/BasicsTab");
        formsTabButton = formsTabButton != null ? formsTabButton : FindButton("Panel/Tabs/FormsTab");
        routesTabButton = routesTabButton != null ? routesTabButton : FindButton("Panel/Tabs/RoutesTab");
        basicsTabImage = basicsTabImage != null ? basicsTabImage : FindImage("Panel/Tabs/BasicsTab");
        formsTabImage = formsTabImage != null ? formsTabImage : FindImage("Panel/Tabs/FormsTab");
        routesTabImage = routesTabImage != null ? routesTabImage : FindImage("Panel/Tabs/RoutesTab");
        basicsContent = basicsContent != null ? basicsContent : FindGameObject("Panel/Contents/BasicsContent");
        formsContent = formsContent != null ? formsContent : FindGameObject("Panel/Contents/FormsContent");
        routesContent = routesContent != null ? routesContent : FindGameObject("Panel/Contents/RoutesContent");
    }

    private Button FindButton(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private Image FindImage(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private GameObject FindGameObject(string path)
    {
        Transform child = transform.Find(path);
        return child != null ? child.gameObject : null;
    }
}
