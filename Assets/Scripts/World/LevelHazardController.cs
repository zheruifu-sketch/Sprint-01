using System.Collections.Generic;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class LevelHazardController : MonoBehaviour
{
    public static LevelHazardController Instance { get; private set; }

    [Header("References")]
    [LabelText("关卡控制器")]
    [SerializeField] private GameLevelController levelController;
    [LabelText("流程配置")]
    [SerializeField] private GameProgressionConfig progressionConfig;
    [LabelText("玩家节点")]
    [SerializeField] private Transform playerTransform;
    [LabelText("灾害父节点")]
    [SerializeField] private Transform hazardParent;

    private readonly List<GameObject> activeHazards = new List<GameObject>();

    private void Reset()
    {
        levelController = FindObjectOfType<GameLevelController>();
        progressionConfig = GameProgressionConfig.Load();
        playerTransform = FindPlayerTransform();
        hazardParent = transform;
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
        progressionConfig = progressionConfig != null ? progressionConfig : GameProgressionConfig.Load();
        playerTransform = playerTransform != null ? playerTransform : FindPlayerTransform();
        hazardParent = hazardParent != null ? hazardParent : transform;
    }

    private void Start()
    {
        RefreshHazardsForCurrentLevel();
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
    }

    private void OnDisable()
    {
        if (levelController != null)
        {
            levelController.LevelChanged -= HandleLevelChanged;
        }
    }

    [ContextMenu("Refresh Hazards")]
    public void RefreshHazardsForCurrentLevel()
    {
        ClearHazards();

        if (progressionConfig == null || levelController == null)
        {
            return;
        }

        GameProgressionConfig.LevelDefinition levelDefinition = progressionConfig.GetLevel(levelController.CurrentLevelIndex);
        if (levelDefinition == null || levelDefinition.Hazards == null || levelDefinition.Hazards.Count == 0)
        {
            return;
        }

        for (int i = 0; i < levelDefinition.Hazards.Count; i++)
        {
            HazardProfile hazardProfile = levelDefinition.Hazards[i];
            if (hazardProfile == null || !hazardProfile.Enabled || hazardProfile.HazardPrefab == null)
            {
                continue;
            }

            SpawnHazard(hazardProfile);
        }
    }

    [ContextMenu("Clear Hazards")]
    public void ClearHazards()
    {
        for (int i = activeHazards.Count - 1; i >= 0; i--)
        {
            GameObject activeHazard = activeHazards[i];
            if (activeHazard == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(activeHazard);
            }
            else
            {
                DestroyImmediate(activeHazard);
            }
        }

        activeHazards.Clear();
    }

    private void SpawnHazard(HazardProfile hazardProfile)
    {
        Vector3 spawnPosition = hazardProfile.SpawnPositionMode == HazardProfile.HazardSpawnPositionMode.RelativeToPlayer && playerTransform != null
            ? playerTransform.position + hazardProfile.SpawnOffset
            : hazardProfile.SpawnOffset;

        GameObject hazardInstance = Instantiate(
            hazardProfile.HazardPrefab,
            spawnPosition,
            Quaternion.identity,
            hazardParent);

        hazardInstance.name = string.IsNullOrWhiteSpace(hazardProfile.DisplayName)
            ? $"{hazardProfile.HazardType} Hazard"
            : hazardProfile.DisplayName;

        activeHazards.Add(hazardInstance);

        EnsureHazardBehaviourForType(hazardInstance, hazardProfile.HazardType);

        LevelHazardBehaviour[] behaviours = hazardInstance.GetComponentsInChildren<LevelHazardBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            behaviours[i].Initialize(hazardProfile, playerTransform, levelController);
        }
    }

    private static void EnsureHazardBehaviourForType(GameObject hazardInstance, GameHazardType hazardType)
    {
        if (hazardInstance == null)
        {
            return;
        }

        switch (hazardType)
        {
            case GameHazardType.BoulderChase:
                if (hazardInstance.GetComponentInChildren<BoulderChaseHazard>(true) == null)
                {
                    hazardInstance.AddComponent<BoulderChaseHazard>();
                }
                break;

            case GameHazardType.FallingRocks:
                if (hazardInstance.GetComponentInChildren<FallingRocksHazard>(true) == null)
                {
                    hazardInstance.AddComponent<FallingRocksHazard>();
                }
                break;
        }
    }

    private void HandleLevelChanged(int _)
    {
        RefreshHazardsForCurrentLevel();
    }

    private static Transform FindPlayerTransform()
    {
        PlayerRuntimeContext runtimeContext = PlayerRuntimeContext.FindInScene();
        if (runtimeContext != null && runtimeContext.FormRoot != null)
        {
            return runtimeContext.FormRoot.transform;
        }

        PlayerFormRoot player = FindObjectOfType<PlayerFormRoot>();
        return player != null ? player.transform : null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
