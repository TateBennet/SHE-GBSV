using UnityEngine;

public class BallServer : MonoBehaviour
{
    [Header("Required")]
    [Tooltip("Rigidbody prefab of your volleyball (must have SphereCollider + Rigidbody).")]
    public Rigidbody ballPrefab;

    [Tooltip("Where the toss starts from.")]
    public Transform servePoint;

    [Tooltip("Usually your Main Camera under XR Origin.")]
    public Transform playerHead;

    [Header("Spawn Behavior")]
    [Tooltip("If true, destroys the previous ball and spawns a brand new one each Serve(). If false, reuses the existing one if it still exists.")]
    public bool respawnEachServe = false;

    [Header("Toss Settings")]
    [Tooltip("Seconds for the ball to reach the target point.")]
    public float timeToTarget = 1.2f;

    [Tooltip("Offset (relative to the player's head) where the ball should arrive.")]
    public Vector3 targetOffset = new Vector3(0f, -0.2f, 0.6f);

    [Tooltip("Random horizontal spread (meters) to vary serves.")]
    public float horizontalJitter = 0.25f;

    [Header("Physics Defaults (applied to spawned ball)")]
    public bool useGravity = true;
    public RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate;
    public CollisionDetectionMode collisionMode = CollisionDetectionMode.Continuous;
    [Tooltip("Recommended ~0.3–0.5 for a volleyball.")]
    public float mass = 0.4f;

    [Header("Editor nicety")]
    public bool faceTargetWhenServing = true;

    const float GRAVITY = 9.81f;

    // Keep track of the current ball (optional)
    Rigidbody _currentBall;

    public void Serve()
    {
        if (!ballPrefab || !servePoint || !playerHead)
        {
            Debug.LogWarning("BallServerSpawner: Assign ballPrefab, servePoint, and playerHead.");
            return;
        }

        // Compute target near the player's head
        Vector3 target = playerHead.position
                       + playerHead.forward * targetOffset.z
                       + Vector3.up * targetOffset.y;

        if (horizontalJitter > 0f)
            target += playerHead.right * Random.Range(-horizontalJitter, horizontalJitter);

        if (faceTargetWhenServing)
            servePoint.LookAt(target);

        // Spawn or reuse a ball
        if (respawnEachServe)
        {
            if (_currentBall) Destroy(_currentBall.gameObject);
            _currentBall = Instantiate(ballPrefab, servePoint.position, Quaternion.identity);
        }
        else
        {
            if (!_currentBall)
                _currentBall = Instantiate(ballPrefab, servePoint.position, Quaternion.identity);
            else
                _currentBall.transform.SetPositionAndRotation(servePoint.position, Quaternion.identity);
        }

        // Ensure physics settings on the spawned ball
        var rb = _currentBall;
        rb.mass = mass;
        rb.isKinematic = false;
        rb.useGravity = useGravity;
        rb.interpolation = interpolation;
        rb.collisionDetectionMode = collisionMode;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Launch with ballistic velocity
        rb.linearVelocity = CalculateBallisticVelocity(servePoint.position, target, timeToTarget);
    }

    static Vector3 CalculateBallisticVelocity(Vector3 start, Vector3 end, float t)
    {
        Vector3 delta = end - start;
        Vector3 deltaXZ = new Vector3(delta.x, 0f, delta.z);
        float vy = (delta.y + 0.5f * GRAVITY * t * t) / t;
        Vector3 vxz = deltaXZ / t;
        return vxz + Vector3.up * vy;
    }

    void OnDrawGizmosSelected()
    {
        if (!servePoint || !playerHead) return;
        Gizmos.DrawLine(servePoint.position, playerHead.position);
    }
}