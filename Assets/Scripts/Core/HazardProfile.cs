using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HazardProfile", menuName = "JumpGame/Hazard Profile")]
public class HazardProfile : ScriptableObject
{
    public enum HazardSpawnPositionMode
    {
        World = 0,
        RelativeToPlayer = 1
    }

    [Serializable]
    public class BoulderChaseSettings
    {
        [SerializeField] private float startBehindDistance = 12f;
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float acceleration = 0f;
        [SerializeField] private bool instantKillOnTouch = true;

        public float StartBehindDistance => startBehindDistance;
        public float MoveSpeed => moveSpeed;
        public float Acceleration => acceleration;
        public bool InstantKillOnTouch => instantKillOnTouch;
    }

    [Serializable]
    public class RisingWaterSettings
    {
        [SerializeField] private float startY = -4f;
        [SerializeField] private float riseSpeed = 0.75f;
        [SerializeField] private float fallSpeed = 0.9f;
        [SerializeField] private float maxY = 8f;
        [SerializeField] private float minHoldAtHighDuration = 1.5f;
        [SerializeField] private float maxHoldAtHighDuration = 3.5f;
        [SerializeField] private float minHoldAtLowDuration = 2f;
        [SerializeField] private float maxHoldAtLowDuration = 5f;
        [SerializeField] private bool instantKillBelowWaterLine = false;

        public float StartY => startY;
        public float RiseSpeed => Mathf.Max(0f, riseSpeed);
        public float FallSpeed => Mathf.Max(0f, fallSpeed);
        public float MaxY => Mathf.Max(startY, maxY);
        public float MinHoldAtHighDuration => Mathf.Max(0f, minHoldAtHighDuration);
        public float MaxHoldAtHighDuration => Mathf.Max(MinHoldAtHighDuration, maxHoldAtHighDuration);
        public float MinHoldAtLowDuration => Mathf.Max(0f, minHoldAtLowDuration);
        public float MaxHoldAtLowDuration => Mathf.Max(MinHoldAtLowDuration, maxHoldAtLowDuration);
        public bool InstantKillBelowWaterLine => instantKillBelowWaterLine;
    }

    [Serializable]
    public class FallingRocksSettings
    {
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private float spawnIntervalJitter = 0.75f;
        [SerializeField] private float spawnChance = 0.7f;
        [SerializeField] private int rocksPerWave = 1;
        [SerializeField] private float warningDuration = 0.75f;
        [SerializeField] private float spawnHeight = 10f;
        [SerializeField] private float minSpawnAheadDistance = 6f;
        [SerializeField] private float maxSpawnAheadDistance = 14f;
        [SerializeField] private float minHorizontalDistanceFromPlayer = 3f;
        [SerializeField] private float fallSpeed = 12f;
        [SerializeField] private bool instantKillOnHit = true;

        public float SpawnInterval => spawnInterval;
        public float SpawnIntervalJitter => Mathf.Max(0f, spawnIntervalJitter);
        public float SpawnChance => Mathf.Clamp01(spawnChance);
        public int RocksPerWave => Mathf.Max(1, rocksPerWave);
        public float WarningDuration => warningDuration;
        public float SpawnHeight => spawnHeight;
        public float MinSpawnAheadDistance => minSpawnAheadDistance;
        public float MaxSpawnAheadDistance => Mathf.Max(minSpawnAheadDistance, maxSpawnAheadDistance);
        public float MinHorizontalDistanceFromPlayer => Mathf.Max(0f, minHorizontalDistanceFromPlayer);
        public float FallSpeed => fallSpeed;
        public bool InstantKillOnHit => instantKillOnHit;
    }

    [Header("Base")]
    [SerializeField] private bool enabled = true;
    [SerializeField] private string displayName = string.Empty;
    [SerializeField] private GameHazardType hazardType = GameHazardType.None;
    [SerializeField] private GameObject hazardPrefab;
    [SerializeField] private HazardSpawnPositionMode spawnPositionMode = HazardSpawnPositionMode.RelativeToPlayer;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    [Header("Settings")]
    [SerializeField] private BoulderChaseSettings boulderChase = new BoulderChaseSettings();
    [SerializeField] private RisingWaterSettings risingWater = new RisingWaterSettings();
    [SerializeField] private FallingRocksSettings fallingRocks = new FallingRocksSettings();

    public bool Enabled => enabled;
    public string DisplayName => displayName;
    public GameHazardType HazardType => hazardType;
    public GameObject HazardPrefab => hazardPrefab;
    public HazardSpawnPositionMode SpawnPositionMode => spawnPositionMode;
    public Vector3 SpawnOffset => spawnOffset;
    public BoulderChaseSettings BoulderChase => boulderChase;
    public RisingWaterSettings RisingWater => risingWater;
    public FallingRocksSettings FallingRocks => fallingRocks;
}
