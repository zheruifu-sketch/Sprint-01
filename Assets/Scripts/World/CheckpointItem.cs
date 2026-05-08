using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class CheckpointItem : PlayerCollectibleBase
{
    [Header("Mode")]
    [LabelText("作为关卡终点")]
    [SerializeField] private bool completeLevelOnTouch;

    [Header("State")]
    [LabelText("检查点距离")]
    [SerializeField] private float checkpointDistance;

    private GameFlowController flowController;

    public void Initialize(float distanceFromLevelStart)
    {
        checkpointDistance = Mathf.Max(0f, distanceFromLevelStart);
    }

    protected override void Reset()
    {
        base.Reset();
    }

    protected override void Awake()
    {
        base.Awake();
        flowController = FindObjectOfType<GameFlowController>();
        SetDestroyOnCollect(!completeLevelOnTouch);
    }

    protected override bool Collect(GameObject playerObject)
    {
        GameSessionController sessionController = FindObjectOfType<GameSessionController>();
        if (completeLevelOnTouch)
        {
            if (flowController == null)
            {
                flowController = FindObjectOfType<GameFlowController>();
            }

            if (flowController == null)
            {
                return false;
            }

            flowController.CompleteCurrentLevelFromTrigger();
            return true;
        }

        if (sessionController == null)
        {
            return false;
        }

        PlayerHealthController playerHealth = playerObject.GetComponentInParent<PlayerHealthController>();
        PlayerFuelController playerFuel = playerObject.GetComponentInParent<PlayerFuelController>();
        float checkpointHealth = playerHealth != null ? playerHealth.CurrentHealth : 0f;
        float checkpointFuel = playerFuel != null ? playerFuel.CurrentFuel : 0f;
        sessionController.ActivateCheckpoint(transform.position, checkpointHealth, checkpointFuel);
        return true;
    }
}
