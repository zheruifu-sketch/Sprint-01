using System.Collections.Generic;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class IcicleSnowSegment : SpecialRoadSegmentBase
{
    private sealed class ActiveIcicle
    {
        public GameObject GameObject;
        public SpriteRenderer[] Renderers;
        public float WarningRemaining;
        public bool IsFalling;
    }

    [Header("Spawn")]
    [LabelText("冰柱预制体")]
    [SerializeField] private GameObject iciclePrefab;
    [LabelText("生成区域")]
    [SerializeField] private Collider2D spawnArea;
    [LabelText("候选生成点")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [LabelText("最小生成间隔")]
    [SerializeField] private float minSpawnInterval = 1.1f;
    [LabelText("最大生成间隔")]
    [SerializeField] private float maxSpawnInterval = 2.1f;
    [LabelText("每次生成概率")]
    [SerializeField] private float spawnChance = 0.8f;
    [LabelText("生成高度补偿")]
    [SerializeField] private float spawnHeightOffset = 0f;

    [Header("Fall")]
    [LabelText("预警时长")]
    [SerializeField] private float warningDuration = 0.55f;
    [LabelText("预警透明度")]
    [SerializeField] private float warningAlpha = 0.35f;
    [LabelText("下落速度")]
    [SerializeField] private float fallSpeed = 13f;
    [LabelText("命中伤害")]
    [SerializeField] private float hitDamage = 35f;
    [LabelText("命中即死")]
    [SerializeField] private bool instantKillOnHit;
    [LabelText("低于玩家多少距离后销毁")]
    [SerializeField] private float destroyBelowPlayerDistance = 10f;
    [LabelText("命中边界扩张")]
    [SerializeField] private float hitPadding = 0.05f;

    [Header("Activation")]
    [LabelText("进入后自动开始")]
    [SerializeField] private bool activateOnPlayerEnter = true;
    [LabelText("离开后继续掉落时长")]
    [SerializeField] private float keepSpawningAfterExitDuration = 0.35f;

    private readonly List<ActiveIcicle> activeIcicles = new List<ActiveIcicle>();
    private Collider2D[] playerColliders;
    private Transform playerTransform;
    private PlayerHealthController playerHealthController;
    private PlayerRespawnController playerRespawnController;
    private float spawnTimer;
    private float stopSpawningTime = float.MinValue;
    private bool playerInside;

    private void Reset()
    {
        EnsureHintDefaults(
            "blizzard.icicle-fall",
            "Icicles overhead. Random drops punish hesitation. Keep moving or brace for impact.");
    }

    private void Awake()
    {
        EnsureHintDefaults(
            "blizzard.icicle-fall",
            "Icicles overhead. Random drops punish hesitation. Keep moving or brace for impact.");
        CachePlayerReferences();
        ScheduleNextSpawn();
    }

    private void OnEnable()
    {
        playerInside = false;
        stopSpawningTime = float.MinValue;
        ScheduleNextSpawn();
    }

    private void OnDisable()
    {
        for (int i = activeIcicles.Count - 1; i >= 0; i--)
        {
            DestroyIcicle(activeIcicles[i]);
        }

        activeIcicles.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!activateOnPlayerEnter || other == null || other.GetComponentInParent<PlayerFormRoot>() == null)
        {
            return;
        }

        playerInside = true;
        stopSpawningTime = float.MaxValue;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!activateOnPlayerEnter || other == null || other.GetComponentInParent<PlayerFormRoot>() == null)
        {
            return;
        }

        playerInside = false;
        stopSpawningTime = Time.time + Mathf.Max(0f, keepSpawningAfterExitDuration);
    }

    private void Update()
    {
        CachePlayerReferences();
        if (playerTransform == null)
        {
            return;
        }

        UpdateSpawner();
        UpdateIcicles();
    }

    private void UpdateSpawner()
    {
        if (iciclePrefab == null)
        {
            return;
        }

        bool canSpawn = !activateOnPlayerEnter || playerInside || Time.time < stopSpawningTime;
        if (!canSpawn)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        ScheduleNextSpawn();
        if (Random.value > Mathf.Clamp01(spawnChance))
        {
            return;
        }

        SpawnIcicle();
    }

    private void SpawnIcicle()
    {
        if (!TryResolveSpawnPosition(out Vector3 spawnPosition))
        {
            return;
        }

        GameObject icicleObject = Instantiate(iciclePrefab, spawnPosition, Quaternion.identity, transform);
        ActiveIcicle icicle = new ActiveIcicle
        {
            GameObject = icicleObject,
            Renderers = icicleObject.GetComponentsInChildren<SpriteRenderer>(true),
            WarningRemaining = Mathf.Max(0f, warningDuration),
            IsFalling = warningDuration <= 0f
        };

        ApplyWarningVisual(icicle);
        activeIcicles.Add(icicle);
    }

    private void UpdateIcicles()
    {
        for (int i = activeIcicles.Count - 1; i >= 0; i--)
        {
            ActiveIcicle icicle = activeIcicles[i];
            if (icicle == null || icicle.GameObject == null)
            {
                activeIcicles.RemoveAt(i);
                continue;
            }

            if (!icicle.IsFalling)
            {
                icicle.WarningRemaining -= Time.deltaTime;
                if (icicle.WarningRemaining <= 0f)
                {
                    icicle.IsFalling = true;
                    ApplyFallingVisual(icicle);
                }
            }
            else
            {
                Vector3 position = icicle.GameObject.transform.position;
                position.y -= Mathf.Max(0f, fallSpeed) * Time.deltaTime;
                icicle.GameObject.transform.position = position;

                if (CheckHit(icicle))
                {
                    DestroyIcicle(icicle);
                    activeIcicles.RemoveAt(i);
                    continue;
                }

                if (playerTransform != null &&
                    position.y <= playerTransform.position.y - Mathf.Max(1f, destroyBelowPlayerDistance))
                {
                    DestroyIcicle(icicle);
                    activeIcicles.RemoveAt(i);
                }
            }
        }
    }

    private bool CheckHit(ActiveIcicle icicle)
    {
        if (icicle == null || icicle.GameObject == null || playerTransform == null)
        {
            return false;
        }

        Bounds hitBounds = GetIcicleBounds(icicle);
        if (hitBounds.size.sqrMagnitude <= 0f)
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

                if (hitBounds.Intersects(playerCollider.bounds))
                {
                    ApplyHit();
                    return true;
                }
            }
        }

        if (hitBounds.Contains(playerTransform.position))
        {
            ApplyHit();
            return true;
        }

        return false;
    }

    private void ApplyHit()
    {
        if (playerRespawnController == null)
        {
            return;
        }

        if (instantKillOnHit || playerHealthController == null)
        {
            playerRespawnController.Respawn(FailureType.HitByFallingRock);
            return;
        }

        playerHealthController.ApplyDamage(hitDamage);
        if (playerHealthController.IsDead())
        {
            playerRespawnController.Respawn(FailureType.HitByFallingRock);
        }
    }

    private Bounds GetIcicleBounds(ActiveIcicle icicle)
    {
        Collider2D collider = icicle.GameObject.GetComponentInChildren<Collider2D>(true);
        if (collider != null && collider.enabled)
        {
            Bounds bounds = collider.bounds;
            bounds.Expand(Mathf.Max(0f, hitPadding));
            return bounds;
        }

        SpriteRenderer[] renderers = icicle.Renderers;
        Bounds result = new Bounds(icicle.GameObject.transform.position, Vector3.zero);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!found)
            {
                result = renderer.bounds;
                found = true;
            }
            else
            {
                result.Encapsulate(renderer.bounds);
            }
        }

        if (!found)
        {
            return new Bounds(icicle.GameObject.transform.position, Vector3.zero);
        }

        result.Expand(Mathf.Max(0f, hitPadding));
        return result;
    }

    private void ApplyWarningVisual(ActiveIcicle icicle)
    {
        if (icicle == null || icicle.Renderers == null)
        {
            return;
        }

        for (int i = 0; i < icicle.Renderers.Length; i++)
        {
            SpriteRenderer renderer = icicle.Renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Color color = renderer.color;
            color.a = Mathf.Clamp01(warningAlpha);
            renderer.color = color;
        }
    }

    private static void ApplyFallingVisual(ActiveIcicle icicle)
    {
        if (icicle == null || icicle.Renderers == null)
        {
            return;
        }

        for (int i = 0; i < icicle.Renderers.Length; i++)
        {
            SpriteRenderer renderer = icicle.Renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Color color = renderer.color;
            color.a = 1f;
            renderer.color = color;
        }
    }

    private void ScheduleNextSpawn()
    {
        spawnTimer = Random.Range(Mathf.Max(0.05f, minSpawnInterval), Mathf.Max(minSpawnInterval, maxSpawnInterval));
    }

    private bool TryResolveSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            int startIndex = Random.Range(0, spawnPoints.Count);
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                Transform point = spawnPoints[(startIndex + i) % spawnPoints.Count];
                if (point == null)
                {
                    continue;
                }

                spawnPosition = point.position + new Vector3(0f, spawnHeightOffset, 0f);
                return true;
            }
        }

        if (spawnArea == null)
        {
            return false;
        }

        Bounds bounds = spawnArea.bounds;
        spawnPosition = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.max.y + spawnHeightOffset,
            0f);
        return true;
    }

    private void CachePlayerReferences()
    {
        if (playerTransform != null && playerRespawnController != null)
        {
            return;
        }

        PlayerRuntimeContext runtimeContext = PlayerRuntimeContext.FindInScene();
        if (runtimeContext == null || runtimeContext.FormRoot == null)
        {
            return;
        }

        playerTransform = runtimeContext.FormRoot.transform;
        playerHealthController = runtimeContext.HealthController;
        playerRespawnController = runtimeContext.RespawnController;
        playerColliders = playerTransform.GetComponentsInChildren<Collider2D>(true);
    }

    private void DestroyIcicle(ActiveIcicle icicle)
    {
        if (icicle == null || icicle.GameObject == null)
        {
            return;
        }

        Destroy(icicle.GameObject);
    }
}
