using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;
#if UNITY_2023_1_OR_NEWER
using UnityEngine.SceneManagement;
#endif

public class XRPostLoadSanitizer : MonoBehaviour
{
    // Auto-runs after each scene load (no need to place in a scene)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        var go = new GameObject("XRPostLoadSanitizer");
        go.hideFlags = HideFlags.DontSave;
        go.AddComponent<XRPostLoadSanitizer>();
    }

    private void Start()
    {
        if (!XRSceneMode.NextSceneIsNonAR)
        {
            Destroy(gameObject);
            return;
        }

        // --- 1) Disable any leftover AR systems ---
        DisableIfFound<ARSession>();
        DisableIfFound<ARCameraBackground>();
        DisableIfFound<ARCameraManager>();
        DisableIfFound<ARPlaneManager>();
        DisableIfFound<ARPointCloudManager>();
        DisableIfFound<ARFaceManager>();
        DisableIfFound<ARHumanBodyManager>();
        DisableIfFound<AROcclusionManager>();
        DisableIfFound<ARRaycastManager>();
        DisableIfFound<AREnvironmentProbeManager>();
        DisableIfFound<ARAnchorManager>();

        var origin = FindFirst<XROrigin>();
        if (origin)
        {
            origin.enabled = false;
            origin.gameObject.SetActive(false);
        }

        // --- 2) Force camera to Rotation-Only tracking (prevent “sphere glued to head”) ---
        var cam = Camera.main;
        if (cam)
        {
            // Remove deprecated ARPoseDriver if present (avoid hard ref to obsolete type)
            var maybeArPose = cam.GetComponent("UnityEngine.XR.ARFoundation.ARPoseDriver") as MonoBehaviour;
            if (maybeArPose) Destroy(maybeArPose);

#if ENABLE_INPUT_SYSTEM
            // Use the Input System’s TrackedPoseDriver
            var tpdIS = cam.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            if (!tpdIS) tpdIS = cam.gameObject.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            tpdIS.trackingType = UnityEngine.InputSystem.XR.TrackedPoseDriver.TrackingType.RotationOnly;
#else
            // Fallback to legacy XR TrackedPoseDriver
            var tpdLegacy = cam.GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
            if (!tpdLegacy) tpdLegacy = cam.gameObject.AddComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
            tpdLegacy.trackingType = UnityEngine.SpatialTracking.TrackedPoseDriver.TrackingType.RotationOnly;
#endif
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
        }

        XRSceneMode.NextSceneIsNonAR = false;
        Destroy(gameObject);
    }

    // ---------- Helpers ----------
    private static T FindFirst<T>() where T : Behaviour
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<T>();
#endif
    }

    private static void DisableIfFound<T>() where T : Behaviour
    {
        var comp = FindFirst<T>();
        if (!comp) return;
        comp.enabled = false;

        if (comp is ARSession s)
        {
            try
            {
                var sub = s.subsystem;
                if (sub != null && sub.running) sub.Stop();
                s.Reset();
            }
            catch { }
        }
    }
}
