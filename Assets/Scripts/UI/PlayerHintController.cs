using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHintController : MonoBehaviour
{
    [Serializable]
    private class ZoneHintEntry
    {
        [SerializeField] private ZoneType zoneType = ZoneType.None;
        [SerializeField] private string message = string.Empty;
        [SerializeField] private PlayerFormType recommendedForm = PlayerFormType.Human;
        [SerializeField] private bool suppressWhenAlreadyUsingRecommendedForm = true;
        [SerializeField] private bool showOnlyOnce = true;
        [SerializeField] private float duration = 3f;

        public ZoneHintEntry()
        {
        }

        public ZoneHintEntry(
            ZoneType zoneType,
            string message,
            PlayerFormType recommendedForm,
            bool suppressWhenAlreadyUsingRecommendedForm = true,
            bool showOnlyOnce = true,
            float duration = 3f)
        {
            this.zoneType = zoneType;
            this.message = message;
            this.recommendedForm = recommendedForm;
            this.suppressWhenAlreadyUsingRecommendedForm = suppressWhenAlreadyUsingRecommendedForm;
            this.showOnlyOnce = showOnlyOnce;
            this.duration = duration;
        }

        public ZoneType ZoneType => zoneType;
        public string Message => message;
        public PlayerFormType RecommendedForm => recommendedForm;
        public bool SuppressWhenAlreadyUsingRecommendedForm => suppressWhenAlreadyUsingRecommendedForm;
        public bool ShowOnlyOnce => showOnlyOnce;
        public float Duration => duration;
    }

    [Header("References")]
    [SerializeField] private PlayerFormRoot playerFormRoot;
    [SerializeField] private PlayerZoneSensor zoneSensor;
    [SerializeField] private PlayerHintUI hintUI;

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

    private readonly Dictionary<ZoneType, bool> previousZoneStates = new Dictionary<ZoneType, bool>();
    private readonly HashSet<ZoneType> shownZoneHints = new HashSet<ZoneType>();
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
        zoneHints.Add(new ZoneHintEntry(ZoneType.Water, "Water ahead: try Boat form", PlayerFormType.Boat));
        zoneHints.Add(new ZoneHintEntry(ZoneType.Cliff, "Cliff ahead: try Plane form", PlayerFormType.Plane));
        zoneHints.Add(new ZoneHintEntry(ZoneType.Blizzard, "Blizzard ahead: use Human or Boat form", PlayerFormType.Human, false));
        zoneHints.Add(new ZoneHintEntry(ZoneType.Obstacle, "Obstacle ahead: avoid it or switch form", PlayerFormType.Human, false));
    }

    private void AutoBind()
    {
        if (playerFormRoot == null)
        {
            playerFormRoot = FindObjectOfType<PlayerFormRoot>();
        }

        if (zoneSensor == null && playerFormRoot != null)
        {
            zoneSensor = playerFormRoot.GetComponent<PlayerZoneSensor>();
        }

        if (zoneSensor == null)
        {
            zoneSensor = FindObjectOfType<PlayerZoneSensor>();
        }

        if (hintUI == null)
        {
            hintUI = FindObjectOfType<PlayerHintUI>(true);
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
            if (entry != null && entry.ZoneType != ZoneType.None && !string.IsNullOrWhiteSpace(entry.Message))
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
            if (entry == null || entry.ZoneType == ZoneType.None)
            {
                continue;
            }

            previousZoneStates[entry.ZoneType] = IsPlayerInZone(entry.ZoneType);
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
        ShowHint(introHintMessage, introHintDuration);
    }

    private void UpdateZoneHints()
    {
        if (zoneSensor == null)
        {
            return;
        }

        for (int i = 0; i < zoneHints.Count; i++)
        {
            ZoneHintEntry entry = zoneHints[i];
            if (entry == null || entry.ZoneType == ZoneType.None || string.IsNullOrWhiteSpace(entry.Message))
            {
                continue;
            }

            bool isInside = IsPlayerInZone(entry.ZoneType);
            bool wasInside = previousZoneStates.TryGetValue(entry.ZoneType, out bool value) && value;

            if (isInside && !wasInside && ShouldShowZoneHint(entry))
            {
                ShowHint(entry.Message, entry.Duration);
                if (entry.ShowOnlyOnce)
                {
                    shownZoneHints.Add(entry.ZoneType);
                }
            }

            previousZoneStates[entry.ZoneType] = isInside;
        }
    }

    private bool ShouldShowZoneHint(ZoneHintEntry entry)
    {
        if (entry.ShowOnlyOnce && shownZoneHints.Contains(entry.ZoneType))
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

    private bool IsPlayerInZone(ZoneType zoneType)
    {
        return zoneSensor != null && zoneSensor.IsInZone(zoneType);
    }

    private void ShowHint(string message, float duration)
    {
        if (hintUI == null)
        {
            return;
        }

        hintUI.ShowHint(message, duration);
    }
}
