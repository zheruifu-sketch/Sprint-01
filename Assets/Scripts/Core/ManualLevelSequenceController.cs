using System.Collections.Generic;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DefaultExecutionOrder(-950)]
[DisallowMultipleComponent]
public class ManualLevelSequenceController : MonoBehaviour
{
    [Header("Mode")]
    [LabelText("启用手工关卡模式")]
    [SerializeField] private bool enableManualLevelMode = true;

    [Header("References")]
    [LabelText("关卡控制器")]
    [SerializeField] private GameLevelController levelController;
    [LabelText("跑局控制器")]
    [SerializeField] private GameSessionController sessionController;
    [LabelText("关卡父节点")]
    [SerializeField] private Transform levelParent;

    [Header("Manual Levels")]
    [LabelText("手工关卡预制体列表")]
    [SerializeField] private List<GameObject> levelPrefabs = new List<GameObject>();
    [LabelText("手工关卡名称列表")]
    [SerializeField] private List<string> levelNames = new List<string>();

    private GameObject currentLevelInstance;

    public static ManualLevelSequenceController Instance { get; private set; }
    public static bool IsManualModeActive => Instance != null && Instance.IsManualLevelModeEnabled;

    public bool IsManualLevelModeEnabled => enableManualLevelMode && GetConfiguredLevelCount() > 0;
    public int LevelCount => GetConfiguredLevelCount();

    private void Reset()
    {
        levelController = FindObjectOfType<GameLevelController>();
        sessionController = FindObjectOfType<GameSessionController>();
        levelParent = transform;
    }

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
        levelParent = levelParent != null ? levelParent : transform;
    }

    private void Start()
    {
        RefreshLoadedLevel();
    }

    public void RefreshLoadedLevel()
    {
        LoadLevel(ResolveCurrentLevelNumber(), true);
    }

    public void ReloadCurrentLevelForRespawn()
    {
        if (!IsManualLevelModeEnabled)
        {
            return;
        }

        // Only rebuild runtime-instantiated level prefabs on respawn.
        // Direct scene-authored test content should stay in place so editing playtests
        // do not destroy the user's in-scene setup every time the player dies.
        if (currentLevelInstance == null)
        {
            return;
        }

        LoadLevel(ResolveCurrentLevelNumber(), false);
    }

    public void LoadLevel(int levelNumber, bool allowSceneTestContent)
    {
        GameObject previousRuntimeLevelInstance = currentLevelInstance;
        ClearCurrentLevel(previousRuntimeLevelInstance);
        if (!IsManualLevelModeEnabled)
        {
            return;
        }

        Transform existingLevelRoot = FindExistingManualLevelContent(levelNumber, allowSceneTestContent, previousRuntimeLevelInstance);
        if (existingLevelRoot != null)
        {
            // When the level parent already has authored content, assume the user is
            // editing or testing the matching level in-place and skip prefab instantiation.
            Vector3 existingLevelStartPosition = ResolveLevelStartPosition(existingLevelRoot);
            if (sessionController != null)
            {
                sessionController.PrepareLevelSpawn(levelNumber, existingLevelStartPosition);
            }

            return;
        }

        GameObject levelPrefab = GetLevelPrefab(levelNumber);
        if (levelPrefab == null)
        {
            return;
        }

        currentLevelInstance = Instantiate(levelPrefab, levelParent);
        currentLevelInstance.name = levelPrefab.name;

        Vector3 levelStartPosition = ResolveLevelStartPosition(currentLevelInstance.transform);
        if (sessionController != null)
        {
            sessionController.PrepareLevelSpawn(levelNumber, levelStartPosition);
        }
    }

    public GameObject GetLevelPrefab(int levelNumber)
    {
        int resolvedIndex = ResolveConfiguredLevelIndex(levelNumber);
        if (resolvedIndex < 0)
        {
            return null;
        }

        return levelPrefabs[resolvedIndex];
    }

    public string GetLevelName(int levelNumber)
    {
        int resolvedIndex = ResolveConfiguredLevelIndex(levelNumber);
        if (resolvedIndex < 0 || resolvedIndex >= levelNames.Count)
        {
            return string.Empty;
        }

        return levelNames[resolvedIndex] != null ? levelNames[resolvedIndex].Trim() : string.Empty;
    }

    private Transform FindExistingManualLevelContent(int targetLevelNumber, bool allowSceneTestContent, GameObject ignoredRuntimeLevelInstance)
    {
        if (levelParent == null)
        {
            return null;
        }

        for (int i = 0; i < levelParent.childCount; i++)
        {
            Transform child = levelParent.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (ignoredRuntimeLevelInstance != null && child.gameObject == ignoredRuntimeLevelInstance)
            {
                continue;
            }

            ManualLevelRoot levelRoot = child.GetComponent<ManualLevelRoot>();
            if (levelRoot == null)
            {
                levelRoot = child.GetComponentInChildren<ManualLevelRoot>(true);
            }

            if (levelRoot != null)
            {
                bool isMatch = levelRoot.SceneTestLevelNumber == Mathf.Max(1, targetLevelNumber);
                child.gameObject.SetActive(allowSceneTestContent && isMatch);
                if (allowSceneTestContent && isMatch)
                {
                    return child;
                }

                continue;
            }

            // If the scene has raw authored content without a ManualLevelRoot marker,
            // only treat it as a direct-play test level for Level 1.
            bool useRawSceneContent = allowSceneTestContent && Mathf.Max(1, targetLevelNumber) == 1;
            child.gameObject.SetActive(useRawSceneContent);
            if (useRawSceneContent)
            {
                return child;
            }
        }

        return null;
    }

    private int ResolveCurrentLevelNumber()
    {
        if (levelController != null)
        {
            return Mathf.Clamp(levelController.CurrentLevelNumber, 1, Mathf.Max(1, LevelCount));
        }

        if (sessionController != null)
        {
            return Mathf.Clamp(sessionController.CurrentLevelNumber, 1, Mathf.Max(1, LevelCount));
        }

        return 1;
    }

    private int GetConfiguredLevelCount()
    {
        int count = 0;
        for (int i = 0; i < levelPrefabs.Count; i++)
        {
            if (levelPrefabs[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private int ResolveConfiguredLevelIndex(int levelNumber)
    {
        int configuredLevelCount = GetConfiguredLevelCount();
        if (configuredLevelCount == 0)
        {
            return -1;
        }

        int targetNonNullIndex = Mathf.Clamp(levelNumber - 1, 0, configuredLevelCount - 1);
        int currentNonNullIndex = 0;
        for (int i = 0; i < levelPrefabs.Count; i++)
        {
            if (levelPrefabs[i] == null)
            {
                continue;
            }

            if (currentNonNullIndex == targetNonNullIndex)
            {
                return i;
            }

            currentNonNullIndex++;
        }

        return -1;
    }

    private Vector3 ResolveLevelStartPosition(Transform levelInstanceTransform)
    {
        if (levelInstanceTransform == null)
        {
            return Vector3.zero;
        }

        ManualLevelRoot levelRoot = levelInstanceTransform.GetComponent<ManualLevelRoot>();
        if (levelRoot == null)
        {
            levelRoot = levelInstanceTransform.GetComponentInChildren<ManualLevelRoot>(true);
        }

        if (levelRoot != null && levelRoot.LevelStartMarker != null)
        {
            return levelRoot.LevelStartMarker.position;
        }

        return levelInstanceTransform.position;
    }

    private void ClearCurrentLevel(GameObject levelInstanceToClear)
    {
        if (levelInstanceToClear == null)
        {
            currentLevelInstance = null;
            return;
        }

        // Manual prefab levels are destroyed and recreated in-place now.
        // Detach the outgoing runtime instance first so the same-frame authored
        // content scan does not mistake it for a valid scene test level.
        if (levelInstanceToClear.transform.parent == levelParent)
        {
            levelInstanceToClear.transform.SetParent(null, false);
        }

        levelInstanceToClear.SetActive(false);

        if (Application.isPlaying)
        {
            Destroy(levelInstanceToClear);
        }
        else
        {
            DestroyImmediate(levelInstanceToClear);
        }

        currentLevelInstance = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
