using UnityEngine;

[DisallowMultipleComponent]
public class GameSessionController : MonoBehaviour
{
    [SerializeField] private bool hasActiveRun;
    [SerializeField] private int currentLevelNumber = 1;

    public bool HasActiveRun => hasActiveRun;
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
        hasActiveRun = true;
        currentLevelNumber = 1;
    }

    public void ResumeLevel(int levelNumber)
    {
        hasActiveRun = true;
        currentLevelNumber = Mathf.Max(1, levelNumber);
    }

    public void AdvanceLevel(int maxLevelNumber)
    {
        hasActiveRun = true;
        currentLevelNumber = Mathf.Clamp(currentLevelNumber + 1, 1, Mathf.Max(1, maxLevelNumber));
    }

    public void ResetRun()
    {
        hasActiveRun = false;
        currentLevelNumber = 1;
    }
}
