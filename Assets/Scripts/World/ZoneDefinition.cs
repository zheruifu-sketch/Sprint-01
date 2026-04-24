using UnityEngine.Serialization;
using UnityEngine;

[DisallowMultipleComponent]
public class ZoneDefinition : MonoBehaviour
{
    [Header("Semantic")]
    [FormerlySerializedAs("zoneType")]
    [SerializeField] private EnvironmentType environmentType = EnvironmentType.None;
    [FormerlySerializedAs("ruleTags")]
    [SerializeField] private RuleTag additionalRuleTags = RuleTag.None;

    public EnvironmentType EnvironmentType => environmentType;
    public RuleTag RuleTags => WorldSemanticUtility.GetDefaultRuleTags(environmentType) | additionalRuleTags;

    private void Reset()
    {
        Collider2D zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }
}
