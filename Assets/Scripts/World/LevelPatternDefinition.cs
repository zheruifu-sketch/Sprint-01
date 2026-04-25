using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelPatternDefinition", menuName = "JumpGame/Level Pattern Definition")]
public class LevelPatternDefinition : ScriptableObject
{
    [Serializable]
    public class EnvironmentGenerationRule
    {
        [SerializeField] private EnvironmentType environmentType = EnvironmentType.None;
        [SerializeField] private float weight = 1f;
        [SerializeField] private int minConsecutiveCount = 1;
        [SerializeField] private int maxConsecutiveCount = 2;
        [SerializeField] private bool canBeFirstRandomSegment = true;
        [SerializeField] private List<EnvironmentType> allowedPreviousEnvironments = new List<EnvironmentType>();

        public EnvironmentGenerationRule()
        {
        }

        public EnvironmentGenerationRule(
            EnvironmentType environmentType,
            float weight,
            int minConsecutiveCount,
            int maxConsecutiveCount,
            bool canBeFirstRandomSegment,
            List<EnvironmentType> allowedPreviousEnvironments)
        {
            this.environmentType = environmentType;
            this.weight = weight;
            this.minConsecutiveCount = minConsecutiveCount;
            this.maxConsecutiveCount = maxConsecutiveCount;
            this.canBeFirstRandomSegment = canBeFirstRandomSegment;
            if (allowedPreviousEnvironments != null)
            {
                this.allowedPreviousEnvironments = new List<EnvironmentType>(allowedPreviousEnvironments);
            }
        }

        public EnvironmentType EnvironmentType => environmentType;
        public float Weight => Mathf.Max(0.01f, weight);
        public int MinConsecutiveCount => Mathf.Max(1, minConsecutiveCount);
        public int MaxConsecutiveCount => Mathf.Max(MinConsecutiveCount, maxConsecutiveCount);
        public bool CanBeFirstRandomSegment => canBeFirstRandomSegment;
        public List<EnvironmentType> AllowedPreviousEnvironments => allowedPreviousEnvironments;
    }

    [SerializeField] private int openingRoadRepeatCount = 3;
    [SerializeField] private List<EnvironmentGenerationRule> environmentRules = new List<EnvironmentGenerationRule>();

    public int OpeningRoadRepeatCount => Mathf.Max(1, openingRoadRepeatCount);
    public List<EnvironmentGenerationRule> EnvironmentRules => environmentRules;
}
