using UnityEngine;

[RequireComponent(typeof(PlayerFormRoot))]
[RequireComponent(typeof(PlayerHealthController))]
[RequireComponent(typeof(PlayerEnergyController))]
[RequireComponent(typeof(PlayerHazardResolver))]
public class PlayerRespawnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFormRoot formRoot;
    [SerializeField] private PlayerHealthController healthController;
    [SerializeField] private PlayerEnergyController energyController;
    [SerializeField] private PlayerHazardResolver hazardResolver;
    [SerializeField] private GameFlowController flowController;
    [SerializeField] private PlayerInputReader inputReader;

    public FailureType LastFailureType { get; private set; } = FailureType.None;
    
    private bool hasTriggeredFailure;

    private void Reset()
    {
        CacheReferences();
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void CacheReferences()
    {
        formRoot = formRoot != null ? formRoot : GetComponent<PlayerFormRoot>();
        healthController = healthController != null ? healthController : GetComponent<PlayerHealthController>();
        energyController = energyController != null ? energyController : GetComponent<PlayerEnergyController>();
        hazardResolver = hazardResolver != null ? hazardResolver : GetComponent<PlayerHazardResolver>();
        flowController = flowController != null ? flowController : FindObjectOfType<GameFlowController>();
        inputReader = inputReader != null ? inputReader : GetComponent<PlayerInputReader>();
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

        if (WorldSemanticUtility.HasEnvironment(collision.collider, EnvironmentType.Obstacle) ||
            (WorldSemanticUtility.ResolveRuleTags(collision.collider) & RuleTag.Obstacle) != 0)
        {
            if (healthController == null)
            {
                Respawn(FailureType.PlaneCrash);
                return;
            }

            healthController.ApplyDamage(healthController.MaxHealth);
            if (healthController.IsDead())
            {
                Respawn(FailureType.PlaneCrash);
            }
        }
    }

    public void Respawn(FailureType failureType)
    {
        if (hasTriggeredFailure)
        {
            return;
        }

        LastFailureType = failureType;
        hasTriggeredFailure = true;
        flowController = flowController != null ? flowController : FindObjectOfType<GameFlowController>();
        if (flowController != null)
        {
            flowController.HandleRunFailed(failureType);
        }
    }

    private void UpdateEnergyAndRespawn()
    {
        if (formRoot == null || energyController == null)
        {
            return;
        }

        energyController.ConsumeByForm(formRoot.CurrentForm, Time.deltaTime);
        if (inputReader != null && inputReader.IsForwardBoostHeld)
        {
            energyController.ConsumeForForwardBoost(Time.deltaTime);
        }

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

        if (hazardResolver != null && hazardResolver.IsProtectedBoatState())
        {
            return;
        }

        if (hazardResolver != null && hazardResolver.TryGetActiveHazard(out FailureType failureType))
        {
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

        if (!hasTriggeredFailure && healthController.IsDead())
        {
            Respawn(FailureType.HealthDepleted);
        }
    }
}
