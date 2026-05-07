using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DefaultExecutionOrder(-900)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerSpawnController : MonoBehaviour
{
    [Header("References")]
    [LabelText("玩家刚体")]
    [SerializeField] private Rigidbody2D playerRigidbody;
    [LabelText("跑局控制器")]
    [SerializeField] private GameSessionController sessionController;
    [LabelText("关卡控制器")]
    [SerializeField] private GameLevelController levelController;

    private void Reset()
    {
        CacheReferences();
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void Start()
    {
        CacheReferences();

        Vector3 sceneStartPosition = transform.position;
        if (sessionController != null)
        {
            sessionController.RegisterSceneEntryPosition(sceneStartPosition);
        }

        int levelNumber = levelController != null
            ? levelController.CurrentLevelNumber
            : (sessionController != null ? sessionController.CurrentLevelNumber : 1);

        if (ManualLevelSequenceController.IsManualModeActive
            && sessionController != null
            && !sessionController.HasActiveRun)
        {
            // In manual level mode, the scene-authored player placement owns the
            // initial Level 1 start position instead of any marker inside the level prefab.
            sessionController.UseSceneEntryAsCurrentLevelStart(levelNumber);
        }

        bool useManualLevelSpawn = ManualLevelSequenceController.IsManualModeActive
                                   && sessionController != null
                                   && sessionController.HasLevelStartPosition
                                   && sessionController.HasActiveRun;

        if (sessionController != null && !useManualLevelSpawn)
        {
            sessionController.PrepareLevelSpawn(levelNumber, sceneStartPosition);
        }

        if (useManualLevelSpawn)
        {
            sceneStartPosition = sessionController.LevelStartPosition;
        }

        if (sessionController != null)
        {
            transform.position = sessionController.GetRespawnPositionOrDefault(sceneStartPosition);
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }
    }

    private void CacheReferences()
    {
        playerRigidbody = playerRigidbody != null ? playerRigidbody : GetComponent<Rigidbody2D>();
        sessionController = sessionController != null ? sessionController : FindObjectOfType<GameSessionController>();
        levelController = levelController != null ? levelController : FindObjectOfType<GameLevelController>();
    }
}
