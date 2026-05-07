using System.Collections;
using System.Collections.Generic;
using System.Text;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameFlowController : MonoBehaviour
{
    private enum FlowUiState
    {
        Start = 0,
        Tutorial = 1,
        Gameplay = 2,
        Completed = 3,
        Failed = 4
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

    [Header("Text")]
    [LabelText("开始按钮文本")]
    [SerializeField] private string startButtonText = "Start";
    [LabelText("重开按钮文本")]
    [SerializeField] private string restartButtonText = "Restart";
    [LabelText("退出按钮文本")]
    [SerializeField] private string exitButtonText = "Exit";
    [LabelText("关卡卡片显示时长")]
    [SerializeField] private float levelCardDuration = 2.25f;
    [LabelText("启用关卡提示卡")]
    [SerializeField] private bool enableLevelCards;
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
            TryShowLevelIntroCard();
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
        ApplyUiState(FlowUiState.Start);
    }

    private void ResumeGameplay()
    {
        Time.timeScale = 1f;
        isTransitioning = false;
        levelStartX = sessionController != null && sessionController.HasLevelStartPosition
            ? sessionController.LevelStartX
            : GetPlayerX();
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

        if (sessionController != null)
        {
            sessionController.ResetRespawnToLevelStart();
        }

        if (IsUsingManualLevelFlow())
        {
            levelController?.SetLevel(0);
            manualLevelSequenceController = manualLevelSequenceController != null
                ? manualLevelSequenceController
                : FindObjectOfType<ManualLevelSequenceController>();
            manualLevelSequenceController?.LoadLevel(1, true);
            sessionController?.UseSceneEntryAsCurrentLevelStart(1);
            RestorePlayerForCurrentManualLevel(false);
        }

        ResumeGameplay();
        TryShowLevelIntroCard();
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
            SoundEffectPlayback.Play(SoundEffectId.Success);
            ShowLevelClearCard(currentLevel, transitionDelay);
            yield return new WaitForSeconds(transitionDelay);
            if (sessionController != null)
            {
                sessionController.AdvanceLevel(levelCount);
            }

            if (IsUsingManualLevelFlow())
            {
                int nextLevelNumber = sessionController != null ? sessionController.CurrentLevelNumber : currentLevel + 1;
                levelController?.SetLevel(nextLevelNumber - 1);
                manualLevelSequenceController = manualLevelSequenceController != null
                    ? manualLevelSequenceController
                    : FindObjectOfType<ManualLevelSequenceController>();
                manualLevelSequenceController?.LoadLevel(nextLevelNumber, false);
                RestorePlayerForCurrentManualLevel(false);
                ResumeGameplay();
                TryShowLevelIntroCard();
                yield break;
            }

            ReloadActiveScene();
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

        levelProgressUi.SetLevelText($"Level {levelController.CurrentLevelNumber}");
    }

    private void RefreshProgressText()
    {
        LevelProgressUI levelProgressUi = GetLevelProgressUi();
        if (levelProgressUi == null || levelController == null)
        {
            return;
        }

        if (IsUsingManualLevelFlow())
        {
            levelProgressUi.SetProgressText(string.Empty);
            return;
        }

        float targetDistance = GetCurrentTargetDistance();
        float currentDistance = Mathf.Clamp(GetDistanceTravelled(), 0f, targetDistance);
        levelProgressUi.SetProgressText($"{currentDistance:0}/{targetDistance:0}m");
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

        ResultPanelUI resultPanelUi = uiManager.Get<ResultPanelUI>();
        if (resultPanelUi != null)
        {
            resultPanelUi.ConfirmRequested -= HandleEndConfirmClicked;
            resultPanelUi.ConfirmRequested += HandleEndConfirmClicked;
            resultPanelUi.SecondaryRequested -= HandleEndSecondaryClicked;
            resultPanelUi.SecondaryRequested += HandleEndSecondaryClicked;
        }
    }

    private void RefreshLevelPresentation()
    {
        RefreshHeader();
        RefreshProgressText();

        StartPanelUI startPanelUi = GetStartPanelUi();
        if (startPanelUi != null)
        {
            startPanelUi.SetContent("Start Run", BuildStartDescription(), startButtonText);
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

        if (uiState == FlowUiState.Tutorial)
        {
            uiManager.ShowOnly<TutorialPanelUI>();
            return;
        }

        ResultPanelUI resultPanelUi = uiManager.ShowOnly<ResultPanelUI>();
        if (resultPanelUi == null)
        {
            return;
        }

        if (uiState == FlowUiState.Completed)
        {
            resultPanelUi.SetContent("Run Complete", "This run has ended.", restartButtonText, exitButtonText);
            return;
        }

        if (uiState == FlowUiState.Failed)
        {
            resultPanelUi.SetContent("Run Failed", BuildFailureDescription(currentFailureType), restartButtonText, exitButtonText);
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

    private void HandleEndConfirmClicked()
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

        if (sessionController != null)
        {
            sessionController.StartNewRun();
        }

        if (IsUsingManualLevelFlow())
        {
            levelController?.SetLevel(0);
            manualLevelSequenceController = manualLevelSequenceController != null
                ? manualLevelSequenceController
                : FindObjectOfType<ManualLevelSequenceController>();
            manualLevelSequenceController?.LoadLevel(1, true);
            sessionController?.UseSceneEntryAsCurrentLevelStart(1);
            RestorePlayerForCurrentManualLevel(false);
            ResumeGameplay();
            TryShowLevelIntroCard();
            return;
        }

        ReloadActiveScene();
    }

    private void HandleEndSecondaryClicked()
    {
        if (sessionController != null)
        {
            sessionController.ResetRun();
        }

        if (IsUsingManualLevelFlow())
        {
            levelController?.SetLevel(0, false);
            manualLevelSequenceController = manualLevelSequenceController != null
                ? manualLevelSequenceController
                : FindObjectOfType<ManualLevelSequenceController>();
            manualLevelSequenceController?.LoadLevel(1, true);
            sessionController?.UseSceneEntryAsCurrentLevelStart(1);
            RestorePlayerForCurrentManualLevel(false);
            PauseForStartScreen();
            return;
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
            case GameRunState.Failed:
                break;

            default:
                ApplyUiState(FlowUiState.Start);
                break;
        }
    }

    private void HandleTutorialRequested()
    {
        if (sessionController != null && sessionController.HasActiveRun)
        {
            return;
        }

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

    private ResultPanelUI GetResultPanelUi()
    {
        return uiManager != null ? uiManager.Get<ResultPanelUI>() : null;
    }

    private TutorialPanelUI GetTutorialPanelUi()
    {
        return uiManager != null ? uiManager.Get<TutorialPanelUI>() : null;
    }

    private LevelProgressUI GetLevelProgressUi()
    {
        return uiManager != null ? uiManager.Get<LevelProgressUI>() : null;
    }

    private void TryShowLevelIntroCard()
    {
        if (levelController == null || sessionController == null)
        {
            return;
        }

        int levelNumber = levelController.CurrentLevelNumber;
        if (!sessionController.ShouldShowLevelIntro(levelNumber))
        {
            return;
        }

        ShowLevelCard(BuildLevelIntroTitle(levelNumber), BuildLevelIntroBody(levelNumber), levelCardDuration);
        sessionController.MarkLevelIntroShown(levelNumber);
    }

    private void ShowLevelClearCard(int clearedLevelNumber, float duration)
    {
        string title = $"Level {Mathf.Max(1, clearedLevelNumber)} Clear";
        string body = "Get ready for the next level.";
        ShowLevelCard(title, body, duration);
    }

    private void ShowLevelCard(string title, string body, float duration)
    {
        if (!enableLevelCards)
        {
            return;
        }

        LevelCardUI levelCardUi = GetLevelCardUi();
        if (levelCardUi == null)
        {
            return;
        }

        levelCardUi.ShowCard(title, body, duration);
    }

    private LevelCardUI GetLevelCardUi()
    {
        return uiManager != null ? uiManager.Get<LevelCardUI>() : null;
    }

    private string BuildLevelIntroTitle(int levelNumber)
    {
        return $"Level {Mathf.Max(1, levelNumber)}";
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

    private string BuildLevelIntroBody(int levelNumber)
    {
        ManualLevelFlowConfig.ManualLevelDefinition manualLevelConfig = GetManualLevelConfig(levelNumber);
        GameProgressionConfig.LevelDefinition levelConfig = GetLegacyLevelConfig(levelNumber);
        if (IsUsingManualLevelFlow())
        {
            if (manualLevelConfig == null)
            {
                return "Reach the goal trigger.";
            }

            StringBuilder manualBuilder = new StringBuilder();
            manualBuilder.Append("Reach the goal trigger.");
            manualBuilder.AppendLine();
            manualBuilder.Append($"Forms: {JoinFormNames(manualLevelConfig != null ? manualLevelConfig.UnlockedForms : null)}");
            return manualBuilder.ToString();
        }

        if (levelConfig == null)
        {
            return "Goal\n0m";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append($"Goal: {levelConfig.TargetDistance:0}m");
        builder.AppendLine();
        builder.Append($"Environments: {JoinEnvironmentNames(levelConfig.AllowedEnvironments)}");
        builder.AppendLine();
        builder.Append($"Forms: {JoinFormNames(levelConfig.UnlockedForms)}");
        builder.AppendLine();
        builder.Append($"Hazards: {JoinHazardNames(levelConfig.Hazards)}");
        return builder.ToString();
    }

    private ManualLevelFlowConfig.ManualLevelDefinition GetManualLevelConfig(int levelNumber)
    {
        ManualLevelFlowConfig manualConfig = ManualLevelFlowConfig.Load();
        return manualConfig != null ? manualConfig.GetLevel(levelNumber - 1) : null;
    }

    private GameProgressionConfig.LevelDefinition GetLegacyLevelConfig(int levelNumber)
    {
        GameProgressionConfig progressionConfig = GameProgressionConfig.Load();
        return progressionConfig != null ? progressionConfig.GetLevel(levelNumber - 1) : null;
    }

    private static string JoinEnvironmentNames(IReadOnlyList<EnvironmentType> environments)
    {
        if (environments == null || environments.Count == 0)
        {
            return "Mixed";
        }

        List<string> names = new List<string>();
        for (int i = 0; i < environments.Count; i++)
        {
            names.Add(GetEnvironmentName(environments[i]));
        }

        return string.Join(", ", names);
    }

    private static string JoinFormNames(IReadOnlyList<PlayerFormType> forms)
    {
        if (forms == null || forms.Count == 0)
        {
            return "All";
        }

        List<string> names = new List<string>();
        for (int i = 0; i < forms.Count; i++)
        {
            names.Add(GetFormName(forms[i]));
        }

        return string.Join(", ", names);
    }

    private static string JoinHazardNames(IReadOnlyList<HazardProfile> hazards)
    {
        if (hazards == null || hazards.Count == 0)
        {
            return "None";
        }

        List<string> names = new List<string>();
        for (int i = 0; i < hazards.Count; i++)
        {
            HazardProfile profile = hazards[i];
            if (profile == null)
            {
                continue;
            }

            names.Add(!string.IsNullOrWhiteSpace(profile.DisplayName)
                ? profile.DisplayName
                : GetHazardName(profile.HazardType));
        }

        return names.Count > 0 ? string.Join(", ", names) : "None";
    }

    private static string GetEnvironmentName(EnvironmentType environmentType)
    {
        switch (environmentType)
        {
            case EnvironmentType.Road:
                return "Road";
            case EnvironmentType.Water:
                return "Water";
            case EnvironmentType.Cliff:
                return "Cliff";
            case EnvironmentType.Blizzard:
                return "Blizzard";
            case EnvironmentType.Obstacle:
                return "Obstacle";
            default:
                return "Unknown";
        }
    }

    private static string GetFormName(PlayerFormType formType)
    {
        switch (formType)
        {
            case PlayerFormType.Human:
                return "Human";
            case PlayerFormType.Car:
                return "Car";
            case PlayerFormType.Plane:
                return "Plane";
            case PlayerFormType.Boat:
                return "Boat";
            default:
                return "Unknown";
        }
    }

    private static string GetHazardName(GameHazardType hazardType)
    {
        switch (hazardType)
        {
            case GameHazardType.BoulderChase:
                return "Boulder";
            case GameHazardType.FallingRocks:
                return "Falling Rocks";
            default:
                return "None";
        }
    }

}
