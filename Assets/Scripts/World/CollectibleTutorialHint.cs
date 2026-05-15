using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class CollectibleTutorialHint : MonoBehaviour
{
    [Header("Tutorial")]
    [LabelText("启用教程提示")]
    [SerializeField] private bool enableTutorialHint;
    [LabelText("标题")]
    [SerializeField] private string title = "Tutorial";
    [LabelText("内容")]
    [TextArea(3, 6)]
    [SerializeField] private string body = "Describe the tutorial step here.";
    [LabelText("暂停时间")]
    [SerializeField] private bool pauseGameplay = true;
    [LabelText("按任意键关闭")]
    [SerializeField] private bool closeOnAnyKey = true;
    [LabelText("仅触发一次")]
    [SerializeField] private bool triggerOnce = true;

    private TutorialMessageController tutorialController;
    private bool hasTriggered;

    private void Awake()
    {
        tutorialController = FindObjectOfType<TutorialMessageController>();
    }

    public void TryShowHint()
    {
        if (!enableTutorialHint)
        {
            return;
        }

        if (triggerOnce && hasTriggered)
        {
            return;
        }

        tutorialController = tutorialController != null ? tutorialController : FindObjectOfType<TutorialMessageController>();
        if (tutorialController == null || !tutorialController.TryShow(BuildMessage()))
        {
            return;
        }

        if (triggerOnce)
        {
            hasTriggered = true;
        }
    }

    private TutorialMessageController.TutorialMessage BuildMessage()
    {
        return new TutorialMessageController.TutorialMessage
        {
            Title = title,
            Body = body,
            PauseGameplay = pauseGameplay,
            CloseOnAnyKey = closeOnAnyKey
        };
    }
}
