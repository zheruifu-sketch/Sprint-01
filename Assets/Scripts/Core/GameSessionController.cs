using System;
using UnityEngine;

[DisallowMultipleComponent]
public class GameSessionController : MonoBehaviour
{
    [SerializeField] private GameRunState runState;
    [SerializeField] private int currentLevelNumber = 1;

    public static GameSessionController Instance { get; private set; }

    public bool HasActiveRun => runState != GameRunState.Idle;
    public bool IsGameplayRunning => runState == GameRunState.Running;
    public bool CanReceiveGameplayInput => runState == GameRunState.Running;
    public GameRunState RunState => runState;
    public int CurrentLevelNumber => currentLevelNumber < 1 ? 1 : currentLevelNumber;

    public event Action<GameRunState> RunStateChanged;
    public event Action<int> RunLevelChanged;

    public static GameSessionController GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameSessionController existing = FindObjectOfType<GameSessionController>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject controllerObject = new GameObject("GameSessionController");
        return controllerObject.AddComponent<GameSessionController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartNewRun()
    {
        SetCurrentLevelNumber(1);
        SetRunState(GameRunState.Running);
    }

    public void ResumeLevel(int levelNumber)
    {
        SetCurrentLevelNumber(levelNumber);
        SetRunState(GameRunState.Running);
    }

    public void BeginTransition()
    {
        if (runState == GameRunState.Idle)
        {
            return;
        }

        SetRunState(GameRunState.Transitioning);
    }

    public void AdvanceLevel(int maxLevelNumber)
    {
        SetCurrentLevelNumber(Mathf.Clamp(currentLevelNumber + 1, 1, Mathf.Max(1, maxLevelNumber)));
        SetRunState(GameRunState.Running);
    }

    public void CompleteRun()
    {
        if (runState == GameRunState.Idle)
        {
            return;
        }

        SetRunState(GameRunState.Completed);
    }

    public void ResetRun()
    {
        SetCurrentLevelNumber(1);
        SetRunState(GameRunState.Idle);
    }

    private void SetRunState(GameRunState nextState)
    {
        if (runState == nextState)
        {
            return;
        }

        runState = nextState;
        RunStateChanged?.Invoke(runState);
    }

    private void SetCurrentLevelNumber(int levelNumber)
    {
        int clampedLevelNumber = Mathf.Max(1, levelNumber);
        if (currentLevelNumber == clampedLevelNumber)
        {
            return;
        }

        currentLevelNumber = clampedLevelNumber;
        RunLevelChanged?.Invoke(currentLevelNumber);
    }
}
