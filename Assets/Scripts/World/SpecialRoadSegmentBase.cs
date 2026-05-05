using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

public abstract class SpecialRoadSegmentBase : MonoBehaviour
{
    [Header("Hint")]
    [LabelText("提示标识")]
    [SerializeField] private string hintId = string.Empty;
    [LabelText("首次提示文本")]
    [TextArea(2, 4)]
    [SerializeField] private string firstEncounterHintMessage = string.Empty;
    [LabelText("提示持续时长")]
    [SerializeField] private float hintDuration = 2.8f;
    [LabelText("预览到路段时即可提示")]
    [SerializeField] private bool showHintWhenPreviewed = true;
    [LabelText("首次仅提示一次")]
    [SerializeField] private bool showHintOnlyOnce = true;

    public string HintId => hintId;
    public string FirstEncounterHintMessage => firstEncounterHintMessage;
    public float HintDuration => Mathf.Max(0.5f, hintDuration);
    public bool ShowHintWhenPreviewed => showHintWhenPreviewed;
    public bool ShowHintOnlyOnce => showHintOnlyOnce;
    public bool HasValidHint => !string.IsNullOrWhiteSpace(hintId) && !string.IsNullOrWhiteSpace(firstEncounterHintMessage);

    protected void EnsureHintDefaults(string defaultHintId, string defaultMessage, float defaultDuration = 2.8f)
    {
        if (string.IsNullOrWhiteSpace(hintId))
        {
            hintId = defaultHintId;
        }

        if (string.IsNullOrWhiteSpace(firstEncounterHintMessage))
        {
            firstEncounterHintMessage = defaultMessage;
        }

        if (hintDuration <= 0f)
        {
            hintDuration = defaultDuration;
        }
    }
}
