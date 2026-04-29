using System.Collections;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameFlowController : MonoBehaviour
{
    private enum FlowUiState
    {
        Start = 0,
        Gameplay = 1,
        Completed = 2
    }

    [Header("References")]
    [LabelText("关卡控制器")]
    [SerializeField] private GameLevelController levelController;
    [LabelText("运行会话控制器")]
    [SerializeField] private GameSessionController sessionController;
    [LabelText("玩家节点")]
    [SerializeField] private Transform player;
    [LabelText("界面管理器")]
    [SerializeField] private UIManager uiManager;

    [Header("Text")]
    [LabelText("开始按钮文本")]
    [SerializeField] private string startButtonText = "Start";
    private bool isTransitioning;
    private float levelStartX;

    public static GameFlowController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        levelController = levelController != null ? levelController : FindObjectOfType<GameLevelController>();
        sessionController = sessionController != null ? sessionController : FindObjectOfType<GameSessionController>();
        player = player != null ? player : FindPlayerTransform();
        uiManager = uiManager != null ? uiManager : FindObjectOfType<UIManager>(true);
        BindUiEvents();
    }

    private void Start()
    {
        BindUiEvents();
        levelStartX = GetPlayerX();
        RefreshLevelPresentation();

        if (sessionController != null && sessionController.HasActiveRun)
        {
            ResumeGameplay();
            ShowLevelHint();
            return;
        }

        PauseForStartScreen();
    }

    private void OnEnable()
    {
        if (levelController == null)
        {
            levelController = FindObjectOfType<GameLevelController>();
        }

        if (levelController != null)
        {
            levelController.LevelChanged += HandleLevelChanged;
        }

        if (sessionController == null)
        {
            sessionController = FindObjectOfType<GameSessionController>();
        }

        if (sessionController != null)
        {
            sessionController.RunStateChanged += HandleRunStateChanged;
            sessionController.RunLevelChanged += HandleRunLevelChanged;
        }
    }

    private void Update()
    {
        if (ShouldStartRunFromKeyboard())
        {
            BeginNewRun();
            return;
        }

        RefreshProgressText();

        if (sessionController == null || !sessionController.IsGameplayRunning || isTransitioning)
        {
            return;
        }

        if (GetDistanceTravelled() < GetCurrentTargetDistance())
        {
            return;
        }

        StartCoroutine(HandleLevelCompleted());
    }

    private bool ShouldStartRunFromKeyboard()
    {
        if (sessionController == null || sessionController.HasActiveRun)
        {
            return false;
        }

        if (isTransitioning)
        {
            return false;
        }

        return Input.GetKeyDown(KeyCode.A)
               || Input.GetKeyDown(KeyCode.D)
               || Input.GetKeyDown(KeyCode.W)
               || Input.GetKeyDown(KeyCode.S)
               || Input.GetKeyDown(KeyCode.LeftArrow)
               || Input.GetKeyDown(KeyCode.RightArrow)
               || Input.GetKeyDown(KeyCode.UpArrow)
               || Input.GetKeyDown(KeyCode.DownArrow)
               || Input.GetKeyDown(KeyCode.Space)
               || Input.GetKeyDown(KeyCode.LeftShift)
               || Input.GetKeyDown(KeyCode.RightShift)
               || Input.GetKeyDown(KeyCode.Alpha1)
               || Input.GetKeyDown(KeyCode.Alpha2)
               || Input.GetKeyDown(KeyCode.Alpha3)
               || Input.GetKeyDown(KeyCode.Alpha4);
    }

    private void OnDisable()
    {
        if (levelController != null)
        {
            levelController.LevelChanged -= HandleLevelChanged;
        }

        if (sessionController != null)
        {
            sessionController.RunStateChanged -= HandleRunStateChanged;
            sessionController.RunLevelChanged -= HandleRunLevelChanged;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void PauseForStartScreen()
    {
        Time.timeScale = 0f;
        isTransitioning = false;
        ApplyUiState(FlowUiState.Start);
    }

    private void ResumeGameplay()
    {
        Time.timeScale = 1f;
        isTransitioning = false;
        levelStartX = GetPlayerX();
        ApplyUiState(FlowUiState.Gameplay);
    }

    private void BeginNewRun()
    {
        if (sessionController != null)
        {
            sessionController.StartNewRun();
        }

        if (levelController != null)
        {
            levelController.SetLevel(0);
        }

        ResumeGameplay();
        ShowLevelHint();
    }

    private IEnumerator HandleLevelCompleted()
    {
        isTransitioning = true;
        Time.timeScale = 1f;
        if (sessionController != null)
        {
            sessionController.BeginTransition();
        }

        int currentLevel = levelController != null ? levelController.CurrentLevelNumber : 1;
        int levelCount = levelController != null ? Mathf.Max(1, levelController.LevelCount) : 3;
        float transitionDelay = levelController != null ? levelController.GetTransitionDelay() : 1.25f;

        if (currentLevel < levelCount)
        {
            string clearHint = levelController != null ? levelController.GetCurrentLevelClearHint() : $"Level {currentLevel} Clear";
            ShowHint(clearHint, transitionDelay);
            yield return new WaitForSeconds(transitionDelay);
            if (sessionController != null)
            {
                sessionController.AdvanceLevel(levelCount);
            }

            ReloadActiveScene();
            yield break;
        }

        ShowEndScreen();
    }

    private void RefreshHeader()
    {
        LevelProgressUI levelProgressUi = GetLevelProgressUi();
        if (levelProgressUi == null || levelController == null)
        {
            return;
        }

        levelProgressUi.SetLevelText($"Level {levelController.CurrentLevelNumber} / {Mathf.Max(1, levelController.LevelCount)}");
    }

    private void RefreshProgressText()
    {
        LevelProgressUI levelProgressUi = GetLevelProgressUi();
        if (levelProgressUi == null || levelController == null)
        {
            return;
        }

        float targetDistance = GetCurrentTargetDistance();
        float currentDistance = Mathf.Clamp(GetDistanceTravelled(), 0f, targetDistance);
        levelProgressUi.SetProgressText($"Goal {currentDistance:0}/{targetDistance:0}m");
    }

    private float GetCurrentTargetDistance()
    {
        return levelController != null ? levelController.GetCurrentTargetDistance() : 60f;
    }

    private float GetDistanceTravelled()
    {
        return Mathf.Max(0f, GetPlayerX() - levelStartX);
    }

    private float GetPlayerX()
    {
        return player != null ? player.position.x : 0f;
    }

    private void ReloadActiveScene()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void ShowLevelHint()
    {
        if (levelController == null)
        {
            return;
        }

        ShowHint(levelController.GetCurrentLevelStartHint(), 1.6f);
    }

    private void ShowHint(string message, float duration)
    {
        HintBarUI hintBarUi = GetHintBarUi();
        if (hintBarUi != null)
        {
            hintBarUi.ShowHint(message, duration);
        }
    }

    private string BuildStartDescription()
    {
        if (levelController != null)
        {
            string description = levelController.GetCurrentLevelDescription();
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description;
            }
        }

        float targetDistance = GetCurrentTargetDistance();
        string levelName = levelController != null ? levelController.CurrentLevelName : "Level";
        return $"{levelName} 准备开始。\n到达 {targetDistance:0}m 即可过关。";
    }

    private void BindUiEvents()
    {
        if (uiManager == null)
        {
            return;
        }

        StartPanelUI startPanelUi = uiManager.Get<StartPanelUI>();
        if (startPanelUi != null)
        {
            startPanelUi.StartRequested -= BeginNewRun;
            startPanelUi.StartRequested += BeginNewRun;
        }

        ResultPanelUI resultPanelUi = uiManager.Get<ResultPanelUI>();
        if (resultPanelUi != null)
        {
            resultPanelUi.ConfirmRequested -= HandleEndConfirmClicked;
            resultPanelUi.ConfirmRequested += HandleEndConfirmClicked;
        }
    }

    private void RefreshLevelPresentation()
    {
        RefreshHeader();
        RefreshProgressText();

        StartPanelUI startPanelUi = GetStartPanelUi();
        if (startPanelUi != null)
        {
            string title = levelController != null ? levelController.CurrentLevelName : "Level";
            string description = BuildStartDescription();
            startPanelUi.SetContent(title, description, startButtonText);
        }
    }

    private void ApplyUiState(FlowUiState uiState)
    {
        if (uiManager == null)
        {
            return;
        }

        if (uiState == FlowUiState.Gameplay)
        {
            uiManager.HideAllPanels();
            return;
        }

        if (uiState == FlowUiState.Start)
        {
            uiManager.ShowOnly<StartPanelUI>();
            return;
        }

        ResultPanelUI resultPanelUi = uiManager.ShowOnly<ResultPanelUI>();
        if (resultPanelUi != null)
        {
            resultPanelUi.SetContent("Run Complete", "本次流程已经结束。", "Restart");
        }
    }

    private static Transform FindPlayerTransform()
    {
        PlayerRuntimeContext runtimeContext = PlayerRuntimeContext.FindInScene();
        if (runtimeContext != null && runtimeContext.FormRoot != null)
        {
            return runtimeContext.FormRoot.transform;
        }

        PlayerFormRoot formRoot = FindObjectOfType<PlayerFormRoot>();
        return formRoot != null ? formRoot.transform : null;
    }

    private void HandleLevelChanged(int _)
    {
        levelStartX = GetPlayerX();
        RefreshLevelPresentation();
    }

    private void HandleRunLevelChanged(int _)
    {
        levelStartX = GetPlayerX();
        RefreshLevelPresentation();
    }

    private void ShowEndScreen()
    {
        Time.timeScale = 0f;
        isTransitioning = false;
        if (sessionController != null)
        {
            sessionController.CompleteRun();
        }
        ApplyUiState(FlowUiState.Completed);
    }

    private void HandleEndConfirmClicked()
    {
        if (sessionController != null)
        {
            sessionController.ResetRun();
        }

        ReloadActiveScene();
    }

    private void HandleRunStateChanged(GameRunState runState)
    {
        switch (runState)
        {
            case GameRunState.Running:
            case GameRunState.Transitioning:
                ApplyUiState(FlowUiState.Gameplay);
                break;

            case GameRunState.Completed:
                ApplyUiState(FlowUiState.Completed);
                break;

            default:
                ApplyUiState(FlowUiState.Start);
                break;
        }
    }
    private StartPanelUI GetStartPanelUi()
    {
        return uiManager != null ? uiManager.Get<StartPanelUI>() : null;
    }

    private ResultPanelUI GetResultPanelUi()
    {
        return uiManager != null ? uiManager.Get<ResultPanelUI>() : null;
    }

    private LevelProgressUI GetLevelProgressUi()
    {
        return uiManager != null ? uiManager.Get<LevelProgressUI>() : null;
    }

    private HintBarUI GetHintBarUi()
    {
        return uiManager != null ? uiManager.Get<HintBarUI>() : null;
    }
}
