using UnityEngine;
using UnityEngine.SceneManagement;

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

    public FailureType LastFailureType { get; private set; } = FailureType.None;
    
    private bool isReloadingScene;

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
    }
}
