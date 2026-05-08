using UnityEngine;

[DisallowMultipleComponent]
public class LavaRoadTriggerRelay : MonoBehaviour
{
    private LavaRoadSegment owner;

    public void SetOwner(LavaRoadSegment segment)
    {
        owner = segment;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        owner?.HandleTriggerEnter(other);
    }
}
