using System;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

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
        [LabelText("初始落后距离")]
        [SerializeField] private float startBehindDistance = 12f;
        [LabelText("移动速度")]
        [SerializeField] private float moveSpeed = 6f;
        [LabelText("加速度")]
        [SerializeField] private float acceleration = 0f;
        [LabelText("接触即死")]
        [SerializeField] private bool instantKillOnTouch = true;

        public float StartBehindDistance => startBehindDistance;
        public float MoveSpeed => moveSpeed;
        public float Acceleration => acceleration;
        public bool InstantKillOnTouch => instantKillOnTouch;
    }

    [Serializable]
    public class FallingRocksSettings
    {
        [LabelText("生成间隔")]
        [SerializeField] private float spawnInterval = 2f;
        [LabelText("生成间隔抖动")]
        [SerializeField] private float spawnIntervalJitter = 0.75f;
        [LabelText("生成概率")]
        [SerializeField] private float spawnChance = 0.7f;
        [LabelText("每波落石数量")]
        [SerializeField] private int rocksPerWave = 1;
        [LabelText("预警时长")]
        [SerializeField] private float warningDuration = 0.75f;
        [LabelText("生成高度")]
        [SerializeField] private float spawnHeight = 10f;
        [LabelText("最小前方距离")]
        [SerializeField] private float minSpawnAheadDistance = 6f;
        [LabelText("最大前方距离")]
        [SerializeField] private float maxSpawnAheadDistance = 14f;
        [LabelText("与玩家最小水平距离")]
        [SerializeField] private float minHorizontalDistanceFromPlayer = 3f;
        [LabelText("下落速度")]
        [SerializeField] private float fallSpeed = 12f;
        [LabelText("命中即死")]
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
    [LabelText("启用")]
    [SerializeField] private bool enabled = true;
    [LabelText("显示名称")]
    [SerializeField] private string displayName = string.Empty;
    [LabelText("灾害类型")]
    [SerializeField] private GameHazardType hazardType = GameHazardType.None;
    [LabelText("灾害预制体")]
    [SerializeField] private GameObject hazardPrefab;
    [LabelText("生成位置模式")]
    [SerializeField] private HazardSpawnPositionMode spawnPositionMode = HazardSpawnPositionMode.RelativeToPlayer;
    [LabelText("生成偏移")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    [Header("Settings")]
    [LabelText("滚石追击参数")]
    [SerializeField] private BoulderChaseSettings boulderChase = new BoulderChaseSettings();
    [LabelText("落石参数")]
    [SerializeField] private FallingRocksSettings fallingRocks = new FallingRocksSettings();

    public bool Enabled => enabled;
    public string DisplayName => displayName;
    public GameHazardType HazardType => hazardType;
    public GameObject HazardPrefab => hazardPrefab;
    public HazardSpawnPositionMode SpawnPositionMode => spawnPositionMode;
    public Vector3 SpawnOffset => spawnOffset;
    public BoulderChaseSettings BoulderChase => boulderChase;
    public FallingRocksSettings FallingRocks => fallingRocks;
}
