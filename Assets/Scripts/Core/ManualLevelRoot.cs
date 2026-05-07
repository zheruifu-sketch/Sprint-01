using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class ManualLevelRoot : MonoBehaviour
{
    [Header("References")]
    [LabelText("关卡起点标记")]
    [SerializeField] private Transform levelStartMarker;
    [Header("Scene Test")]
    [LabelText("场景直跑对应关卡")]
    [SerializeField] private int sceneTestLevelNumber = 1;

    public Transform LevelStartMarker => levelStartMarker != null ? levelStartMarker : transform;
    public int SceneTestLevelNumber => Mathf.Max(1, sceneTestLevelNumber);

    private void Reset()
    {
        levelStartMarker = levelStartMarker != null ? levelStartMarker : transform;
    }
}
