using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PickupSpawner : MonoBehaviour
{
    public static PickupSpawner Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameLevelController levelController;
    [SerializeField] private GameSessionController sessionController;
    [SerializeField] private GameProgressionConfig progressionConfig;
    [SerializeField] private EndlessLevelGenerator levelGenerator;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform pickupParent;

    private readonly List<GameObject> activePickups = new List<GameObject>();
    private float lastSpawnCheckX;
    private bool hasSpawnBaseline;

    public static PickupSpawner GetOrCreateInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        PickupSpawner existing = FindObjectOfType<PickupSpawner>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject controllerObject = new GameObject("PickupSpawner");
        return controllerObject.AddComponent<PickupSpawner>();
    }

    private void Reset()
    {
        levelController = GameLevelController.GetOrCreateInstance();
        sessionController = GameSessionController.GetOrCreate();
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
        levelController = levelController != null ? levelController : GameLevelController.GetOrCreateInstance();
        sessionController = sessionController != null ? sessionController : GameSessionController.GetOrCreate();
        progressionConfig = progressionConfig != null ? progressionConfig : GameProgressionConfig.Load();
        levelGenerator = levelGenerator != null ? levelGenerator : FindObjectOfType<EndlessLevelGenerator>();
        playerTransform = playerTransform != null ? playerTransform : FindPlayerTransform();
        pickupParent = pickupParent != null ? pickupParent : transform;
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
        Collider2D[] colliders = levelGenerator.GetComponentsInChildren<Collider2D>(true);
        if (colliders == null || colliders.Length == 0)
        {
            return false;
        }

        float minX = playerTransform.position.x + settings.MinSpawnAheadDistance;
        float maxX = playerTransform.position.x + settings.MaxSpawnAheadDistance;
        List<Collider2D> candidates = new List<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            ZoneDefinition zoneDefinition = collider.GetComponent<ZoneDefinition>();
            bool isRoad = zoneDefinition != null
                          ? zoneDefinition.ZoneType == ZoneType.Road
                          : collider.CompareTag("Road");
            if (!isRoad)
            {
                continue;
            }

            Bounds bounds = collider.bounds;
            if (bounds.max.x < minX || bounds.min.x > maxX)
            {
                continue;
            }

            candidates.Add(collider);
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        Collider2D selectedCollider = candidates[Random.Range(0, candidates.Count)];
        Bounds selectedBounds = selectedCollider.bounds;
        float spawnMinX = Mathf.Max(minX, selectedBounds.min.x + 0.75f);
        float spawnMaxX = Mathf.Min(maxX, selectedBounds.max.x - 0.75f);
        if (spawnMaxX <= spawnMinX)
        {
            return false;
        }

        spawnPosition = new Vector3(
            Random.Range(spawnMinX, spawnMaxX),
            selectedBounds.max.y + settings.YOffset,
            0f);

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

        PlayerHealthController healthController = playerTransform != null ? playerTransform.GetComponent<PlayerHealthController>() : null;
        PlayerEnergyController energyController = playerTransform != null ? playerTransform.GetComponent<PlayerEnergyController>() : null;

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
