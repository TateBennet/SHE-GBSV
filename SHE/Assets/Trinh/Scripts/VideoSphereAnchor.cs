using UnityEngine;

public class VideoSphereAnchor : MonoBehaviour
{
    [Tooltip("Optional yaw (degrees) to rotate the sphere at start so the user faces the intended part of the 360.")]
    public float initialYawDegrees = 0f;

    [Tooltip("Radius/scale of the sphere. Keep large (e.g., 100) so head is near the center.")]
    public float sphereScale = 100f;

    Transform _cam;

    void Start()
    {
        // 1) Find camera
        var main = Camera.main;
        _cam = main ? main.transform : null;

        // 2) Make sure we are NOT parented under the rig/camera
        transform.SetParent(null, worldPositionStays: true);

        // 3) Put sphere at camera position once
        if (_cam) transform.position = _cam.position;

        // 4) Lock sphere rotation to world, apply only an optional initial yaw
        var e = transform.eulerAngles;
        e.x = 0f; e.z = 0f;
        e.y = initialYawDegrees;
        transform.eulerAngles = e;

        // 5) Make sure the sphere is big enough and stays “inside-out”
        transform.localScale = Vector3.one * sphereScale;
    }

    void LateUpdate()
    {
        // Follow camera position every frame (no rotation copy!)
        if (!_cam)
        {
            var main = Camera.main;
            _cam = main ? main.transform : null;
        }
        if (_cam) transform.position = _cam.position;
    }
}
