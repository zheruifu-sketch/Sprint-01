using System.Collections.Generic;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class PickupSpawner : MonoBehaviour
{
    public static PickupSpawner Instance { get; private set; }

    [Header("References")]
    [LabelText("关卡控制器")]
    [SerializeField] private GameLevelController levelController;
    [LabelText("跑局控制器")]
    [SerializeField] private GameSessionController sessionController;
    [LabelText("流程配置")]
    [SerializeField] private GameProgressionConfig progressionConfig;
    [LabelText("关卡生成器")]
    [SerializeField] private EndlessLevelGenerator levelGenerator;
    [LabelText("玩家节点")]
    [SerializeField] private Transform playerTransform;
    [LabelText("拾取物父节点")]
    [SerializeField] private Transform pickupParent;

    private readonly List<GameObject> activePickups = new List<GameObject>();
    private float lastSpawnCheckX;
    private bool hasSpawnBaseline;
    private PlayerRuntimeContext playerRuntimeContext;

    private void Reset()
    {
        levelController = FindObjectOfType<GameLevelController>();
        sessionController = FindObjectOfType<GameSessionController>();
        progressionConfig = GameProgressionConfig.Load();
        levelGenerator = FindObjectOfType<EndlessLevelGenerator>();
        playerTransform = FindPlayerTransform();
        pickupParent = transform;
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
        progressionConfig = progressionConfig != null ? progressionConfig : GameProgressionConfig.Load();
        levelGenerator = levelGenerator != null ? levelGenerator : FindObjectOfType<EndlessLevelGenerator>();
        playerTransform = playerTransform != null ? playerTransform : FindPlayerTransform();
        playerRuntimeContext = playerTransform != null ? playerTransform.GetComponent<PlayerRuntimeContext>() : null;
        pickupParent = pickupParent != null ? pickupParent : transform;
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

    private void Update()
    {
        CleanupCollectedPickups();

        if (sessionController == null || !sessionController.HasActiveRun)
        {
            return;
        }

        if (playerTransform == null)
        {
            playerTransform = FindPlayerTransform();
            if (playerTransform == null)
            {
                return;
            }

            playerRuntimeContext = playerTransform.GetComponent<PlayerRuntimeContext>();
        }

        if (levelGenerator == null)
        {
            levelGenerator = FindObjectOfType<EndlessLevelGenerator>();
            if (levelGenerator == null)
            {
                return;
            }
        }

        GameProgressionConfig.LevelDefinition levelDefinition = progressionConfig != null && levelController != null
            ? progressionConfig.GetLevel(levelController.CurrentLevelIndex)
            : null;
        if (levelDefinition == null || levelDefinition.Pickups == null || !levelDefinition.Pickups.Enabled)
        {
            return;
        }

        GameProgressionConfig.LevelDefinition.PickupSpawnSettings settings = levelDefinition.Pickups;
        if (!hasSpawnBaseline)
        {
            lastSpawnCheckX = playerTransform.position.x;
            hasSpawnBaseline = true;
            return;
        }

        if (activePickups.Count >= settings.MaxActivePickups)
        {
            return;
        }

        float deltaX = playerTransform.position.x - lastSpawnCheckX;
        if (deltaX < settings.MinSpawnDistance)
        {
            return;
        }

        lastSpawnCheckX = playerTransform.position.x;
        if (Random.value > settings.SpawnChance)
        {
            return;
        }

        PickupProfile profile = ChooseProfile(settings.Profiles);
        if (profile == null)
        {
            return;
        }

        if (!TryResolveRoadSpawnPosition(settings, out Vector3 spawnPosition))
        {
            return;
        }

        SpawnPickup(profile, spawnPosition);
    }

    public void ClearSpawnedPickups()
    {
        for (int i = activePickups.Count - 1; i >= 0; i--)
        {
            GameObject pickup = activePickups[i];
            if (pickup == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(pickup);
            }
            else
            {
                DestroyImmediate(pickup);
            }
        }

        activePickups.Clear();
    }

    private void SpawnPickup(PickupProfile profile, Vector3 spawnPosition)
    {
        if (profile == null || profile.PickupPrefab == null)
        {
            return;
        }

        GameObject pickupInstance = Instantiate(profile.PickupPrefab, spawnPosition, Quaternion.identity, pickupParent);
        pickupInstance.name = string.IsNullOrWhiteSpace(profile.DisplayName)
            ? profile.PickupType.ToString()
            : profile.DisplayName;

        PickupItem pickupItem = pickupInstance.GetComponent<PickupItem>();
        if (pickupItem == null)
        {
            pickupItem = pickupInstance.AddComponent<PickupItem>();
        }

        pickupItem.Initialize(profile);
        activePickups.Add(pickupInstance);
    }

    private bool TryResolveRoadSpawnPosition(GameProgressionConfig.LevelDefinition.PickupSpawnSettings settings, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        if (levelGenerator == null || playerTransform == null)
        {
            return false;
        }

        float minX = playerTransform.position.x + settings.MinSpawnAheadDistance;
        float maxX = playerTransform.position.x + settings.MaxSpawnAheadDistance;
        if (!levelGenerator.TryGetRandomPickupSpawnPoint(EnvironmentType.Road, minX, maxX, settings.YOffset, out spawnPosition))
        {
            return false;
        }

        return !IsPickupTooClose(spawnPosition);
    }

    private bool IsPickupTooClose(Vector3 spawnPosition)
    {
        const float minSpacing = 3f;
        for (int i = 0; i < activePickups.Count; i++)
        {
            GameObject pickup = activePickups[i];
            if (pickup == null)
            {
                continue;
            }

            if (Mathf.Abs(pickup.transform.position.x - spawnPosition.x) < minSpacing)
            {
                return true;
            }
        }

        return false;
    }

    private PickupProfile ChooseProfile(List<PickupProfile> profiles)
    {
        if (profiles == null || profiles.Count == 0)
        {
            return null;
        }

        playerRuntimeContext = playerRuntimeContext != null ? playerRuntimeContext : (playerTransform != null ? playerTransform.GetComponent<PlayerRuntimeContext>() : null);
        PlayerHealthController healthController = playerRuntimeContext != null ? playerRuntimeContext.HealthController : null;
        PlayerEnergyController energyController = playerRuntimeContext != null ? playerRuntimeContext.EnergyController : null;

        List<PickupProfile> validProfiles = new List<PickupProfile>();
        float totalWeight = 0f;

        for (int i = 0; i < profiles.Count; i++)
        {
            PickupProfile profile = profiles[i];
            if (profile == null || !profile.Enabled || profile.PickupPrefab == null)
            {
                continue;
            }

            if (profile.SkipSpawnWhenStatIsFull)
            {
                if (profile.PickupType == PickupType.Health && healthController != null && healthController.IsFull())
                {
                    continue;
                }

                if (profile.PickupType == PickupType.Energy && energyController != null && energyController.IsFull())
                {
                    continue;
                }
            }

            validProfiles.Add(profile);
            totalWeight += profile.Weight;
        }

        if (validProfiles.Count == 0 || totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.Range(0f, totalWeight);
        for (int i = 0; i < validProfiles.Count; i++)
        {
            roll -= validProfiles[i].Weight;
            if (roll <= 0f)
            {
                return validProfiles[i];
            }
        }

        return validProfiles[validProfiles.Count - 1];
    }

    private void CleanupCollectedPickups()
    {
        for (int i = activePickups.Count - 1; i >= 0; i--)
        {
            if (activePickups[i] == null)
            {
                activePickups.RemoveAt(i);
            }
        }
    }

    private void HandleLevelChanged(int _)
    {
        ClearSpawnedPickups();
        hasSpawnBaseline = false;
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
