using System;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameLevelController : MonoBehaviour
{
    [Header("Config")]
    [LabelText("关卡流程配置")]
    [SerializeField] private GameProgressionConfig progressionConfig;
    [LabelText("手工关卡流程配置")]
    [SerializeField] private ManualLevelFlowConfig manualFlowConfig;
    [LabelText("跑局控制器")]
    [SerializeField] private GameSessionController sessionController;
    [LabelText("手工关卡序列控制器")]
    [SerializeField] private ManualLevelSequenceController manualLevelSequenceController;

    public static GameLevelController Instance { get; private set; }

    public int LevelCount
    {
        get
        {
            ManualLevelSequenceController manualController = ResolveManualLevelSequenceController();
            if (manualController != null && manualController.IsManualLevelModeEnabled)
            {
                return manualController.LevelCount;
            }

            return progressionConfig != null ? progressionConfig.LevelCount : 0;
        }
    }
    public int CurrentLevelIndex { get; private set; }
    public int CurrentLevelNumber => CurrentLevelIndex + 1;
    public string CurrentLevelName
    {
        get
        {
            if (IsUsingManualLevelFlow())
            {
                ManualLevelFlowConfig.ManualLevelDefinition manualConfig = GetCurrentManualLevelConfig();
                return manualConfig != null && !string.IsNullOrWhiteSpace(manualConfig.LevelName)
                    ? manualConfig.LevelName
                    : $"Level {CurrentLevelNumber}";
            }

            GameProgressionConfig.LevelDefinition legacyConfig = progressionConfig != null ? progressionConfig.GetLevel(CurrentLevelIndex) : null;
            return legacyConfig != null && !string.IsNullOrWhiteSpace(legacyConfig.LevelName)
                ? legacyConfig.LevelName
                : $"Level {CurrentLevelNumber}";
        }
    }

    public event Action<int> LevelChanged;

    private void Reset()
    {
        progressionConfig = GameProgressionConfig.Load();
        manualFlowConfig = ManualLevelFlowConfig.Load();
        sessionController = FindObjectOfType<GameSessionController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate GameLevelController found. Destroying the new instance.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        progressionConfig = progressionConfig != null ? progressionConfig : GameProgressionConfig.Load();
        manualFlowConfig = manualFlowConfig != null ? manualFlowConfig : ManualLevelFlowConfig.Load();
        sessionController = sessionController != null ? sessionController : FindObjectOfType<GameSessionController>();
        manualLevelSequenceController = manualLevelSequenceController != null
            ? manualLevelSequenceController
            : FindObjectOfType<ManualLevelSequenceController>();
        int defaultStartingLevel = IsUsingManualLevelFlow()
            ? (manualFlowConfig != null ? manualFlowConfig.DefaultStartingLevel : 1)
            : (progressionConfig != null ? progressionConfig.DefaultStartingLevel : 1);
        int initialLevel = sessionController != null && sessionController.HasActiveRun
            ? sessionController.CurrentLevelNumber
            : defaultStartingLevel;
        SetLevel(initialLevel - 1, false);
    }

    private void Start()
    {
        LevelChanged?.Invoke(CurrentLevelIndex);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        bool enableDebugHotkeys = IsUsingManualLevelFlow()
            ? (manualFlowConfig != null && manualFlowConfig.EnableDebugHotkeys)
            : (progressionConfig != null && progressionConfig.EnableDebugHotkeys);
        if (!enableDebugHotkeys)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            DebugLoadLevel(CurrentLevelIndex);
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            DebugLoadLevel(CurrentLevelIndex + 2);
        }
        else if (Input.GetKeyDown(KeyCode.Home))
        {
            DebugLoadLevel(1);
        }

        for (int i = 0; i < Mathf.Min(9, LevelCount); i++)
        {
            KeyCode levelHotkey = (KeyCode)((int)KeyCode.F1 + i);
            if (Input.GetKeyDown(levelHotkey))
            {
                DebugLoadLevel(i + 1);
                break;
            }
        }
    }

    public void SetLevel(int levelIndex, bool notifyListeners = true)
    {
        int levelCount = LevelCount;
        if (levelCount == 0)
        {
            CurrentLevelIndex = 0;
            return;
        }

        int clampedIndex = Mathf.Clamp(levelIndex, 0, levelCount - 1);
        bool changed = clampedIndex != CurrentLevelIndex;
        CurrentLevelIndex = clampedIndex;
        if (sessionController != null && sessionController.HasActiveRun)
        {
            sessionController.ResumeLevel(CurrentLevelNumber);
        }

        if (notifyListeners && changed)
        {
            LevelChanged?.Invoke(CurrentLevelIndex);
        }
    }

    public void NextLevel()
    {
        SetLevel(CurrentLevelIndex + 1);
    }

    public void PreviousLevel()
    {
        SetLevel(CurrentLevelIndex - 1);
    }

    private void DebugLoadLevel(int levelNumber)
    {
        int clampedLevelNumber = Mathf.Clamp(levelNumber, 1, Mathf.Max(1, LevelCount));

        if (sessionController != null)
        {
            sessionController.DebugEnterLevel(clampedLevelNumber);
        }

        if (IsUsingManualLevelFlow())
        {
            SetLevel(clampedLevelNumber - 1);

            ManualLevelSequenceController manualController = ResolveManualLevelSequenceController();
            manualController?.LoadLevel(clampedLevelNumber, false);

            PlayerRuntimeContext runtimeContext = PlayerRuntimeContext.FindInScene();
            if (runtimeContext != null && runtimeContext.RespawnController != null)
            {
                runtimeContext.RespawnController.RestorePlayerAtCurrentRespawn();
            }

            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    public bool IsFormUnlocked(PlayerFormType formType)
    {
        if (IsUsingManualLevelFlow())
        {
            return manualFlowConfig == null || manualFlowConfig.IsFormUnlocked(CurrentLevelIndex, formType);
        }

        return progressionConfig == null || progressionConfig.IsFormUnlocked(CurrentLevelIndex, formType);
    }

    public bool IsEnvironmentAllowed(EnvironmentType environmentType)
    {
        return progressionConfig == null || progressionConfig.IsEnvironmentAllowed(CurrentLevelIndex, environmentType);
    }

    public PlayerFormType GetFallbackUnlockedForm(PlayerFormType preferredForm = PlayerFormType.Human)
    {
        if (IsUsingManualLevelFlow())
        {
            return manualFlowConfig != null
                ? manualFlowConfig.GetFallbackUnlockedForm(CurrentLevelIndex, preferredForm)
                : preferredForm;
        }

        return progressionConfig != null
            ? progressionConfig.GetFallbackUnlockedForm(CurrentLevelIndex, preferredForm)
            : preferredForm;
    }

    public float GetCurrentTargetDistance()
    {
        if (IsUsingManualLevelFlow())
        {
            return 0f;
        }

        GameProgressionConfig.LevelDefinition config = progressionConfig != null ? progressionConfig.GetLevel(CurrentLevelIndex) : null;
        return config != null ? config.TargetDistance : 60f;
    }

    public float GetTransitionDelay()
    {
        if (IsUsingManualLevelFlow())
        {
            return manualFlowConfig != null ? manualFlowConfig.TransitionDelay : 1.25f;
        }

        return progressionConfig != null ? progressionConfig.TransitionDelay : 1.25f;
    }

    public string GetCurrentLevelDescription()
    {
        if (IsUsingManualLevelFlow())
        {
            ManualLevelFlowConfig.ManualLevelDefinition manualConfig = GetCurrentManualLevelConfig();
            return manualConfig != null ? manualConfig.Description : string.Empty;
        }

        GameProgressionConfig.LevelDefinition legacyConfig = progressionConfig != null ? progressionConfig.GetLevel(CurrentLevelIndex) : null;
        return legacyConfig != null ? legacyConfig.Description : string.Empty;
    }

    public string GetCurrentLevelStartHint()
    {
        if (IsUsingManualLevelFlow())
        {
            ManualLevelFlowConfig.ManualLevelDefinition manualConfig = GetCurrentManualLevelConfig();
            return manualConfig != null && !string.IsNullOrWhiteSpace(manualConfig.StartHint)
                ? manualConfig.StartHint
                : $"{CurrentLevelName} Start";
        }

        GameProgressionConfig.LevelDefinition legacyConfig = progressionConfig != null ? progressionConfig.GetLevel(CurrentLevelIndex) : null;
        if (legacyConfig == null || string.IsNullOrWhiteSpace(legacyConfig.StartHint))
        {
            return $"{CurrentLevelName} Start";
        }

        return legacyConfig.StartHint;
    }

    public string GetCurrentLevelClearHint()
    {
        if (IsUsingManualLevelFlow())
        {
            ManualLevelFlowConfig.ManualLevelDefinition manualConfig = GetCurrentManualLevelConfig();
            return manualConfig != null && !string.IsNullOrWhiteSpace(manualConfig.ClearHint)
                ? manualConfig.ClearHint
                : $"{CurrentLevelName} Clear";
        }

        GameProgressionConfig.LevelDefinition legacyConfig = progressionConfig != null ? progressionConfig.GetLevel(CurrentLevelIndex) : null;
        if (legacyConfig == null || string.IsNullOrWhiteSpace(legacyConfig.ClearHint))
        {
            return $"{CurrentLevelName} Clear";
        }

        return legacyConfig.ClearHint;
    }

    private ManualLevelSequenceController ResolveManualLevelSequenceController()
    {
        if (manualLevelSequenceController == null)
        {
            manualLevelSequenceController = FindObjectOfType<ManualLevelSequenceController>();
        }

        return manualLevelSequenceController;
    }

    private bool IsUsingManualLevelFlow()
    {
        ManualLevelSequenceController manualController = ResolveManualLevelSequenceController();
        return manualController != null && manualController.IsManualLevelModeEnabled;
    }

    private ManualLevelFlowConfig.ManualLevelDefinition GetCurrentManualLevelConfig()
    {
        if (manualFlowConfig == null)
        {
            manualFlowConfig = ManualLevelFlowConfig.Load();
        }

        return manualFlowConfig != null ? manualFlowConfig.GetLevel(CurrentLevelIndex) : null;
    }
}
