using UnityEngine;
using Unity.XR.CoreUtils;              // XROrigin
using UnityEngine.SpatialTracking;     // TrackedPoseDriver
using UnityEngine.XR.ARFoundation;     // AR* managers

public class XRNonARSceneFix : MonoBehaviour
{
    [Tooltip("Set true to make head tracking rotation-only (no positional movement).")]
    public bool rotationOnly = true;

    [Tooltip("Recenters the rig at scene start for comfort.")]
    public bool recenterOnStart = true;

    private XROrigin xrOrigin;
    private Camera xrCam;

    void Awake()
    {
        xrOrigin = FindOne<XROrigin>();
        xrCam = (xrOrigin && xrOrigin.Camera) ? xrOrigin.Camera : Camera.main;

        // 1) Disable any stray AR components (safe even if missing)
        var arSession = FindOne<ARSession>(); if (arSession) arSession.enabled = false;
        var planeMgr = FindOne<ARPlaneManager>(); if (planeMgr) planeMgr.enabled = false;
        var raycastMgr = FindOne<ARRaycastManager>(); if (raycastMgr) raycastMgr.enabled = false;
        var pointCloudMgr = FindOne<ARPointCloudManager>(); if (pointCloudMgr) pointCloudMgr.enabled = false;
        var anchorMgr = FindOne<ARAnchorManager>(); if (anchorMgr) anchorMgr.enabled = false;

        var arCamBg = xrCam ? xrCam.GetComponent<ARCameraBackground>() : null; if (arCamBg) arCamBg.enabled = false;
        var arCamMgr = xrCam ? xrCam.GetComponent<ARCameraManager>() : null; if (arCamMgr) arCamMgr.enabled = false;

        // 2) Force non-AR (device/head) origin — ***use XROrigin enum, not XR.TrackingOriginModeFlags***
        if (xrOrigin)
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;

        // 3) Ensure a single pose driver, set tracking type
        if (xrCam)
        {
            var tpd = xrCam.GetComponent<TrackedPoseDriver>();
            if (!tpd) tpd = xrCam.gameObject.AddComponent<TrackedPoseDriver>();

            tpd.SetPoseSource(
                TrackedPoseDriver.DeviceType.GenericXRDevice,
                TrackedPoseDriver.TrackedPose.Center
            );
            tpd.trackingType = rotationOnly
                ? TrackedPoseDriver.TrackingType.RotationOnly
                : TrackedPoseDriver.TrackingType.RotationAndPosition;

            // Make sure any AR Pose Driver isn’t driving the camera in non-AR scenes
            var arPoseDriver = xrCam.GetComponent("ARPoseDriver") as Behaviour;
            if (arPoseDriver) arPoseDriver.enabled = false;
        }
    }

    void Start()
    {
        // 4) Recenter for comfort after systems initialize
        if (recenterOnStart && xrOrigin && xrCam)
        {
            xrOrigin.MoveCameraToWorldLocation(Vector3.zero);
            xrOrigin.MatchOriginUpCameraForward(Vector3.up, xrCam.transform.forward);
        }
    }

    // Unity 6-safe helper for finding one object
    static T FindOne<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
