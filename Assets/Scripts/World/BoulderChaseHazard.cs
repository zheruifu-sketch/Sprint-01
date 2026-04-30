using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class BoulderChaseHazard : LevelHazardBehaviour
{
    [Header("Optional Overrides")]
    [LabelText("表现根节点")]
    [SerializeField] private Transform visualRoot;
    [LabelText("命中碰撞体")]
    [SerializeField] private Collider2D hitCollider;
    [LabelText("命中边界扩张")]
    [SerializeField] private float hitPadding = 0.1f;
    [LabelText("备用命中半径")]
    [SerializeField] private float fallbackHitRadius = 1.25f;

    private HazardProfile hazardProfile;
    private Transform playerTransform;
    private PlayerRuntimeContext playerRuntimeContext;
    private PlayerRespawnController playerRespawnController;
    private PlayerHealthController playerHealthController;
    private Collider2D[] playerColliders;
    private float currentSpeed;
    private float fixedY;
    private bool initialized;
    private bool hasTriggeredHit;

    public override void Initialize(HazardProfile hazardProfile, Transform playerTransform, GameLevelController levelController)
    {
        this.hazardProfile = hazardProfile;
        this.playerTransform = playerTransform;
        playerRuntimeContext = PlayerRuntimeContext.ResolveFromComponent(playerTransform);
        playerRespawnController = playerRuntimeContext != null ? playerRuntimeContext.RespawnController : null;
        playerHealthController = playerRuntimeContext != null ? playerRuntimeContext.HealthController : null;
        playerColliders = playerTransform != null ? playerTransform.GetComponentsInChildren<Collider2D>(true) : null;

        HazardProfile.BoulderChaseSettings settings = hazardProfile != null ? hazardProfile.BoulderChase : null;
        currentSpeed = settings != null ? Mathf.Max(0f, settings.MoveSpeed) : 0f;

        Vector3 position = transform.position;
        if (playerTransform != null && settings != null)
        {
            position.x = playerTransform.position.x - settings.StartBehindDistance;
        }

        transform.position = position;
        fixedY = position.y;
        initialized = true;
        hasTriggeredHit = false;
    }

    private void Reset()
    {
        visualRoot = transform;
        hitCollider = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (hitCollider == null)
        {
            hitCollider = GetComponent<Collider2D>();
        }
    }

    private void Update()
    {
        if (!initialized || hazardProfile == null)
        {
            return;
        }

        MoveForward();
        CheckPlayerHit();
    }

    private void MoveForward()
    {
        HazardProfile.BoulderChaseSettings settings = hazardProfile.BoulderChase;
        currentSpeed += Mathf.Max(0f, settings.Acceleration) * Time.deltaTime;

        Vector3 position = transform.position;
        position.x += currentSpeed * Time.deltaTime;
        position.y = fixedY;
        transform.position = position;
    }

    private void CheckPlayerHit()
    {
        if (hasTriggeredHit || playerTransform == null)
        {
            return;
        }

        Bounds hazardBounds = GetHazardBounds();
        if (hazardBounds.size.sqrMagnitude <= 0f)
        {
            float radius = Mathf.Max(0.1f, fallbackHitRadius);
            float distanceSq = (playerTransform.position - transform.position).sqrMagnitude;
            if (distanceSq <= radius * radius)
            {
                TriggerHit();
            }

            return;
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

                if (hazardBounds.Intersects(playerCollider.bounds))
                {
                    TriggerHit();
                    return;
                }
            }
        }

        if (hazardBounds.Contains(playerTransform.position))
        {
            TriggerHit();
        }
    }

    private Bounds GetHazardBounds()
    {
        if (hitCollider != null && hitCollider.enabled)
        {
            Bounds colliderBounds = hitCollider.bounds;
            colliderBounds.Expand(Mathf.Max(0f, hitPadding));
            return colliderBounds;
        }

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        Bounds hazardBounds = new Bounds(transform.position, Vector3.zero);
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
                hazardBounds = renderer.bounds;
                found = true;
            }
            else
            {
                hazardBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!found)
        {
            return new Bounds(transform.position, Vector3.zero);
        }

        hazardBounds.Expand(Mathf.Max(0f, hitPadding));
        return hazardBounds;
    }

    private void TriggerHit()
    {
        hasTriggeredHit = true;

        if (hazardProfile.BoulderChase.InstantKillOnTouch)
        {
            if (playerHealthController != null)
            {
                playerHealthController.ApplyDamage(playerHealthController.MaxHealth);
            }

            if (playerRespawnController != null)
            {
                playerRespawnController.Respawn(FailureType.CrushedByBoulder);
            }

            return;
        }

        if (playerHealthController != null)
        {
            playerHealthController.ApplyHazardDamage(Time.deltaTime);
            if (playerHealthController.IsDead() && playerRespawnController != null)
            {
                playerRespawnController.Respawn(FailureType.CrushedByBoulder);
                return;
            }

            hasTriggeredHit = false;
        }
        else if (playerRespawnController != null)
        {
            playerRespawnController.Respawn(FailureType.CrushedByBoulder);
        }
    }
}
