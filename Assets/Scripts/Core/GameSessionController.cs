using System;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class GameSessionController : MonoBehaviour
{
    private static GameRunState savedRunState = GameRunState.Idle;
    private static int savedCurrentLevelNumber = 1;

    [LabelText("当前跑局状态")]
    [SerializeField] private GameRunState runState;
    [LabelText("当前关卡序号")]
    [SerializeField] private int currentLevelNumber = 1;

    public static GameSessionController Instance { get; private set; }

    public bool HasActiveRun => runState != GameRunState.Idle;
    public bool IsGameplayRunning => runState == GameRunState.Running;
    public bool CanReceiveGameplayInput => runState == GameRunState.Running;
    public GameRunState RunState => runState;
    public int CurrentLevelNumber => currentLevelNumber < 1 ? 1 : currentLevelNumber;

    public event Action<GameRunState> RunStateChanged;
    public event Action<int> RunLevelChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        runState = savedRunState;
        currentLevelNumber = Mathf.Max(1, savedCurrentLevelNumber);
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

    public void FailRun()
    {
        if (runState == GameRunState.Idle)
        {
            return;
        }

        SetRunState(GameRunState.Failed);
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
        savedRunState = runState;
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
        savedCurrentLevelNumber = currentLevelNumber;
        RunLevelChanged?.Invoke(currentLevelNumber);
    }
}
