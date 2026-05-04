using System.Collections.Generic;
using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class PlayerEnvironmentContext : MonoBehaviour
{
    [LabelText("环境采样点")]
    [SerializeField] private Transform samplePoint;

    private readonly HashSet<EnvironmentType> sampledEnvironments = new HashSet<EnvironmentType>();
    private readonly HashSet<EnvironmentType> previewEnvironments = new HashSet<EnvironmentType>();
    private readonly Collider2D[] results = new Collider2D[16];
    private RuleTag sampledRuleTags;

    private void Reset()
    {
        samplePoint = transform;
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        sampledEnvironments.Clear();
        previewEnvironments.Clear();
        sampledRuleTags = RuleTag.None;

        Vector2 origin = samplePoint != null ? samplePoint.position : transform.position;
        int hitCount = Physics2D.OverlapPointNonAlloc(origin, results);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = results[i];
            if (hit == null)
            {
                continue;
            }

            if (WorldSemanticUtility.TryResolveEnvironment(hit, out EnvironmentType environmentType) &&
                environmentType != EnvironmentType.None)
            {
                sampledEnvironments.Add(environmentType);
            }

            sampledRuleTags |= WorldSemanticUtility.ResolveRuleTags(hit);
        }

        RefreshPreviewEnvironments(origin);
    }

    public bool IsInEnvironment(EnvironmentType environmentType)
    {
        return sampledEnvironments.Contains(environmentType);
    }

    public bool IsInOrPreviewingEnvironment(EnvironmentType environmentType)
    {
        return sampledEnvironments.Contains(environmentType) || previewEnvironments.Contains(environmentType);
    }

    public bool IsPreviewingEnvironment(EnvironmentType environmentType)
    {
        return previewEnvironments.Contains(environmentType);
    }

    public bool HasRule(RuleTag ruleTag)
    {
        return (sampledRuleTags & ruleTag) == ruleTag;
    }

    private void RefreshPreviewEnvironments(Vector2 origin)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            origin,
            GameConstants.DefaultEnvironmentPreviewForwardDistance + GameConstants.DefaultEnvironmentPreviewVerticalTolerance,
            results);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = results[i];
            if (hit == null)
            {
                continue;
            }

            if (!WorldSemanticUtility.TryResolveEnvironment(hit, out EnvironmentType environmentType) ||
                environmentType == EnvironmentType.None)
            {
                continue;
            }

            if (sampledEnvironments.Contains(environmentType))
            {
                previewEnvironments.Add(environmentType);
                continue;
            }

            if (IsWithinPreviewRange(origin, hit))
            {
                previewEnvironments.Add(environmentType);
            }
        }
    }

    private static bool IsWithinPreviewRange(Vector2 origin, Collider2D collider)
    {
        Vector2 closestPoint = collider.ClosestPoint(origin);
        float horizontalDelta = closestPoint.x - origin.x;
        float verticalDelta = Mathf.Abs(closestPoint.y - origin.y);

        Bounds bounds = collider.bounds;
        float expandPercent = GameConstants.DefaultEnvironmentPreviewBoundsExpandPercent;
        float horizontalExpand = bounds.size.x * expandPercent;
        float verticalExpand = bounds.size.y * expandPercent;

        float backwardTolerance = GameConstants.DefaultEnvironmentPreviewBackwardTolerance + horizontalExpand;
        float forwardTolerance = GameConstants.DefaultEnvironmentPreviewForwardDistance + horizontalExpand;
        float verticalTolerance = GameConstants.DefaultEnvironmentPreviewVerticalTolerance + verticalExpand;

        return horizontalDelta >= -backwardTolerance
               && horizontalDelta <= forwardTolerance
               && verticalDelta <= verticalTolerance;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = samplePoint != null ? samplePoint.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(origin, 0.06f);
    }
}
