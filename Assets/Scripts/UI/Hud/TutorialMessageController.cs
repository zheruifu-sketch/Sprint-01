using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class TutorialMessageController : MonoBehaviour
{
    public struct TutorialMessage
    {
        public string Title;
        public string Body;
        public bool PauseGameplay;
        public bool CloseOnAnyKey;
    }

    [Header("References")]
    [LabelText("教程卡片界面")]
    [SerializeField] private LevelCardUI levelCardUi;

    private TutorialMessage activeMessage;
    private bool isShowing;

    public bool IsShowing => isShowing;

    private void Awake()
    {
        levelCardUi = levelCardUi != null ? levelCardUi : FindObjectOfType<LevelCardUI>(true);
    }

    private void Update()
    {
        if (!isShowing || !activeMessage.CloseOnAnyKey)
        {
            return;
        }

        if (!Input.anyKeyDown)
        {
            return;
        }

        DismissActiveMessage();
    }

    public bool TryShow(TutorialMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Title) && string.IsNullOrWhiteSpace(message.Body))
        {
            return false;
        }

        levelCardUi = levelCardUi != null ? levelCardUi : FindObjectOfType<LevelCardUI>(true);
        if (levelCardUi == null)
        {
            return false;
        }

        activeMessage = message;
        isShowing = true;
        levelCardUi.ShowCard(message.Title, message.Body, 0f);

        if (message.PauseGameplay)
        {
            Time.timeScale = 0f;
        }

        return true;
    }

    public void DismissActiveMessage()
    {
        if (!isShowing)
        {
            return;
        }

        TutorialMessage message = activeMessage;
        activeMessage = default;
        isShowing = false;

        if (levelCardUi != null)
        {
            levelCardUi.HideImmediate();
        }

        if (message.PauseGameplay)
        {
            Time.timeScale = 1f;
        }
    }
}
