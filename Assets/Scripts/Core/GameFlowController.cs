using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [SerializeField] private GameLevelController levelController;
    [SerializeField] private GameSessionController sessionController;
    [SerializeField] private Transform player;
    [SerializeField] private PlayerHintUI hintUI;

    [Header("UI")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TMP_Text startTitleText;
    [SerializeField] private TMP_Text startDescriptionText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button endConfirmButton;
    [SerializeField] private TMP_Text startButtonLabelText;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private string startButtonText = "Start";
    private bool isTransitioning;
    private float levelStartX;

    public static GameFlowController Instance { get; private set; }

    public static GameFlowController GetOrCreateInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameFlowController existing = FindObjectOfType<GameFlowController>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject flowObject = new GameObject("GameFlowController");
        return flowObject.AddComponent<GameFlowController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        levelController = levelController != null ? levelController : GameLevelController.GetOrCreateInstance();
        sessionController = sessionController != null ? sessionController : GameSessionController.GetOrCreate();
        player = player != null ? player : FindPlayerTransform();
        hintUI = hintUI != null ? hintUI : FindObjectOfType<PlayerHintUI>(true);
        BindUi();
        RefreshStaticUiText();
    }

    private void Start()
    {
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
            levelController = GameLevelController.GetOrCreateInstance();
        }

        if (levelController != null)
        {
            levelController.LevelChanged += HandleLevelChanged;
        }

        if (sessionController == null)
        {
            sessionController = GameSessionController.GetOrCreate();
        }

        if (sessionController != null)
        {
            sessionController.RunStateChanged += HandleRunStateChanged;
            sessionController.RunLevelChanged += HandleRunLevelChanged;
        }
    }

    private void Update()
    {
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
        if (headerText == null || levelController == null)
        {
            return;
        }

        headerText.text = $"Level {levelController.CurrentLevelNumber} / {Mathf.Max(1, levelController.LevelCount)}";
    }

    private void RefreshProgressText()
    {
        if (progressText == null || levelController == null)
        {
            return;
        }

        float targetDistance = GetCurrentTargetDistance();
        float currentDistance = Mathf.Clamp(GetDistanceTravelled(), 0f, targetDistance);
        progressText.text = $"Goal {currentDistance:0}/{targetDistance:0}m";
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
        if (hintUI != null)
        {
            hintUI.ShowHint(message, duration);
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

    private void BindUi()
    {
        if (rootCanvas == null)
        {
            rootCanvas = FindObjectOfType<Canvas>();
        }

        if (rootCanvas == null)
        {
            return;
        }

        Transform canvasTransform = rootCanvas.transform;
        startPanel = startPanel != null ? startPanel : FindChildGameObject(canvasTransform, "GameStartPanel");
        endPanel = endPanel != null ? endPanel : FindChildGameObject(canvasTransform, "EndUI");
        headerText = headerText != null ? headerText : FindChildText(canvasTransform, "GameFlowHUD/LevelText");
        progressText = progressText != null ? progressText : FindChildText(canvasTransform, "GameFlowHUD/ProgressText");
        startTitleText = startTitleText != null ? startTitleText : FindChildText(canvasTransform, "GameStartPanel/Card/Title");
        startDescriptionText = startDescriptionText != null ? startDescriptionText : FindChildText(canvasTransform, "GameStartPanel/Card/Description");
        startButton = startButton != null ? startButton : FindChildButton(canvasTransform, "GameStartPanel/Card/StartButton");
        endConfirmButton = endConfirmButton != null ? endConfirmButton : FindChildButton(canvasTransform, "EndUI/Card/StartButton");
        startButtonLabelText = startButtonLabelText != null ? startButtonLabelText : FindChildText(canvasTransform, "GameStartPanel/Card/StartButton/Label");

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(BeginNewRun);
            startButton.onClick.AddListener(BeginNewRun);
        }

        if (endConfirmButton != null)
        {
            endConfirmButton.onClick.RemoveListener(HandleEndConfirmClicked);
            endConfirmButton.onClick.AddListener(HandleEndConfirmClicked);
        }
    }

    private void RefreshStaticUiText()
    {
        if (startButtonLabelText != null)
        {
            startButtonLabelText.text = startButtonText;
        }
    }

    private void RefreshLevelPresentation()
    {
        RefreshHeader();
        RefreshProgressText();

        if (startTitleText != null && levelController != null)
        {
            startTitleText.text = levelController.CurrentLevelName;
        }

        if (startDescriptionText != null)
        {
            startDescriptionText.text = BuildStartDescription();
        }
    }

    private void ApplyUiState(FlowUiState uiState)
    {
        SetActive(startPanel, uiState == FlowUiState.Start);
        SetActive(endPanel, uiState == FlowUiState.Completed);
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

    private static GameObject FindChildGameObject(Transform root, string path)
    {
        Transform target = root != null ? root.Find(path) : null;
        return target != null ? target.gameObject : null;
    }

    private static TMP_Text FindChildText(Transform root, string path)
    {
        Transform target = root != null ? root.Find(path) : null;
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static Button FindChildButton(Transform root, string path)
    {
        Transform target = root != null ? root.Find(path) : null;
        return target != null ? target.GetComponent<Button>() : null;
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

        if (endPanel != null)
        {
            endPanel.SetActive(false);
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

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
