using UnityEngine;

[DisallowMultipleComponent]
public class BreakableRoadBlockColliderRelay : MonoBehaviour
{
    private BreakableRoadBlock owner;

    public void SetOwner(BreakableRoadBlock block)
    {
        owner = block;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        owner?.HandleRelayCollision(
            collision != null ? collision.collider : null,
            collision != null ? Mathf.Abs(collision.relativeVelocity.x) : -1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        owner?.HandleRelayCollision(other, -1f);
    }
}
