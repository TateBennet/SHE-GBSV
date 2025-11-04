using UnityEngine;
using Unity.XR.CoreUtils; // for XROrigin

// Run early so cameras are cleaned up before rendering
[DefaultExecutionOrder(-500)]
public class ForceSingleXRCamera : MonoBehaviour
{
    void Awake()
    {
        // 1. Try to get the scene's main/XR camera
        Camera targetCam = Camera.main;

        if (targetCam == null)
        {
            // fallback: try XR Origin
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin != null)
                targetCam = origin.Camera;
        }

        // 2. If we still don't have a camera, bail quietly
        if (targetCam == null)
        {
            Debug.LogWarning("[ForceSingleXRCamera] No camera found to keep enabled.");
            return;
        }

        // 3. Disable every other camera in the scene
        var allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCams)
        {
            if (cam == targetCam)
                continue; // keep the one we want

            cam.enabled = false;
        }

        // 4. Make 100% sure our target is on
        targetCam.enabled = true;
    }
}
