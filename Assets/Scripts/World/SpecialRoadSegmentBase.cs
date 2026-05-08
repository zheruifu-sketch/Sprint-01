using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

public abstract class SpecialRoadSegmentBase : MonoBehaviour
{
    [Header("Hint")]
    [LabelText("提示标识")]
    [SerializeField] private string hintId = string.Empty;

    public string HintId => hintId;
    public string FirstEncounterHintMessage
    {
        get
        {
            // Special road text now comes from the shared message catalog.
            // Segment scripts only keep a stable hintId so gameplay text no longer
            // needs to be duplicated across every road behaviour.
            GameMessageCatalog.SpecialRoadHintEntry entry = ResolveHintEntry();
            return entry != null ? entry.Message : string.Empty;
        }
    }

    public float HintDuration
    {
        get
        {
            GameMessageCatalog.SpecialRoadHintEntry entry = ResolveHintEntry();
            return entry != null ? entry.Duration : 2.8f;
        }
    }

    public bool ShowHintWhenPreviewed
    {
        get
        {
            GameMessageCatalog.SpecialRoadHintEntry entry = ResolveHintEntry();
            return entry == null || entry.ShowWhenPreviewed;
        }
    }

    public bool ShowHintOnlyOnce
    {
        get
        {
            GameMessageCatalog.SpecialRoadHintEntry entry = ResolveHintEntry();
            return entry == null || entry.ShowOnlyOnce;
        }
    }

    public bool HasValidHint => !string.IsNullOrWhiteSpace(HintId) && !string.IsNullOrWhiteSpace(FirstEncounterHintMessage);

    protected void EnsureHintId(string defaultHintId)
    {
        if (string.IsNullOrWhiteSpace(hintId))
        {
            hintId = defaultHintId;
        }
    }

    private GameMessageCatalog.SpecialRoadHintEntry ResolveHintEntry()
    {
        GameMessageCatalog messageCatalog = GameMessageCatalog.Load();
        return messageCatalog != null ? messageCatalog.GetSpecialRoadHint(hintId) : null;
    }
}
