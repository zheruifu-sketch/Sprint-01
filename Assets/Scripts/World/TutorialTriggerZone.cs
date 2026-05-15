using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class TutorialTriggerZone : MonoBehaviour
{
    [Header("Tutorial")]
    [LabelText("标题")]
    [SerializeField] private string title = "Tutorial";
    [LabelText("内容")]
    [TextArea(3, 6)]
    [SerializeField] private string body = "Describe the tutorial step here.";
    [LabelText("暂停时间")]
    [SerializeField] private bool pauseGameplay = true;
    [LabelText("按任意键关闭")]
    [SerializeField] private bool closeOnAnyKey = true;
    [LabelText("触发器")]
    [SerializeField] private Collider2D triggerCollider;
    [LabelText("仅触发一次")]
    [SerializeField] private bool triggerOnce = true;

    private TutorialMessageController tutorialController;
    private bool hasTriggered;

    private void Reset()
    {
        CacheReferences();
    }

    private void Awake()
    {
        CacheReferences();
        tutorialController = FindObjectOfType<TutorialMessageController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered || other == null)
        {
            return;
        }

        PlayerFormRoot formRoot = other.GetComponentInParent<PlayerFormRoot>();
        if (formRoot == null)
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
            Destroy(gameObject);
        }
    }

    private void CacheReferences()
    {
        triggerCollider = triggerCollider != null ? triggerCollider : GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
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
