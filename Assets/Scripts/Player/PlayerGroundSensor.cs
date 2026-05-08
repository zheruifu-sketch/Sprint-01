using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

public class PlayerGroundSensor : MonoBehaviour
{
    [LabelText("地面检测点")]
    [SerializeField] private Transform groundCheckPoint;
    [LabelText("玩家主碰撞体")]
    [SerializeField] private Collider2D playerCollider;
    [LabelText("地面检测层")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [LabelText("脚底接触容差")]
    [SerializeField] private float footContactTolerance = 0.12f;
    [LabelText("脚底向下补偿检测距离")]
    [SerializeField] private float groundProbeDistance = 0.8f;
    [LabelText("最小地面法线Y")]
    [SerializeField] private float minGroundNormalY = 0.35f;

    public bool IsGrounded { get; private set; }

    private Rigidbody2D playerRigidbody;
    private readonly ContactPoint2D[] contactResults = new ContactPoint2D[16];
    private readonly RaycastHit2D[] castResults = new RaycastHit2D[16];
    private ContactFilter2D groundContactFilter;

    private void Reset()
    {
        groundCheckPoint = transform;
        CacheReferences();
    }

    private void Awake()
    {
        CacheReferences();
    }

    public void Refresh()
    {
        if (playerRigidbody == null)
        {
            CacheReferences();
        }

        if (playerRigidbody == null)
        {
            IsGrounded = false;
            return;
        }

        Vector2 footPoint = groundCheckPoint != null ? groundCheckPoint.position : transform.position;
        float maxFootY = footPoint.y + Mathf.Max(0f, footContactTolerance);
        // Use real physics contacts instead of overlap probes so side brushes and
        // body-inside-trigger situations do not fake a "landed" state.
        int contactCount = playerRigidbody.GetContacts(groundContactFilter, contactResults);
        IsGrounded = false;

        for (int i = 0; i < contactCount; i++)
        {
            ContactPoint2D contact = contactResults[i];
            if (contact.collider == null)
            {
                continue;
            }

            if (contact.normal.y < minGroundNormalY)
            {
                continue;
            }

            if (contact.point.y > maxFootY)
            {
                continue;
            }

            IsGrounded = true;
            return;
        }

        if (playerCollider == null)
        {
            IsGrounded = false;
            return;
        }

        float probeDistance = Mathf.Max(0f, groundProbeDistance);
        if (probeDistance <= 0f)
        {
            IsGrounded = false;
            return;
        }

        // Moving or sinking platforms can momentarily open a tiny gap between the
        // player's feet and the platform collider. Cast the player's own collider a
        // short distance downward so those micro-gaps still count as jumpable ground.
        int castCount = playerCollider.Cast(Vector2.down, groundContactFilter, castResults, probeDistance);
        for (int i = 0; i < castCount; i++)
        {
            RaycastHit2D hit = castResults[i];
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.normal.y < minGroundNormalY)
            {
                continue;
            }

            if (hit.centroid.y > maxFootY + probeDistance)
            {
                continue;
            }

            IsGrounded = true;
            return;
        }
    }

    private void CacheReferences()
    {
        playerRigidbody = GetComponentInParent<Rigidbody2D>();
        playerCollider = playerCollider != null ? playerCollider : GetComponentInParent<Collider2D>();
        groundContactFilter = new ContactFilter2D();
        groundContactFilter.useLayerMask = true;
        groundContactFilter.layerMask = groundLayers;
        groundContactFilter.useTriggers = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = groundCheckPoint != null ? groundCheckPoint.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, Mathf.Max(0.01f, footContactTolerance));
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + Vector3.down * Mathf.Max(0f, groundProbeDistance));
    }
}
