using System;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class GameSessionController : MonoBehaviour
{
    [LabelText("当前跑局状态")]
    [SerializeField] private GameRunState runState;
    [LabelText("当前关卡序号")]
    [SerializeField] private int currentLevelNumber = 1;
    [LabelText("是否已记录关卡起点")]
    [SerializeField] private bool hasLevelStartPosition;
    [LabelText("当前关卡起点位置")]
    [SerializeField] private Vector3 levelStartPosition;
    [LabelText("是否已记录重生点")]
    [SerializeField] private bool hasRespawnPosition;
    [LabelText("当前重生点位置")]
    [SerializeField] private Vector3 respawnPosition;
    [LabelText("是否已激活检查点")]
    [SerializeField] private bool hasActiveCheckpoint;
    [LabelText("当前检查点距离")]
    [SerializeField] private float activeCheckpointDistance;
    [LabelText("已展示过开场卡片的关卡")]
    [SerializeField] private int lastShownLevelIntroNumber;

    public bool HasActiveRun => runState != GameRunState.Idle;
    public bool IsGameplayRunning => runState == GameRunState.Running;
    public bool CanReceiveGameplayInput => runState == GameRunState.Running;
    public GameRunState RunState => runState;
    public int CurrentLevelNumber => currentLevelNumber < 1 ? 1 : currentLevelNumber;
    public bool HasLevelStartPosition => hasLevelStartPosition;
    public Vector3 LevelStartPosition => levelStartPosition;
    public float LevelStartX => hasLevelStartPosition ? levelStartPosition.x : 0f;
    public bool HasRespawnPosition => hasRespawnPosition;
    public Vector3 RespawnPosition => respawnPosition;
    public bool HasActiveCheckpoint => hasActiveCheckpoint;
    public float ActiveCheckpointDistance => activeCheckpointDistance;
    public int LastShownLevelIntroNumber => Mathf.Max(0, lastShownLevelIntroNumber);

    public event Action<GameRunState> RunStateChanged;
    public event Action<int> RunLevelChanged;

    private void Awake()
    {
        GameSessionController previousSession = FindPersistentSessionInstance();
        if (previousSession != null && previousSession != this)
        {
            CopyRuntimeStateFrom(previousSession);
            Destroy(previousSession.gameObject);
        }

        DontDestroyOnLoad(gameObject);
        currentLevelNumber = Mathf.Max(1, currentLevelNumber);
    }

    private GameSessionController FindPersistentSessionInstance()
    {
        GameSessionController[] sessionControllers = FindObjectsOfType<GameSessionController>();
        for (int i = 0; i < sessionControllers.Length; i++)
        {
            GameSessionController other = sessionControllers[i];
            if (other == null || other == this)
            {
                continue;
            }

            if (other.gameObject.scene.name == "DontDestroyOnLoad")
            {
                return other;
            }
        }

        return null;
    }

    private void CopyRuntimeStateFrom(GameSessionController source)
    {
        if (source == null)
        {
            return;
        }

        runState = source.runState;
        currentLevelNumber = source.currentLevelNumber;
        hasLevelStartPosition = source.hasLevelStartPosition;
        levelStartPosition = source.levelStartPosition;
        hasRespawnPosition = source.hasRespawnPosition;
        respawnPosition = source.respawnPosition;
        hasActiveCheckpoint = source.hasActiveCheckpoint;
        activeCheckpointDistance = source.activeCheckpointDistance;
        lastShownLevelIntroNumber = source.lastShownLevelIntroNumber;
    }

    public void StartNewRun()
    {
        SetCurrentLevelNumber(1);
        ClearCheckpointProgress();
        lastShownLevelIntroNumber = 0;
        SetRunState(GameRunState.Running);
    }

    public void ResumeLevel(int levelNumber)
    {
        bool levelChanged = CurrentLevelNumber != Mathf.Max(1, levelNumber);
        SetCurrentLevelNumber(levelNumber);
        if (levelChanged)
        {
            ClearCheckpointProgress();
        }

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
        ClearCheckpointProgress();
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
        ClearCheckpointProgress();
        lastShownLevelIntroNumber = 0;
        SetLevelStartPositionInternal(Vector3.zero, false);
        SetRespawnPositionInternal(Vector3.zero, false);
        SetRunState(GameRunState.Idle);
    }

    public void DebugEnterLevel(int levelNumber)
    {
        int resolvedLevelNumber = Mathf.Max(1, levelNumber);
        SetCurrentLevelNumber(resolvedLevelNumber);
        ClearCheckpointProgress();
        lastShownLevelIntroNumber = resolvedLevelNumber - 1;
        SetLevelStartPositionInternal(Vector3.zero, false);
        SetRespawnPositionInternal(Vector3.zero, false);
        SetRunState(GameRunState.Running);
    }

    public bool ShouldShowLevelIntro(int levelNumber)
    {
        int resolvedLevelNumber = Mathf.Max(1, levelNumber);
        return lastShownLevelIntroNumber < resolvedLevelNumber;
    }

    public void MarkLevelIntroShown(int levelNumber)
    {
        lastShownLevelIntroNumber = Mathf.Max(lastShownLevelIntroNumber, Mathf.Max(1, levelNumber));
    }

    public void PrepareLevelSpawn(int levelNumber, Vector3 sceneLevelStartPosition)
    {
        int resolvedLevelNumber = Mathf.Max(1, levelNumber);
        bool levelChanged = CurrentLevelNumber != resolvedLevelNumber;

        if (levelChanged)
        {
            SetCurrentLevelNumber(resolvedLevelNumber);
            ClearCheckpointProgress();
        }

        if (!hasLevelStartPosition || levelChanged)
        {
            SetLevelStartPositionInternal(sceneLevelStartPosition, true);
        }

        if (!hasRespawnPosition || levelChanged || !hasActiveCheckpoint)
        {
            SetRespawnPositionInternal(sceneLevelStartPosition, true);
        }
    }

    public Vector3 GetRespawnPositionOrDefault(Vector3 fallbackPosition)
    {
        return hasRespawnPosition ? respawnPosition : fallbackPosition;
    }

    public float GetTravelDistanceFromWorldX(float worldX)
    {
        if (!hasLevelStartPosition)
        {
            return Mathf.Max(0f, worldX);
        }

        return Mathf.Max(0f, worldX - levelStartPosition.x);
    }

    public void ActivateCheckpoint(Vector3 checkpointPosition)
    {
        if (!hasLevelStartPosition)
        {
            SetLevelStartPositionInternal(checkpointPosition, true);
        }

        SetRespawnPositionInternal(checkpointPosition, true);
        hasActiveCheckpoint = true;
        activeCheckpointDistance = Mathf.Max(0f, checkpointPosition.x - levelStartPosition.x);
    }

    public void ResetRespawnToLevelStart()
    {
        if (!hasLevelStartPosition)
        {
            return;
        }

        ClearCheckpointProgress();
        SetRespawnPositionInternal(levelStartPosition, true);
    }

    private void ClearCheckpointProgress()
    {
        hasActiveCheckpoint = false;
        activeCheckpointDistance = 0f;
    }

    private void SetLevelStartPositionInternal(Vector3 position, bool hasValue)
    {
        hasLevelStartPosition = hasValue;
        levelStartPosition = position;
    }

    private void SetRespawnPositionInternal(Vector3 position, bool hasValue)
    {
        hasRespawnPosition = hasValue;
        respawnPosition = position;
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
