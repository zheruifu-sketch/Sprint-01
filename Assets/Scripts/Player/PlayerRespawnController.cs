using UnityEngine;

[RequireComponent(typeof(PlayerFormRoot))]
public class PlayerRespawnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFormRoot formRoot;
    [SerializeField] private PlayerRuleController ruleController;
    [SerializeField] private PlayerHealthController healthController;
    [SerializeField] private PlayerEnergyController energyController;

    [Header("Respawn")]
    [SerializeField] private PlayerFormType respawnForm = PlayerFormType.Human;
    [SerializeField] private float cliffGroundY = GameConstants.DefaultCliffGroundY;
    [SerializeField] private float cliffDeathY = GameConstants.DefaultCliffDeathY;

    public FailureType LastFailureType { get; private set; } = FailureType.None;

    private Vector3 spawnPosition;

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

        spawnPosition = transform.position;
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
        LastFailureType = failureType;

        transform.position = spawnPosition;
        if (formRoot != null && formRoot.PlayerRigidbody != null)
        {
            formRoot.PlayerRigidbody.velocity = Vector2.zero;
            formRoot.PlayerRigidbody.angularVelocity = 0f;
        }

        if (formRoot != null)
        {
            formRoot.SetForm(respawnForm);
        }

        if (healthController != null)
        {
            healthController.ResetHealth();
        }

        if (energyController != null)
        {
            energyController.ResetEnergy();
        }
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
        if (ruleController == null || formRoot == null || healthController == null)
        {
            return;
        }

        if (TryGetActiveHazard(out FailureType failureType))
        {
            healthController.ApplyHazardDamage(Time.deltaTime);
            if (healthController.IsDead())
            {
                Respawn(failureType);
            }
        }
    }

    private bool TryGetActiveHazard(out FailureType failureType)
    {
        if (formRoot.CurrentForm != PlayerFormType.Boat && ruleController.IsInWater())
        {
            failureType = FailureType.FellIntoWater;
            return true;
        }

        bool isInCliffDanger = formRoot.CurrentForm != PlayerFormType.Plane
                              && ruleController.IsInCliff()
                              && transform.position.y <= cliffGroundY
                              && transform.position.y <= cliffDeathY;
        if (isInCliffDanger)
        {
            failureType = FailureType.FellFromCliff;
            return true;
        }

        if (formRoot.CurrentForm == PlayerFormType.Boat && !ruleController.IsBoatSupportedSurface())
        {
            failureType = FailureType.InvalidForm;
            return true;
        }

        failureType = FailureType.None;
        return false;
    }
}
