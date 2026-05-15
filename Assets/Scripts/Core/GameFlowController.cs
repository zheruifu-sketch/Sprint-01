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
        LevelSelect = 1,
        Tutorial = 2,
        Gameplay = 3,
        Completed = 4,
        Failed = 5
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
    [LabelText("手工关卡序列控制器")]
    [SerializeField] private ManualLevelSequenceController manualLevelSequenceController;

    private bool isTransitioning;
    private float levelStartX;
    private FailureType currentFailureType;
    private FlowUiState currentUiState;

    private void Awake()
    {
        levelController = levelController != null ? levelController : FindObjectOfType<GameLevelController>();
        sessionController = sessionController != null ? sessionController : FindObjectOfType<GameSessionController>();
        player = player != null ? player : FindPlayerTransform();
        uiManager = uiManager != null ? uiManager : FindObjectOfType<UIManager>(true);
        manualLevelSequenceController = manualLevelSequenceController != null
            ? manualLevelSequenceController
            : FindObjectOfType<ManualLevelSequenceController>();
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

        if (IsUsingManualLevelFlow())
        {
            return;
        }

        if (GetDistanceTravelled() < GetCurrentTargetDistance())
        {
            return;
        }

        TryCompleteCurrentLevel();
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

        TutorialPanelUI tutorialPanelUi = GetTutorialPanelUi();
        if (tutorialPanelUi != null && tutorialPanelUi.IsVisible)
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
               || Input.GetKeyDown(KeyCode.Alpha1)
               || Input.GetKeyDown(KeyCode.Alpha2)
               || Input.GetKeyDown(KeyCode.Alpha3)
               || Input.GetKeyDown(KeyCode.Alpha4)
               || Input.GetKeyDown(KeyCode.Keypad1)
               || Input.GetKeyDown(KeyCode.Keypad2)
               || Input.GetKeyDown(KeyCode.Keypad3)
               || Input.GetKeyDown(KeyCode.Keypad4);
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

    private void PauseForStartScreen()
    {
        Time.timeScale = 0f;
        isTransitioning = false;
        BackgroundMusicPlayback.Stop();
        ApplyUiState(FlowUiState.Start);
    }

    private void ResumeGameplay()
    {
        StartCoroutine(ResumeGameplayRoutine());
    }

    private IEnumerator ResumeGameplayRoutine()
    {
        Time.timeScale = 1f;
        isTransitioning = false;
        BackgroundMusicPlayback.PlayLoop();
        levelStartX = sessionController != null && sessionController.HasLevelStartPosition
            ? sessionController.LevelStartX
            : GetPlayerX();
        ApplyUiState(FlowUiState.Gameplay);

        PlayerRuntimeContext runtimeContext = PlayerRuntimeContext.FindInScene();
        if (runtimeContext != null && runtimeContext.RespawnController != null)
        {
            yield return StartCoroutine(runtimeContext.RespawnController.PlayRespawnCountdownRoutine());
        }
    }

    private void BeginNewRun()
    {
        if (sessionController != null && sessionController.HasActiveRun)
        {
            return;
        }

        Time.timeScale = 0f;
        isTransitioning = false;
        ApplyUiState(FlowUiState.LevelSelect);
    }

    private void BeginNewRunFromLevelSelection(int levelNumber)
    {
        if (sessionController != null)
        {
            sessionController.StartNewRun();
        }

        if (levelController != null)
        {
            levelController.SetLevel(Mathf.Max(0, levelNumber - 1));
        }

        if (sessionController != null)
        {
            sessionController.ResetRespawnToLevelStart();
        }

        if (IsUsingManualLevelFlow())
        {
            levelController?.SetLevel(Mathf.Max(0, levelNumber - 1));
            manualLevelSequenceController = manualLevelSequenceController != null
                ? manualLevelSequenceController
                : FindObjectOfType<ManualLevelSequenceController>();
            manualLevelSequenceController?.LoadLevel(levelNumber, false);
            RestorePlayerForCurrentManualLevel(false);
        }

        ResumeGameplay();
    }

    private IEnumerator HandleLevelCompleted()
    {
        isTransitioning = true;
        Time.timeScale = 1f;
        if (sessionController != null)
        {
            sessionController.StopLevelTimer();
            sessionController.BeginTransition();
        }

        int currentLevel = levelController != null ? levelController.CurrentLevelNumber : 1;
        int levelCount = levelController != null ? Mathf.Max(1, levelController.LevelCount) : 3;
        float transitionDelay = levelController != null ? levelController.GetTransitionDelay() : 1.25f;

        if (currentLevel < levelCount)
        {
            SoundEffectPlayback.Play(SoundEffectId.Success);
            ShowEndScreen();
            yield break;
        }

        SoundEffectPlayback.Play(SoundEffectId.Success);
        ShowEndScreen();
    }

    private void RefreshHeader()
    {
        LevelProgressUI levelProgressUi = GetLevelProgressUi();
        if (levelProgressUi == null || levelController == null)
        {
            return;
        }

        levelProgressUi.SetLevelText(levelController.CurrentLevelName);
    }

    private void RefreshProgressText()
    {
        // Level progress distance text is no longer used in the current UI flow.
    }

    private float GetCurrentTargetDistance()
    {
        return levelController != null ? levelController.GetCurrentTargetDistance() : 60f;
    }

    private float GetDistanceTravelled()
    {
        if (sessionController != null)
        {
            return sessionController.GetTravelDistanceFromWorldX(GetPlayerX());
        }

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

    private string BuildStartDescription()
    {
        return "Switch forms, manage health and fuel, and clear every level in one run.";
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
            startPanelUi.HelpRequested -= HandleTutorialRequested;
            startPanelUi.HelpRequested += HandleTutorialRequested;
        }

        TutorialPanelUI tutorialPanelUi = uiManager.Get<TutorialPanelUI>();
        if (tutorialPanelUi != null)
        {
            tutorialPanelUi.CloseRequested -= HandleTutorialClosed;
            tutorialPanelUi.CloseRequested += HandleTutorialClosed;
        }

        FixedActionButtonsUI fixedActionButtonsUi = uiManager.Get<FixedActionButtonsUI>();
        if (fixedActionButtonsUi != null)
        {
            fixedActionButtonsUi.HelpRequested -= HandleTutorialRequested;
            fixedActionButtonsUi.HelpRequested += HandleTutorialRequested;
            fixedActionButtonsUi.ResetLevelRequested -= HandleResetCurrentLevelRequested;
            fixedActionButtonsUi.ResetLevelRequested += HandleResetCurrentLevelRequested;
        }

        FailResultPanelUI failResultPanelUi = uiManager.Get<FailResultPanelUI>();
        if (failResultPanelUi != null)
        {
            failResultPanelUi.ConfirmRequested -= HandleFailConfirmClicked;
            failResultPanelUi.ConfirmRequested += HandleFailConfirmClicked;
            failResultPanelUi.SecondaryRequested -= HandleReturnHomeClicked;
            failResultPanelUi.SecondaryRequested += HandleReturnHomeClicked;
        }

        LevelSelectPanelUI levelSelectPanelUi = uiManager.Get<LevelSelectPanelUI>();
        if (levelSelectPanelUi != null)
        {
            levelSelectPanelUi.LevelSelected -= BeginNewRunFromLevelSelection;
            levelSelectPanelUi.LevelSelected += BeginNewRunFromLevelSelection;
            levelSelectPanelUi.BackRequested -= HandleLevelSelectBackRequested;
            levelSelectPanelUi.BackRequested += HandleLevelSelectBackRequested;
        }

        WinResultPanelUI winResultPanelUi = uiManager.Get<WinResultPanelUI>();
        if (winResultPanelUi != null)
        {
            winResultPanelUi.NextLevelRequested -= HandleNextLevelRequested;
            winResultPanelUi.NextLevelRequested += HandleNextLevelRequested;
            winResultPanelUi.HomeRequested -= HandleReturnHomeClicked;
            winResultPanelUi.HomeRequested += HandleReturnHomeClicked;
        }
    }

    private void RefreshLevelPresentation()
    {
        RefreshHeader();
        RefreshProgressText();

        StartPanelUI startPanelUi = GetStartPanelUi();
        if (startPanelUi != null)
        {
            startPanelUi.SetContent("Start Run", BuildStartDescription());
        }

        LevelSelectPanelUI levelSelectPanelUi = GetLevelSelectPanelUi();
        if (levelSelectPanelUi != null)
        {
            levelSelectPanelUi.SetContent(levelController != null ? levelController.LevelCount : 0);
        }
    }

    private void ApplyUiState(FlowUiState uiState)
    {
        if (uiManager == null)
        {
            return;
        }

        currentUiState = uiState;

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

        if (uiState == FlowUiState.LevelSelect)
        {
            uiManager.ShowOnly<LevelSelectPanelUI>();
            return;
        }

        if (uiState == FlowUiState.Tutorial)
        {
            uiManager.ShowOnly<TutorialPanelUI>();
            return;
        }

        if (uiState == FlowUiState.Completed)
        {
            WinResultPanelUI winResultPanelUi = uiManager.ShowOnly<WinResultPanelUI>();
            if (winResultPanelUi == null)
            {
                return;
            }

            bool canGoToNextLevel = levelController != null && levelController.CurrentLevelNumber < levelController.LevelCount;
            string stats = BuildWinResultStats();
            string levelTitle = !string.IsNullOrWhiteSpace(levelController?.CurrentLevelName)
                ? levelController.CurrentLevelName
                : $"Level {levelController?.CurrentLevelNumber ?? 1}";
            winResultPanelUi.SetContent(
                $"{levelTitle} Clear",
                stats,
                canGoToNextLevel);
            return;
        }

        if (uiState == FlowUiState.Failed)
        {
            FailResultPanelUI failResultPanelUi = uiManager.ShowOnly<FailResultPanelUI>();
            if (failResultPanelUi != null)
            {
                failResultPanelUi.SetContent("Run Failed", BuildFailureDescription(currentFailureType), "Back to Home");
            }
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
        levelStartX = sessionController != null && sessionController.HasLevelStartPosition
            ? sessionController.LevelStartX
            : GetPlayerX();
        RefreshLevelPresentation();
    }

    private void HandleRunLevelChanged(int _)
    {
        levelStartX = sessionController != null && sessionController.HasLevelStartPosition
            ? sessionController.LevelStartX
            : GetPlayerX();
        RefreshLevelPresentation();
    }

    private void ShowEndScreen()
    {
        BackgroundMusicPlayback.Stop();
        Time.timeScale = 0f;
        isTransitioning = false;
        if (sessionController != null)
        {
            sessionController.CompleteRun();
        }
        ApplyUiState(FlowUiState.Completed);
    }

    public void HandleRunFailed(FailureType failureType)
    {
        currentFailureType = failureType;
        SoundEffectPlayback.Play(SoundEffectId.Failure);
        BackgroundMusicPlayback.Stop();
        Time.timeScale = 0f;
        isTransitioning = false;
        if (sessionController != null)
        {
            sessionController.FailRun();
        }
        ApplyUiState(FlowUiState.Failed);
    }

    public void CompleteCurrentLevelFromTrigger()
    {
        TryCompleteCurrentLevel();
    }

    private void HandleFailConfirmClicked()
    {
        if (sessionController != null && sessionController.RunState == GameRunState.Failed)
        {
            if (currentFailureType == FailureType.FuelDepleted || currentFailureType == FailureType.HealthDepleted)
            {
                // Resource-depletion deaths should reset the player back to the
                // level start instead of preserving checkpoint progress, otherwise
                // the player can get soft-locked into a bad health/fuel state.
                sessionController.ResetRespawnToLevelStart();
            }

            if (IsUsingManualLevelFlow())
            {
                sessionController.ResumeLevel(sessionController.CurrentLevelNumber);
                RestorePlayerForCurrentManualLevel(true);
                ResumeGameplay();
                return;
            }

            sessionController.ResumeLevel(sessionController.CurrentLevelNumber);
            ReloadActiveScene();
            return;
        }

    }

    private void HandleNextLevelRequested()
    {
        if (levelController == null)
        {
            return;
        }

        int currentLevelNumber = levelController.CurrentLevelNumber;
        int nextLevelNumber = currentLevelNumber + 1;
        if (nextLevelNumber > levelController.LevelCount)
        {
            return;
        }

        Time.timeScale = 1f;
        isTransitioning = false;

        if (sessionController != null)
        {
            sessionController.AdvanceLevel(levelController.LevelCount);
        }

        if (IsUsingManualLevelFlow())
        {
            levelController.SetLevel(nextLevelNumber - 1);
            manualLevelSequenceController = manualLevelSequenceController != null
                ? manualLevelSequenceController
                : FindObjectOfType<ManualLevelSequenceController>();
            manualLevelSequenceController?.LoadLevel(nextLevelNumber, false);
            RestorePlayerForCurrentManualLevel(false);
            ResumeGameplay();
            return;
        }

        levelController.SetLevel(nextLevelNumber - 1);
        ReloadActiveScene();
    }

    private void HandleReturnHomeClicked()
    {
        if (sessionController != null)
        {
            sessionController.ResetRun();
        }

        ReloadActiveScene();
    }

    private void HandleLevelSelectBackRequested()
    {
        PauseForStartScreen();
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
            case GameRunState.Failed:
                break;

            default:
                ApplyUiState(FlowUiState.Start);
                break;
        }
    }

    private void HandleTutorialRequested()
    {
        Time.timeScale = 0f;
        isTransitioning = false;
        ApplyUiState(FlowUiState.Tutorial);
    }

    private void HandleTutorialClosed()
    {
        if (sessionController != null && sessionController.HasActiveRun)
        {
            ResumeGameplay();
            return;
        }

        PauseForStartScreen();
    }

    private void HandleResetCurrentLevelRequested()
    {
        if (sessionController == null || !sessionController.HasActiveRun)
        {
            return;
        }

        Time.timeScale = 1f;
        isTransitioning = false;
        sessionController.ResetRespawnToLevelStart();
        sessionController.ResumeLevel(sessionController.CurrentLevelNumber);

        if (IsUsingManualLevelFlow())
        {
            RestorePlayerForCurrentManualLevel(true);
            ResumeGameplay();
            return;
        }

        ReloadActiveScene();
    }

    private string BuildFailureDescription(FailureType failureType)
    {
        GameMessageCatalog messageCatalog = GameMessageCatalog.Load();
        string fallbackMessage = "Run failed.";
        return messageCatalog != null
            ? messageCatalog.GetFailureMessage(failureType, fallbackMessage)
            : fallbackMessage;
    }

    private StartPanelUI GetStartPanelUi()
    {
        return uiManager != null ? uiManager.Get<StartPanelUI>() : null;
    }

    private FailResultPanelUI GetResultPanelUi()
    {
        return uiManager != null ? uiManager.Get<FailResultPanelUI>() : null;
    }

    private LevelSelectPanelUI GetLevelSelectPanelUi()
    {
        return uiManager != null ? uiManager.Get<LevelSelectPanelUI>() : null;
    }

    private TutorialPanelUI GetTutorialPanelUi()
    {
        return uiManager != null ? uiManager.Get<TutorialPanelUI>() : null;
    }

    private LevelProgressUI GetLevelProgressUi()
    {
        return uiManager != null ? uiManager.Get<LevelProgressUI>() : null;
    }

    private string BuildWinResultStats()
    {
        if (sessionController == null)
        {
            return "Time: 00:00.00\nPickup Score: 0\nHealth Bonus: 50\nFuel Bonus: 50\nBonus Score: 100\nTotal Score: 100";
        }

        PlayerRuntimeContext runtimeContext = PlayerRuntimeContext.FindInScene();
        float healthValue = runtimeContext != null && runtimeContext.HealthController != null ? runtimeContext.HealthController.CurrentHealth : 0f;
        float fuelValue = runtimeContext != null && runtimeContext.FuelController != null ? runtimeContext.FuelController.CurrentFuel : 0f;
        float maxHealth = runtimeContext != null && runtimeContext.HealthController != null ? runtimeContext.HealthController.MaxHealth : 100f;
        float maxFuel = runtimeContext != null && runtimeContext.FuelController != null ? runtimeContext.FuelController.MaxFuel : 100f;

        int pickupScore = sessionController.PickupScore;
        int healthBonus = GetSurvivalBonus(healthValue, maxHealth);
        int fuelBonus = GetSurvivalBonus(fuelValue, maxFuel);
        int bonusScore = healthBonus + fuelBonus;
        int totalScore = pickupScore + bonusScore;

        return $"Time: {FormatElapsedTime(sessionController.LevelElapsedTime)}\nPickup Score: {pickupScore}\nHealth Bonus: {healthBonus}\nFuel Bonus: {fuelBonus}\nTotal Score: {totalScore}";
    }

    private static int GetSurvivalBonus(float value, float maxValue)
    {
        return value > 50f || value > maxValue * 0.5f ? 100 : 50;
    }

    private static string FormatElapsedTime(float elapsedTime)
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        float seconds = elapsedTime - minutes * 60f;
        return $"{minutes:00}:{seconds:00.000}";
    }

    private void TryCompleteCurrentLevel()
    {
        if (isTransitioning || sessionController == null || !sessionController.IsGameplayRunning)
        {
            return;
        }

        StartCoroutine(HandleLevelCompleted());
    }

    private bool IsUsingManualLevelFlow()
    {
        if (manualLevelSequenceController == null)
        {
            manualLevelSequenceController = FindObjectOfType<ManualLevelSequenceController>();
        }

        return manualLevelSequenceController != null && manualLevelSequenceController.IsManualLevelModeEnabled;
    }

    private void RestorePlayerForCurrentManualLevel(bool rebuildLevelInstance)
    {
        if (rebuildLevelInstance)
        {
            if (manualLevelSequenceController == null)
            {
                manualLevelSequenceController = FindObjectOfType<ManualLevelSequenceController>();
            }

            // Manual prefab levels can contain one-shot runtime state such as sinking
            // platforms. Rebuild the current instantiated level before restoring the
            // player so each respawn starts from a clean authored layout.
            manualLevelSequenceController?.ReloadCurrentLevelForRespawn();
        }

        PlayerRuntimeContext runtimeContext = PlayerRuntimeContext.FindInScene();
        if (runtimeContext == null || runtimeContext.RespawnController == null)
        {
            return;
        }

        runtimeContext.RespawnController.RestorePlayerAtCurrentRespawn();
        player = runtimeContext.FormRoot != null ? runtimeContext.FormRoot.transform : player;
        levelStartX = sessionController != null && sessionController.HasLevelStartPosition
            ? sessionController.LevelStartX
            : GetPlayerX();
    }


}
