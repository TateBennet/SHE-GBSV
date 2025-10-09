using UnityEngine;
using Unity.XR.CoreUtils;

public class VideoSphereAnchor : MonoBehaviour
{
    [Tooltip("Leave OFF to keep the sphere’s own rotation (recommended).")]
    public bool matchCameraRotation = false;

    private Transform cam;

    void Start()
    {
        var xro = FindOne<XROrigin>();
        cam = xro && xro.Camera ? xro.Camera.transform : Camera.main?.transform;
        if (!cam) Debug.LogWarning("VideoSphereAnchor: No XR camera found.");
    }

    void LateUpdate()
    {
        if (!cam) return;

        // Follow camera POSITION (kill parallax), keep sphere’s rotation unless requested
        transform.position = cam.position;
        if (matchCameraRotation) transform.rotation = cam.rotation;
    }

    static T FindOne<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
