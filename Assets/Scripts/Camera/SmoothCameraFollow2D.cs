using UnityEngine;

public class SmoothCameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;
    [SerializeField] private bool useRigidbodyPrediction = true;

    private Vector3 velocity;
    private Rigidbody2D targetRigidbody;

    private void Awake()
    {
        CacheTargetRigidbody();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (targetRigidbody == null)
        {
            CacheTargetRigidbody();
        }

        Vector3 currentPosition = transform.position;
        Vector3 desiredPosition = GetDesiredPosition(currentPosition);

        if (smoothTime <= 0.0001f)
        {
            transform.position = desiredPosition;
            velocity = Vector3.zero;
            return;
        }

        transform.position = Vector3.SmoothDamp(currentPosition, desiredPosition, ref velocity, smoothTime);
    }

    private Vector3 GetDesiredPosition(Vector3 currentPosition)
    {
        Vector2 targetPosition2D = GetTargetPosition();
        Vector3 desiredPosition = new Vector3(targetPosition2D.x, targetPosition2D.y, 0f) + offset;

        if (!followX)
        {
            desiredPosition.x = currentPosition.x;
        }

        if (!followY)
        {
            desiredPosition.y = currentPosition.y;
        }

        desiredPosition.z = offset.z;
        return desiredPosition;
    }

    private Vector2 GetTargetPosition()
    {
        if (!ShouldUseRigidbodyPrediction())
        {
            return target.position;
        }

        float predictionTime = Mathf.Clamp(Time.time - Time.fixedTime, 0f, Time.fixedDeltaTime);
        return targetRigidbody.position + targetRigidbody.velocity * predictionTime;
    }

    private bool ShouldUseRigidbodyPrediction()
    {
        if (!useRigidbodyPrediction || targetRigidbody == null)
        {
            return false;
        }

        // Interpolated rigidbodies already render between physics steps.
        // Adding camera-side prediction on top of that tends to create visible overshoot/jitter.
        return targetRigidbody.interpolation == RigidbodyInterpolation2D.None;
    }

    private void CacheTargetRigidbody()
    {
        targetRigidbody = target != null ? target.GetComponent<Rigidbody2D>() : null;
    }
}
