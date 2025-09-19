using UnityEngine;

public class PhoneFloat : MonoBehaviour
{
    [Header("Float Settings")]
    [Tooltip("Vertical float amplitude in meters.")]
    public float floatHeight = 0.05f;

    [Tooltip("How fast it floats up and down.")]
    public float floatSpeed = 2f;

    [Tooltip("Optional small rotation tilt amplitude (degrees).")]
    public float tiltAngle = 5f;

    [Tooltip("How fast it tilts.")]
    public float tiltSpeed = 1.5f;

    private Vector3 startPos;
    private Quaternion startRot;

    void Start()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;
    }

    void Update()
    {
        float t = Time.time;

        // Float up/down
        float yOffset = Mathf.Sin(t * floatSpeed) * floatHeight;
        transform.localPosition = startPos + new Vector3(0, yOffset, 0);

        // Gentle tilt
        if (tiltAngle > 0f)
        {
            float tilt = Mathf.Sin(t * tiltSpeed) * tiltAngle;
            transform.localRotation = startRot * Quaternion.Euler(0, 0, tilt);
        }
    }
}