using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerEnvironmentContext : MonoBehaviour
{
    [SerializeField] private Transform samplePoint;

    private readonly HashSet<EnvironmentType> sampledEnvironments = new HashSet<EnvironmentType>();
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
    }

    public bool IsInEnvironment(EnvironmentType environmentType)
    {
        return sampledEnvironments.Contains(environmentType);
    }

    public bool HasRule(RuleTag ruleTag)
    {
        return (sampledRuleTags & ruleTag) == ruleTag;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = samplePoint != null ? samplePoint.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(origin, 0.06f);
    }
}
