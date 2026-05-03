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
    [LabelText("跑局控制器")]
    [SerializeField] private GameSessionController sessionController;

    public static GameLevelController Instance { get; private set; }

    public int LevelCount => progressionConfig != null ? progressionConfig.LevelCount : 0;
    public int CurrentLevelIndex { get; private set; }
    public int CurrentLevelNumber => CurrentLevelIndex + 1;
    public GameProgressionConfig.LevelDefinition CurrentLevelConfig => progressionConfig != null ? progressionConfig.GetLevel(CurrentLevelIndex) : null;
    public string CurrentLevelName
    {
        get
        {
            GameProgressionConfig.LevelDefinition config = CurrentLevelConfig;
            return config != null && !string.IsNullOrWhiteSpace(config.LevelName)
                ? config.LevelName
                : $"Level {CurrentLevelNumber}";
        }
    }

    public event Action<int> LevelChanged;

    private void Reset()
    {
        progressionConfig = GameProgressionConfig.Load();
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
        sessionController = sessionController != null ? sessionController : FindObjectOfType<GameSessionController>();
        int defaultStartingLevel = progressionConfig != null ? progressionConfig.DefaultStartingLevel : 1;
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
        if (progressionConfig == null || !progressionConfig.EnableDebugHotkeys)
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
        if (progressionConfig == null || progressionConfig.LevelCount == 0)
        {
            CurrentLevelIndex = 0;
            return;
        }

        int clampedIndex = Mathf.Clamp(levelIndex, 0, progressionConfig.LevelCount - 1);
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

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    public bool IsFormUnlocked(PlayerFormType formType)
    {
        return progressionConfig == null || progressionConfig.IsFormUnlocked(CurrentLevelIndex, formType);
    }

    public bool IsEnvironmentAllowed(EnvironmentType environmentType)
    {
        return progressionConfig == null || progressionConfig.IsEnvironmentAllowed(CurrentLevelIndex, environmentType);
    }

    public PlayerFormType GetFallbackUnlockedForm(PlayerFormType preferredForm = PlayerFormType.Human)
    {
        return progressionConfig != null
            ? progressionConfig.GetFallbackUnlockedForm(CurrentLevelIndex, preferredForm)
            : preferredForm;
    }

    public float GetCurrentTargetDistance()
    {
        GameProgressionConfig.LevelDefinition config = CurrentLevelConfig;
        return config != null ? config.TargetDistance : 60f;
    }

    public float GetTransitionDelay()
    {
        return progressionConfig != null ? progressionConfig.TransitionDelay : 1.25f;
    }

    public string GetCurrentLevelDescription()
    {
        GameProgressionConfig.LevelDefinition config = CurrentLevelConfig;
        if (config == null)
        {
            return string.Empty;
        }

        return config.Description;
    }

    public string GetCurrentLevelStartHint()
    {
        GameProgressionConfig.LevelDefinition config = CurrentLevelConfig;
        if (config == null || string.IsNullOrWhiteSpace(config.StartHint))
        {
            return $"{CurrentLevelName} Start";
        }

        return config.StartHint;
    }

    public string GetCurrentLevelClearHint()
    {
        GameProgressionConfig.LevelDefinition config = CurrentLevelConfig;
        if (config == null || string.IsNullOrWhiteSpace(config.ClearHint))
        {
            return $"{CurrentLevelName} Clear";
        }

        return config.ClearHint;
    }
}
