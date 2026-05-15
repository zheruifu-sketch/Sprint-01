using UnityEngine;
using System.Collections;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[RequireComponent(typeof(PlayerFormRoot))]
[RequireComponent(typeof(PlayerHealthController))]
[RequireComponent(typeof(PlayerFuelController))]
[RequireComponent(typeof(PlayerHazardResolver))]
public class PlayerRespawnController : MonoBehaviour
{
    private const float HumanSprintHealthCostPerSecond = 5f;

    [Header("References")]
    [SerializeField] private PlayerFormRoot formRoot;
    [SerializeField] private PlayerHealthController healthController;
    [SerializeField] private PlayerFuelController fuelController;
    [SerializeField] private PlayerHazardResolver hazardResolver;
    [SerializeField] private GameFlowController flowController;
    [SerializeField] private GameSessionController sessionController;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerFormController formController;
    [LabelText("重生倒计时秒数")]
    [SerializeField] private int respawnCountdownSeconds = 3;
    [LabelText("每拍停留时长")]
    [SerializeField] private float respawnCountdownStepDuration = 0.45f;

    private const float RespawnHealthRatio = 0.3f;

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
        fuelController = fuelController != null ? fuelController : GetComponent<PlayerFuelController>();
        hazardResolver = hazardResolver != null ? hazardResolver : GetComponent<PlayerHazardResolver>();
        flowController = flowController != null ? flowController : FindObjectOfType<GameFlowController>();
        sessionController = sessionController != null ? sessionController : FindObjectOfType<GameSessionController>();
        inputReader = inputReader != null ? inputReader : GetComponent<PlayerInputReader>();
        uiManager = uiManager != null ? uiManager : FindObjectOfType<UIManager>(true);
        formController = formController != null ? formController : GetComponent<PlayerFormController>();
    }

    private void Update()
    {
        if (!CanEvaluateFailure())
        {
            return;
        }

        UpdateFuelAndRespawn();
        UpdateHazardDamageAndRespawn();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!CanEvaluateFailure())
        {
            return;
        }

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
        if (hasTriggeredFailure || !CanEvaluateFailure())
        {
            return;
        }

        if (failureType == FailureType.FuelDepleted)
        {
            HandleFuelDepleted();
            return;
        }

        if (failureType == FailureType.HealthDepleted)
        {
            HandleHealthDepleted();
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

    public void RestorePlayerAtCurrentRespawn(bool resetSurvivalState = true)
    {
        if (sessionController == null)
        {
            sessionController = FindObjectOfType<GameSessionController>();
        }

        if (formRoot == null)
        {
            formRoot = GetComponent<PlayerFormRoot>();
        }

        if (formRoot == null)
        {
            return;
        }

        Vector3 targetPosition = sessionController != null
            ? sessionController.GetRespawnPositionOrDefault(transform.position)
            : transform.position;
        transform.position = targetPosition;

        if (sessionController != null)
        {
            sessionController.StopLevelTimer();
            if (sessionController.HasActiveCheckpoint)
            {
                sessionController.RestoreCheckpointProgressSnapshot();
            }
            else
            {
                sessionController.ResetLevelScoreAndTimer();
            }

            sessionController.StartLevelTimer();
        }

        Rigidbody2D playerRigidbody = formRoot.PlayerRigidbody;
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }

        if (resetSurvivalState)
        {
            bool shouldUseCheckpointSnapshot = sessionController != null && sessionController.HasActiveCheckpoint;
            if (healthController != null)
            {
                if (shouldUseCheckpointSnapshot && sessionController.HasCheckpointHealthSnapshot)
                {
                    healthController.SetHealthDirect(sessionController.CheckpointHealthSnapshot);
                }
                else
                {
                    healthController.ResetHealth();
                }
            }

            if (fuelController != null)
            {
                if (shouldUseCheckpointSnapshot && sessionController.HasCheckpointFuelSnapshot)
                {
                    fuelController.SetFuelDirect(sessionController.CheckpointFuelSnapshot);
                }
                else
                {
                    fuelController.ResetFuel();
                }
            }
        }

        if (formController != null)
        {
            formController.RestoreVehicleForms();
        }

        PlayerBuffController buffController = GetComponent<PlayerBuffController>();
        if (buffController != null)
        {
            buffController.ClearBuffs();
        }

        GameLevelController levelController = FindObjectOfType<GameLevelController>();
        if (levelController != null)
        {
            formRoot.SetForm(levelController.GetFallbackUnlockedForm());
        }
        else
        {
            formRoot.SetForm(PlayerFormType.Human);
        }

        LastFailureType = FailureType.None;
        hasTriggeredFailure = false;
    }

    private bool CanEvaluateFailure()
    {
        if (sessionController == null)
        {
            sessionController = FindObjectOfType<GameSessionController>();
        }

        return sessionController == null || sessionController.IsGameplayRunning;
    }

    private void UpdateFuelAndRespawn()
    {
        if (formRoot == null || fuelController == null)
        {
            return;
        }

        fuelController.ConsumeByForm(formRoot.CurrentForm, Time.deltaTime);
        if (inputReader != null && inputReader.IsSprintHeld)
        {
            fuelController.ConsumeSprintExtraByForm(formRoot.CurrentForm, Time.deltaTime);
        }

        if (fuelController.IsEmpty())
        {
            Respawn(FailureType.FuelDepleted);
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

        if (!hasTriggeredFailure
            && inputReader != null
            && inputReader.IsSprintHeld
            && formRoot.CurrentForm == PlayerFormType.Human)
        {
            healthController.ApplyDamage(HumanSprintHealthCostPerSecond * Time.deltaTime);
        }

        if (!hasTriggeredFailure && healthController.IsDead())
        {
            Respawn(FailureType.HealthDepleted);
        }
    }

    private void HandleFuelDepleted()
    {
        if (fuelController != null)
        {
            fuelController.SetFuelDirect(0f);
        }

        if (formController != null)
        {
            formController.DisableVehicleFormsForFuelDepleted();
        }
    }

    private void HandleHealthDepleted()
    {
        if (hasTriggeredFailure)
        {
            return;
        }

        LastFailureType = FailureType.HealthDepleted;
        hasTriggeredFailure = true;
        StartCoroutine(RespawnFromCheckpointRoutine());
    }

    private IEnumerator RespawnFromCheckpointRoutine()
    {
        RestorePlayerAtCurrentRespawn();
        if (healthController != null)
        {
            healthController.SetHealthDirect(healthController.MaxHealth * RespawnHealthRatio);
        }

        yield return StartCoroutine(PlayRespawnCountdownRoutine());
        LastFailureType = FailureType.None;
        hasTriggeredFailure = false;
    }

    public IEnumerator PlayRespawnCountdownRoutine()
    {
        Time.timeScale = 0f;
        RespawnCountdownPanelUI countdownPanelUi = uiManager != null ? uiManager.Get<RespawnCountdownPanelUI>() : null;
        if (countdownPanelUi != null)
        {
            countdownPanelUi.Show();
        }

        int countdownSeconds = Mathf.Max(1, respawnCountdownSeconds);
        float stepDuration = Mathf.Max(0.05f, respawnCountdownStepDuration);
        for (int seconds = countdownSeconds; seconds >= 1; seconds--)
        {
            if (countdownPanelUi != null)
            {
                countdownPanelUi.SetCountdownText(seconds.ToString());
            }

            yield return new WaitForSecondsRealtime(stepDuration);
        }

        if (countdownPanelUi != null)
        {
            countdownPanelUi.Hide();
        }

        Time.timeScale = 1f;
    }
}
