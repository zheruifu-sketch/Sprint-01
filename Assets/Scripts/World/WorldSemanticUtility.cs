using UnityEngine;

public static class WorldSemanticUtility
{
    public static bool HasEnvironment(Collider2D collider, EnvironmentType environmentType)
    {
        return TryResolveEnvironment(collider, out EnvironmentType resolved) && resolved == environmentType;
    }

    public static bool TryResolveEnvironment(Collider2D collider, out EnvironmentType environmentType)
    {
        if (collider == null)
        {
            environmentType = EnvironmentType.None;
            return false;
        }

        ZoneDefinition zoneDefinition = collider.GetComponent<ZoneDefinition>();
        if (zoneDefinition == null)
        {
            zoneDefinition = collider.GetComponentInParent<ZoneDefinition>();
        }

        if (zoneDefinition != null && zoneDefinition.EnvironmentType != EnvironmentType.None)
        {
            environmentType = zoneDefinition.EnvironmentType;
            return true;
        }

        SegmentDescriptor segmentDescriptor = collider.GetComponentInParent<SegmentDescriptor>();
        if (segmentDescriptor != null && segmentDescriptor.PrimaryEnvironment != EnvironmentType.None)
        {
            environmentType = segmentDescriptor.PrimaryEnvironment;
            return true;
        }

        environmentType = ResolveEnvironmentFromTag(collider.tag);
        return environmentType != EnvironmentType.None;
    }

    public static RuleTag ResolveRuleTags(Collider2D collider)
    {
        if (collider == null)
        {
            return RuleTag.None;
        }

        ZoneDefinition zoneDefinition = collider.GetComponent<ZoneDefinition>();
        if (zoneDefinition == null)
        {
            zoneDefinition = collider.GetComponentInParent<ZoneDefinition>();
        }

        if (zoneDefinition != null)
        {
            return zoneDefinition.RuleTags;
        }

        if (!TryResolveEnvironment(collider, out EnvironmentType environmentType))
        {
            return RuleTag.None;
        }

        return GetDefaultRuleTags(environmentType);
    }

    public static RuleTag GetDefaultRuleTags(EnvironmentType environmentType)
    {
        return environmentType switch
        {
            EnvironmentType.Road => RuleTag.SupportsGroundedTravel,
            EnvironmentType.Water => RuleTag.SupportsBoat | RuleTag.InstantWaterDeath,
            EnvironmentType.Cliff => RuleTag.CliffDrop | RuleTag.BlocksBoat,
            EnvironmentType.Blizzard => RuleTag.SlowsHuman | RuleTag.BlocksPlane | RuleTag.SupportsBoat,
            EnvironmentType.Obstacle => RuleTag.Obstacle | RuleTag.HazardDamage,
            _ => RuleTag.None
        };
    }

    public static EnvironmentType ResolveEnvironmentFromTag(string tag)
    {
        return tag switch
        {
            "Road" => EnvironmentType.Road,
            "Water" => EnvironmentType.Water,
            "Cliff" => EnvironmentType.Cliff,
            "Blizzard" => EnvironmentType.Blizzard,
            "Obstacle" => EnvironmentType.Obstacle,
            _ => EnvironmentType.None
        };
    }
}
