using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlayerHintController : MonoBehaviour
{
    [Serializable]
    private class ZoneHintEntry
    {
        [FormerlySerializedAs("zoneType")]
        [SerializeField] private EnvironmentType environmentType = EnvironmentType.None;
        [SerializeField] private string message = string.Empty;
        [SerializeField] private PlayerFormType recommendedForm = PlayerFormType.Human;
        [SerializeField] private bool suppressWhenAlreadyUsingRecommendedForm = true;
        [SerializeField] private bool showOnlyOnce = true;
        [SerializeField] private float duration = 3f;

        public ZoneHintEntry()
        {
        }

        public ZoneHintEntry(
            EnvironmentType environmentType,
            string message,
            PlayerFormType recommendedForm,
            bool suppressWhenAlreadyUsingRecommendedForm = true,
            bool showOnlyOnce = true,
            float duration = 3f)
        {
            this.environmentType = environmentType;
            this.message = message;
            this.recommendedForm = recommendedForm;
            this.suppressWhenAlreadyUsingRecommendedForm = suppressWhenAlreadyUsingRecommendedForm;
            this.showOnlyOnce = showOnlyOnce;
            this.duration = duration;
        }

        public EnvironmentType EnvironmentType => environmentType;
        public string Message => message;
        public PlayerFormType RecommendedForm => recommendedForm;
        public bool SuppressWhenAlreadyUsingRecommendedForm => suppressWhenAlreadyUsingRecommendedForm;
        public bool ShowOnlyOnce => showOnlyOnce;
        public float Duration => duration;
    }

    [Header("References")]
    [SerializeField] private PlayerFormRoot playerFormRoot;
    [SerializeField] private PlayerEnvironmentContext environmentContext;
    [SerializeField] private PlayerHintUI hintUI;
    [SerializeField] private GameLevelController levelController;

    [Header("Intro Hint")]
    [SerializeField] private bool showIntroHintOnStart = true;
    [SerializeField] private string introHintMessage = "Press 1-4 to transform";
    [SerializeField] private float introHintDelay = 0.6f;
    [SerializeField] private float introHintDuration = 3.5f;

    [Header("Zone Hints")]
    [SerializeField] private List<ZoneHintEntry> zoneHints = new List<ZoneHintEntry>
    {
        new ZoneHintEntry(),
        new ZoneHintEntry()
    };

    private readonly Dictionary<EnvironmentType, bool> previousZoneStates = new Dictionary<EnvironmentType, bool>();
    private readonly HashSet<EnvironmentType> shownZoneHints = new HashSet<EnvironmentType>();
    private float introHintTimer;
    private bool introHintQueued;

    private void Reset()
    {
        AutoBind();
        EnsureDefaultZoneHints();
    }

    private void Awake()
    {
        AutoBind();
        EnsureDefaultZoneHints();
        PrimeZoneStates();
        introHintTimer = introHintDelay;
        introHintQueued = showIntroHintOnStart && !string.IsNullOrWhiteSpace(introHintMessage);
    }

    private void Update()
    {
        UpdateIntroHint();
        UpdateZoneHints();
    }

    [ContextMenu("Reset Zone Hints To Defaults")]
    private void ResetZoneHintsToDefaults()
    {
        zoneHints.Clear();
        zoneHints.Add(new ZoneHintEntry(EnvironmentType.Water, "Water ahead: try Boat form", PlayerFormType.Boat));
        zoneHints.Add(new ZoneHintEntry(EnvironmentType.Cliff, "Cliff ahead: try Plane form", PlayerFormType.Plane));
        zoneHints.Add(new ZoneHintEntry(EnvironmentType.Blizzard, "Blizzard ahead: use Human or Boat form", PlayerFormType.Human, false));
        zoneHints.Add(new ZoneHintEntry(EnvironmentType.Obstacle, "Obstacle ahead: avoid it or switch form", PlayerFormType.Human, false));
    }

    private void AutoBind()
    {
        if (playerFormRoot == null)
        {
            playerFormRoot = FindObjectOfType<PlayerFormRoot>();
        }

        if (environmentContext == null && playerFormRoot != null)
        {
            environmentContext = playerFormRoot.GetComponent<PlayerEnvironmentContext>();
        }

        if (environmentContext == null)
        {
            environmentContext = FindObjectOfType<PlayerEnvironmentContext>();
        }

        if (hintUI == null)
        {
            hintUI = FindObjectOfType<PlayerHintUI>(true);
        }

        if (levelController == null)
        {
            levelController = GameLevelController.GetOrCreateInstance();
        }
    }

    private void EnsureDefaultZoneHints()
    {
        if (zoneHints.Count > 0 && HasConfiguredZoneHint())
        {
            return;
        }

        ResetZoneHintsToDefaults();
    }

    private bool HasConfiguredZoneHint()
    {
        for (int i = 0; i < zoneHints.Count; i++)
        {
            ZoneHintEntry entry = zoneHints[i];
            if (entry != null && entry.EnvironmentType != EnvironmentType.None && !string.IsNullOrWhiteSpace(entry.Message))
            {
                return true;
            }
        }

        return false;
    }

    private void PrimeZoneStates()
    {
        previousZoneStates.Clear();
        for (int i = 0; i < zoneHints.Count; i++)
        {
            ZoneHintEntry entry = zoneHints[i];
            if (entry == null || entry.EnvironmentType == EnvironmentType.None)
            {
                continue;
            }

            previousZoneStates[entry.EnvironmentType] = IsPlayerInEnvironment(entry.EnvironmentType);
        }
    }

    private void UpdateIntroHint()
    {
        if (!introHintQueued)
        {
            return;
        }

        introHintTimer -= Time.deltaTime;
        if (introHintTimer > 0f)
        {
            return;
        }

        introHintQueued = false;
        ShowHint(ResolveIntroHintMessage(), introHintDuration);
    }

    private void UpdateZoneHints()
    {
        if (environmentContext == null)
        {
            return;
        }

        for (int i = 0; i < zoneHints.Count; i++)
        {
            ZoneHintEntry entry = zoneHints[i];
            if (entry == null || entry.EnvironmentType == EnvironmentType.None || string.IsNullOrWhiteSpace(entry.Message))
            {
                continue;
            }

            bool isInside = IsPlayerInEnvironment(entry.EnvironmentType);
            bool wasInside = previousZoneStates.TryGetValue(entry.EnvironmentType, out bool value) && value;

            if (isInside && !wasInside && ShouldShowZoneHint(entry))
            {
                ShowHint(entry.Message, entry.Duration);
                if (entry.ShowOnlyOnce)
                {
                    shownZoneHints.Add(entry.EnvironmentType);
                }
            }

            previousZoneStates[entry.EnvironmentType] = isInside;
        }
    }

    private bool ShouldShowZoneHint(ZoneHintEntry entry)
    {
        if (entry.ShowOnlyOnce && shownZoneHints.Contains(entry.EnvironmentType))
        {
            return false;
        }

        if (entry.SuppressWhenAlreadyUsingRecommendedForm &&
            playerFormRoot != null &&
            playerFormRoot.CurrentForm == entry.RecommendedForm)
        {
            return false;
        }

        return true;
    }

    private bool IsPlayerInEnvironment(EnvironmentType environmentType)
    {
        return environmentContext != null && environmentContext.IsInEnvironment(environmentType);
    }

    private void ShowHint(string message, float duration)
    {
        if (hintUI == null)
        {
            return;
        }

        hintUI.ShowHint(message, duration);
    }

    private string ResolveIntroHintMessage()
    {
        if (levelController == null)
        {
            return introHintMessage;
        }

        List<string> hotkeys = new List<string>();
        if (levelController.IsFormUnlocked(PlayerFormType.Human))
        {
            hotkeys.Add("1-Human");
        }

        if (levelController.IsFormUnlocked(PlayerFormType.Car))
        {
            hotkeys.Add("2-Car");
        }

        if (levelController.IsFormUnlocked(PlayerFormType.Plane))
        {
            hotkeys.Add("3-Plane");
        }

        if (levelController.IsFormUnlocked(PlayerFormType.Boat))
        {
            hotkeys.Add("4-Boat");
        }

        if (hotkeys.Count == 0)
        {
            return introHintMessage;
        }

        return $"Press {string.Join(" / ", hotkeys)} to transform";
    }
}
