using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerRespawnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFormRoot formRoot;
    [SerializeField] private PlayerRuleController ruleController;
    [SerializeField] private PlayerHealthController healthController;
    [SerializeField] private PlayerEnergyController energyController;
    [Header("Fall Death")]
    [SerializeField] private bool useGlobalFallDeath = true;
    [SerializeField] private float cliffGroundY = GameConstants.DefaultCliffGroundY;
    [SerializeField] private float cliffDeathY = GameConstants.DefaultCliffDeathY;

    public FailureType LastFailureType { get; private set; } = FailureType.None;
    
    private bool isReloadingScene;

    private void Reset()
    {
        formRoot = GetComponent<PlayerFormRoot>();
        ruleController = GetComponent<PlayerRuleController>();
        healthController = GetComponent<PlayerHealthController>();
        energyController = GetComponent<PlayerEnergyController>();
    }

    private void Awake()
    {
        if (formRoot == null)
        {
            formRoot = GetComponent<PlayerFormRoot>();
        }

        if (healthController == null)
        {
            healthController = GetComponent<PlayerHealthController>();
            if (healthController == null)
            {
                healthController = gameObject.AddComponent<PlayerHealthController>();
            }
        }

        if (energyController == null)
        {
            energyController = GetComponent<PlayerEnergyController>();
            if (energyController == null)
            {
                energyController = gameObject.AddComponent<PlayerEnergyController>();
            }
        }
    }

    private void Update()
    {
        UpdateEnergyAndRespawn();
        UpdateHazardDamageAndRespawn();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (formRoot == null || formRoot.CurrentForm != PlayerFormType.Plane)
        {
            return;
        }

        if (collision.collider.CompareTag("Obstacle"))
        {
            if (healthController == null)
            {
                Respawn(FailureType.HitObstacle);
                return;
            }

            healthController.ApplyDamage(healthController.MaxHealth);
            if (healthController.IsDead())
            {
                Respawn(FailureType.HitObstacle);
            }
            return;
        }

        ZoneDefinition zoneDefinition = collision.collider.GetComponent<ZoneDefinition>();
        if (zoneDefinition != null && zoneDefinition.ZoneType == ZoneType.Obstacle)
        {
            if (healthController == null)
            {
                Respawn(FailureType.HitObstacle);
                return;
            }

            healthController.ApplyDamage(healthController.MaxHealth);
            if (healthController.IsDead())
            {
                Respawn(FailureType.HitObstacle);
            }
        }
    }

    public void Respawn(FailureType failureType)
    {
        if (isReloadingScene)
        {
            return;
        }

        Debug.Log($"[Respawn] Trigger form={GetCurrentFormLabel()} failureType={failureType}", this);
        LastFailureType = failureType;
        isReloadingScene = true;

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void UpdateEnergyAndRespawn()
    {
        if (formRoot == null || energyController == null)
        {
            return;
        }

        energyController.ConsumeByForm(formRoot.CurrentForm, Time.deltaTime);
        if (energyController.IsEmpty())
        {
            Respawn(FailureType.EnergyDepleted);
        }
    }

    private void UpdateHazardDamageAndRespawn()
    {
        if (formRoot == null || healthController == null)
        {
            return;
        }

        if (ruleController != null
            && formRoot.CurrentForm == PlayerFormType.Boat
            && ruleController.IsBoatSupportedByFlood())
        {
            Debug.Log($"[RespawnHazard] SkipFloodDamage form={GetCurrentFormLabel()}", this);
            return;
        }

        if (TryGetActiveHazard(out FailureType failureType))
        {
            Debug.Log($"[RespawnHazard] ActiveHazard form={GetCurrentFormLabel()} failureType={failureType}", this);
            if (failureType == FailureType.FellFromCliff)
            {
                Respawn(failureType);
                return;
            }

            healthController.ApplyHazardDamage(Time.deltaTime);
            if (healthController.IsDead())
            {
                Respawn(failureType);
            }
        }
    }

    private bool TryGetActiveHazard(out FailureType failureType)
    {
        if (ruleController != null
            && formRoot != null
            && formRoot.CurrentForm == PlayerFormType.Boat
            && ruleController.IsBoatSupportedByFlood())
        {
            Debug.Log($"[RespawnHazard] FloodBoatImmune form={GetCurrentFormLabel()}", this);
            failureType = FailureType.None;
            return false;
        }

        if (useGlobalFallDeath && transform.position.y <= cliffDeathY)
        {
            Debug.Log($"[RespawnHazard] CliffDeath form={GetCurrentFormLabel()} y={transform.position.y:F3} cliffDeathY={cliffDeathY:F3}", this);
            failureType = FailureType.FellFromCliff;
            return true;
        }

        if (ruleController != null && formRoot.CurrentForm != PlayerFormType.Boat && ruleController.IsInWater())
        {
            Debug.Log($"[RespawnHazard] StaticWater form={GetCurrentFormLabel()}", this);
            failureType = FailureType.FellIntoWater;
            return true;
        }

        bool isInCliffDanger = ruleController != null
                              && formRoot.CurrentForm != PlayerFormType.Plane
                              && ruleController.IsInCliff()
                              && transform.position.y <= cliffGroundY
                              && transform.position.y <= cliffDeathY;
        if (isInCliffDanger)
        {
            Debug.Log($"[RespawnHazard] CliffZone form={GetCurrentFormLabel()} y={transform.position.y:F3}", this);
            failureType = FailureType.FellFromCliff;
            return true;
        }

        if (ruleController != null && formRoot.CurrentForm == PlayerFormType.Boat && !ruleController.IsBoatSupportedSurface())
        {
            Debug.Log($"[RespawnHazard] BoatInvalidSurface form={GetCurrentFormLabel()} floodSupport={ruleController.IsBoatSupportedByFlood()} inFlood={ruleController.IsInFloodWater()} inWater={ruleController.IsInWater()} inBlizzard={ruleController.IsInBlizzard()}", this);
            failureType = FailureType.InvalidForm;
            return true;
        }

        failureType = FailureType.None;
        return false;
    }

    private string GetCurrentFormLabel()
    {
        return formRoot != null ? formRoot.CurrentForm.ToString() : "Unknown";
    }
}
