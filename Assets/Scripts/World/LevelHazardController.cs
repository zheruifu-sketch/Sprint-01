using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelHazardController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameLevelController levelController;
    [SerializeField] private GameProgressionConfig progressionConfig;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform hazardParent;

    private readonly List<GameObject> activeHazards = new List<GameObject>();

    public static LevelHazardController GetOrCreateInstance()
    {
        LevelHazardController existing = FindObjectOfType<LevelHazardController>();
        if (existing != null)
        {
            return existing;
        }

        GameObject controllerObject = new GameObject("LevelHazardController");
        return controllerObject.AddComponent<LevelHazardController>();
    }

    private void Reset()
    {
        levelController = GameLevelController.GetOrCreateInstance();
        progressionConfig = GameProgressionConfig.Load();
        playerTransform = FindPlayerTransform();
        hazardParent = transform;
    }

    private void Awake()
    {
        levelController = levelController != null ? levelController : GameLevelController.GetOrCreateInstance();
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
            levelController = GameLevelController.GetOrCreateInstance();
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

        LevelHazardBehaviour[] behaviours = hazardInstance.GetComponentsInChildren<LevelHazardBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            behaviours[i].Initialize(hazardProfile, playerTransform, levelController);
        }
    }

    private void HandleLevelChanged(int _)
    {
        RefreshHazardsForCurrentLevel();
    }

    private static Transform FindPlayerTransform()
    {
        PlayerFormRoot player = FindObjectOfType<PlayerFormRoot>();
        return player != null ? player.transform : null;
    }
}
