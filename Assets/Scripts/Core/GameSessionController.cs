using UnityEngine;

[DisallowMultipleComponent]
public class GameSessionController : MonoBehaviour
{
    [SerializeField] private GameRunState runState;
    [SerializeField] private int currentLevelNumber = 1;

    public bool HasActiveRun => runState != GameRunState.Idle;
    public bool IsGameplayRunning => runState == GameRunState.Running;
    public GameRunState RunState => runState;
    public int CurrentLevelNumber => currentLevelNumber < 1 ? 1 : currentLevelNumber;

    public static GameSessionController GetOrCreate()
    {
        GameSessionController existing = FindObjectOfType<GameSessionController>();
        if (existing != null)
        {
            return existing;
        }

        GameObject controllerObject = new GameObject("GameSessionController");
        return controllerObject.AddComponent<GameSessionController>();
    }

    private void Awake()
    {
        GameSessionController[] controllers = FindObjectsOfType<GameSessionController>();
        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] == this)
            {
                continue;
            }

            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    public void StartNewRun()
    {
        runState = GameRunState.Running;
        currentLevelNumber = 1;
    }

    public void ResumeLevel(int levelNumber)
    {
        runState = GameRunState.Running;
        currentLevelNumber = Mathf.Max(1, levelNumber);
    }

    public void BeginTransition()
    {
        if (runState == GameRunState.Idle)
        {
            return;
        }

        runState = GameRunState.Transitioning;
    }

    public void AdvanceLevel(int maxLevelNumber)
    {
        runState = GameRunState.Running;
        currentLevelNumber = Mathf.Clamp(currentLevelNumber + 1, 1, Mathf.Max(1, maxLevelNumber));
    }

    public void CompleteRun()
    {
        if (runState == GameRunState.Idle)
        {
            return;
        }

        runState = GameRunState.Completed;
    }

    public void ResetRun()
    {
        runState = GameRunState.Idle;
        currentLevelNumber = 1;
    }
}
