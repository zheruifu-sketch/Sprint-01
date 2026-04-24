using UnityEngine;

[DisallowMultipleComponent]
public class SegmentDescriptor : MonoBehaviour
{
    [Header("Semantic")]
    [SerializeField] private EnvironmentType primaryEnvironment = EnvironmentType.None;

    [Header("Layout")]
    [SerializeField] private float explicitLength;
    [SerializeField] private Transform pickupAnchorRoot;
    [SerializeField] private Transform obstacleAnchorRoot;

    public EnvironmentType PrimaryEnvironment => primaryEnvironment;
    public float ExplicitLength => Mathf.Max(0f, explicitLength);
    public Transform PickupAnchorRoot => pickupAnchorRoot;
    public Transform ObstacleAnchorRoot => obstacleAnchorRoot;
}
