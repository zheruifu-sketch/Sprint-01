using System;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class GameSessionController : MonoBehaviour
{
    public static GameSessionController Instance { get; private set; }

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
    [LabelText("是否已记录场景首关起点")]
    [SerializeField] private bool hasSceneEntryPosition;
    [LabelText("场景首关起点位置")]
    [SerializeField] private Vector3 sceneEntryPosition;
    [LabelText("是否已激活检查点")]
    [SerializeField] private bool hasActiveCheckpoint;
    [LabelText("当前检查点距离")]
    [SerializeField] private float activeCheckpointDistance;
    [LabelText("是否已记录检查点生命快照")]
    [SerializeField] private bool hasCheckpointHealthSnapshot;
    [LabelText("检查点生命快照")]
    [SerializeField] private float checkpointHealthSnapshot;
    [LabelText("是否已记录检查点燃料快照")]
    [SerializeField] private bool hasCheckpointFuelSnapshot;
    [LabelText("检查点燃料快照")]
    [SerializeField] private float checkpointFuelSnapshot;
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
    public bool HasSceneEntryPosition => hasSceneEntryPosition;
    public Vector3 SceneEntryPosition => sceneEntryPosition;
    public bool HasActiveCheckpoint => hasActiveCheckpoint;
    public float ActiveCheckpointDistance => activeCheckpointDistance;
    public bool HasCheckpointHealthSnapshot => hasCheckpointHealthSnapshot;
    public float CheckpointHealthSnapshot => checkpointHealthSnapshot;
    public bool HasCheckpointFuelSnapshot => hasCheckpointFuelSnapshot;
    public float CheckpointFuelSnapshot => checkpointFuelSnapshot;
    public int LastShownLevelIntroNumber => Mathf.Max(0, lastShownLevelIntroNumber);

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
        DontDestroyOnLoad(gameObject);
        currentLevelNumber = Mathf.Max(1, currentLevelNumber);
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

    public void RegisterSceneEntryPosition(Vector3 playerScenePosition)
    {
        hasSceneEntryPosition = true;
        sceneEntryPosition = playerScenePosition;
    }

    public void UseSceneEntryAsCurrentLevelStart(int levelNumber)
    {
        if (!hasSceneEntryPosition)
        {
            return;
        }

        SetCurrentLevelNumber(Mathf.Max(1, levelNumber));
        ClearCheckpointProgress();
        SetLevelStartPositionInternal(sceneEntryPosition, true);
        SetRespawnPositionInternal(sceneEntryPosition, true);
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

    public void ActivateCheckpoint(Vector3 checkpointPosition, float currentHealth, float currentFuel)
    {
        if (!hasLevelStartPosition)
        {
            SetLevelStartPositionInternal(checkpointPosition, true);
        }

        SetRespawnPositionInternal(checkpointPosition, true);
        hasActiveCheckpoint = true;
        activeCheckpointDistance = Mathf.Max(0f, checkpointPosition.x - levelStartPosition.x);
        hasCheckpointHealthSnapshot = true;
        checkpointHealthSnapshot = Mathf.Max(0f, currentHealth);
        hasCheckpointFuelSnapshot = true;
        checkpointFuelSnapshot = Mathf.Max(0f, currentFuel);
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
        hasCheckpointHealthSnapshot = false;
        checkpointHealthSnapshot = 0f;
        hasCheckpointFuelSnapshot = false;
        checkpointFuelSnapshot = 0f;
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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
