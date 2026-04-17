using UnityEngine;

[CreateAssetMenu(fileName = "PickupProfile", menuName = "JumpGame/Pickup Profile")]
public class PickupProfile : ScriptableObject
{
    [Header("Base")]
    [SerializeField] private bool enabled = true;
    [SerializeField] private string displayName = string.Empty;
    [SerializeField] private PickupType pickupType = PickupType.Health;
    [SerializeField] private GameObject pickupPrefab;

    [Header("Effect")]
    [SerializeField] private float amount = 25f;
    [SerializeField] private bool skipSpawnWhenStatIsFull = true;
    [SerializeField] private float weight = 1f;

    public bool Enabled => enabled;
    public string DisplayName => displayName;
    public PickupType PickupType => pickupType;
    public GameObject PickupPrefab => pickupPrefab;
    public float Amount => Mathf.Max(0f, amount);
    public bool SkipSpawnWhenStatIsFull => skipSpawnWhenStatIsFull;
    public float Weight => Mathf.Max(0.01f, weight);
}
