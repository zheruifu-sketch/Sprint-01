using System.Collections.Generic;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class FallingRocksHazard : LevelHazardBehaviour
{
    private sealed class FallingRockInstance
    {
        public GameObject GameObject;
        public SpriteRenderer[] Renderers;
        public float WarningRemaining;
        public bool IsFalling;
    }

    [Header("Optional Overrides")]
    [LabelText("表现根节点")]
    [SerializeField] private Transform visualRoot;
    [LabelText("预警透明度")]
    [SerializeField] private float warningAlpha = 0.4f;
    [LabelText("低于玩家多少距离后销毁")]
    [SerializeField] private float destroyBelowPlayerDistance = 12f;
    [LabelText("命中边界扩张")]
    [SerializeField] private float hitPadding = 0.05f;

    private readonly List<FallingRockInstance> activeRocks = new List<FallingRockInstance>();

    private HazardProfile hazardProfile;
    private Transform playerTransform;
    private PlayerRuntimeContext playerRuntimeContext;
    private PlayerRespawnController playerRespawnController;
    private PlayerHealthController playerHealthController;
    private Collider2D[] playerColliders;
    private float spawnTimer;
    private bool initialized;

    public override void Initialize(HazardProfile hazardProfile, Transform playerTransform, GameLevelController levelController)
    {
        this.hazardProfile = hazardProfile;
        this.playerTransform = playerTransform;
        playerRuntimeContext = PlayerRuntimeContext.ResolveFromComponent(playerTransform);
        playerRespawnController = playerRuntimeContext != null ? playerRuntimeContext.RespawnController : null;
        playerHealthController = playerRuntimeContext != null ? playerRuntimeContext.HealthController : null;
        playerColliders = playerTransform != null ? playerTransform.GetComponentsInChildren<Collider2D>(true) : null;
        spawnTimer = 0f;
        initialized = true;
        HideManagerVisual();
    }

    private void Reset()
    {
        visualRoot = transform;
    }

    private void Awake()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }
    }

    private void OnDestroy()
    {
        for (int i = activeRocks.Count - 1; i >= 0; i--)
        {
            DestroyRock(activeRocks[i]);
        }

        activeRocks.Clear();
    }

    private void Update()
    {
        if (!initialized || hazardProfile == null || playerTransform == null)
        {
            return;
        }

        UpdateSpawnTimer();
        UpdateRocks();
    }

    private void UpdateSpawnTimer()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        ScheduleNextSpawn();

        if (Random.value <= hazardProfile.FallingRocks.SpawnChance)
        {
            SpawnWave();
        }
    }

    private void SpawnWave()
    {
        HazardProfile.FallingRocksSettings settings = hazardProfile.FallingRocks;
        for (int i = 0; i < settings.RocksPerWave; i++)
        {
            SpawnRock(settings, i);
        }
    }

    private void SpawnRock(HazardProfile.FallingRocksSettings settings, int waveIndex)
    {
        GameObject rockObject = Instantiate(
            hazardProfile.HazardPrefab,
            GetSpawnPosition(settings, waveIndex),
            Quaternion.identity,
            transform);

        rockObject.name = $"FallingRock_{waveIndex}";

        FallingRocksHazard nestedHazard = rockObject.GetComponent<FallingRocksHazard>();
        if (nestedHazard != null)
        {
            Destroy(nestedHazard);
        }

        DisableNestedHazardBehaviours(rockObject);

        FallingRockInstance instance = new FallingRockInstance
        {
            GameObject = rockObject,
            Renderers = rockObject.GetComponentsInChildren<SpriteRenderer>(true),
            WarningRemaining = Mathf.Max(0f, settings.WarningDuration),
            IsFalling = settings.WarningDuration <= 0f
        };

        SetWarningVisual(instance, !instance.IsFalling);
        activeRocks.Add(instance);
    }

    private Vector3 GetSpawnPosition(HazardProfile.FallingRocksSettings settings, int waveIndex)
    {
        float minAhead = settings.MinSpawnAheadDistance;
        float maxAhead = settings.MaxSpawnAheadDistance;
        float aheadX = Random.Range(minAhead, maxAhead);
        if (Mathf.Abs(aheadX) < settings.MinHorizontalDistanceFromPlayer)
        {
            float sign = aheadX >= 0f ? 1f : -1f;
            if (Mathf.Approximately(sign, 0f))
            {
                sign = Random.value < 0.5f ? -1f : 1f;
            }

            aheadX = sign * settings.MinHorizontalDistanceFromPlayer;
        }

        float spreadX = settings.RocksPerWave > 1 ? (waveIndex - (settings.RocksPerWave - 1) * 0.5f) * 1.25f : 0f;

        Vector3 spawnPosition = playerTransform.position;
        spawnPosition.x += aheadX + spreadX;
        spawnPosition.y += settings.SpawnHeight;
        spawnPosition.z = 0f;
        return spawnPosition;
    }

    private void ScheduleNextSpawn()
    {
        HazardProfile.FallingRocksSettings settings = hazardProfile.FallingRocks;
        float baseInterval = Mathf.Max(0.05f, settings.SpawnInterval);
        float jitter = settings.SpawnIntervalJitter;
        spawnTimer = Mathf.Max(0.05f, baseInterval + Random.Range(-jitter, jitter));
    }

    private void UpdateRocks()
    {
        for (int i = activeRocks.Count - 1; i >= 0; i--)
        {
            FallingRockInstance instance = activeRocks[i];
            if (instance == null || instance.GameObject == null)
            {
                activeRocks.RemoveAt(i);
                continue;
            }

            if (!instance.IsFalling)
            {
                instance.WarningRemaining -= Time.deltaTime;
                if (instance.WarningRemaining <= 0f)
                {
                    instance.IsFalling = true;
                    SetWarningVisual(instance, false);
                }
            }
            else
            {
                Vector3 position = instance.GameObject.transform.position;
                position.y -= Mathf.Max(0f, hazardProfile.FallingRocks.FallSpeed) * Time.deltaTime;
                instance.GameObject.transform.position = position;

                if (CheckHit(instance))
                {
                    DestroyRock(instance);
                    activeRocks.RemoveAt(i);
                    continue;
                }

                if (playerTransform != null
                    && position.y <= playerTransform.position.y - Mathf.Max(1f, destroyBelowPlayerDistance))
                {
                    DestroyRock(instance);
                    activeRocks.RemoveAt(i);
                }
            }
        }
    }

    private bool CheckHit(FallingRockInstance instance)
    {
        if (instance == null || instance.GameObject == null || playerTransform == null)
        {
            return false;
        }

        Bounds rockBounds = GetRockBounds(instance);
        if (rockBounds.size.sqrMagnitude <= 0f)
        {
            return false;
        }

        if (playerColliders != null)
        {
            for (int i = 0; i < playerColliders.Length; i++)
            {
                Collider2D playerCollider = playerColliders[i];
                if (playerCollider == null || !playerCollider.enabled)
                {
                    continue;
                }

                if (rockBounds.Intersects(playerCollider.bounds))
                {
                    ApplyRockHit();
                    return true;
                }
            }
        }

        if (rockBounds.Contains(playerTransform.position))
        {
            ApplyRockHit();
            return true;
        }

        return false;
    }

    private Bounds GetRockBounds(FallingRockInstance instance)
    {
        Bounds rockBounds = new Bounds(instance.GameObject.transform.position, Vector3.zero);
        bool found = false;

        if (instance.Renderers != null)
        {
            for (int i = 0; i < instance.Renderers.Length; i++)
            {
                SpriteRenderer renderer = instance.Renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!found)
                {
                    rockBounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    rockBounds.Encapsulate(renderer.bounds);
                }
            }
        }

        if (!found)
        {
            return new Bounds(instance.GameObject.transform.position, Vector3.zero);
        }

        rockBounds.Expand(Mathf.Max(0f, hitPadding));
        return rockBounds;
    }

    private void ApplyRockHit()
    {
        if (hazardProfile.FallingRocks.InstantKillOnHit)
        {
            if (playerHealthController != null)
            {
                playerHealthController.ApplyDamage(playerHealthController.MaxHealth);
            }

            if (playerRespawnController != null)
            {
                playerRespawnController.Respawn(FailureType.HitByFallingRock);
            }

            return;
        }

        if (playerHealthController != null)
        {
            playerHealthController.ApplyHazardDamage(Time.deltaTime);
            if (playerHealthController.IsDead() && playerRespawnController != null)
            {
                playerRespawnController.Respawn(FailureType.HitByFallingRock);
            }
        }
        else if (playerRespawnController != null)
        {
            playerRespawnController.Respawn(FailureType.HitByFallingRock);
        }
    }

    private void HideManagerVisual()
    {
        if (visualRoot == null)
        {
            return;
        }

        SpriteRenderer[] renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
    }

    private void SetWarningVisual(FallingRockInstance instance, bool warningState)
    {
        if (instance == null || instance.Renderers == null)
        {
            return;
        }

        for (int i = 0; i < instance.Renderers.Length; i++)
        {
            SpriteRenderer renderer = instance.Renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = true;
            Color color = renderer.color;
            color.a = warningState ? warningAlpha : 1f;
            renderer.color = color;
        }
    }

    private static void DisableNestedHazardBehaviours(GameObject rockObject)
    {
        LevelHazardBehaviour[] behaviours = rockObject.GetComponentsInChildren<LevelHazardBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
            {
                Destroy(behaviours[i]);
            }
        }
    }

    private static void DestroyRock(FallingRockInstance instance)
    {
        if (instance == null || instance.GameObject == null)
        {
            return;
        }

        Destroy(instance.GameObject);
    }
}
