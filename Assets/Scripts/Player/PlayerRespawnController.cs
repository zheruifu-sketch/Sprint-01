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

        if (ruleController != null
            && formRoot.CurrentForm == PlayerFormType.Boat
            && ruleController.IsBoatSupportedByFlood())
        {
            return;
        }

        if (TryGetActiveHazard(out FailureType failureType))
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

    private bool TryGetActiveHazard(out FailureType failureType)
    {
        if (ruleController != null
            && formRoot != null
            && formRoot.CurrentForm == PlayerFormType.Boat
            && ruleController.IsBoatSupportedByFlood())
        {
            failureType = FailureType.None;
            return false;
        }

        if (useGlobalFallDeath && transform.position.y <= cliffDeathY)
        {
            failureType = FailureType.FellFromCliff;
            return true;
        }

        if (ruleController != null && formRoot.CurrentForm != PlayerFormType.Boat && ruleController.IsInWater())
        {
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
            failureType = FailureType.FellFromCliff;
            return true;
        }

        if (ruleController != null && formRoot.CurrentForm == PlayerFormType.Boat && !ruleController.IsBoatSupportedSurface())
        {
            failureType = FailureType.InvalidForm;
            return true;
        }

        failureType = FailureType.None;
        return false;
    }
}
