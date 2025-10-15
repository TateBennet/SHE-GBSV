using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

public class XRTrackingResetManager : MonoBehaviour
{
    public bool hasResetTracking = false;

    void Start()
    {
        StartCoroutine(ForceXRTrackingReset());
    }

    private IEnumerator ForceXRTrackingReset()
    {
        // Wait 2 frames for XR system to initialize
        yield return null;
        yield return null;

        // Step 1: Force tracking origin to Floor
        ForceTrackingOriginFloor();

        // Step 2: Multiple recenter attempts
        for (int i = 0; i < 3; i++)
        {
            TryRecenterXR();
            yield return new WaitForSeconds(0.1f);
        }

        // Step 3: Reset camera transform
        ResetCameraTransform();

        // Step 4: Verify tracking origin
        VerifyTrackingOrigin();

        hasResetTracking = true;
        Debug.Log("✅ XR Tracking Reset Complete - Origin: Floor");
    }

    private void ForceTrackingOriginFloor()
    {
        // Unity 6: Use static SubsystemManager.GetSubsystems method
        var inputSubsystems = new List<XRInputSubsystem>();
        UnityEngine.SubsystemManager.GetSubsystems(inputSubsystems);

        foreach (var subsystem in inputSubsystems)
        {
            if (subsystem.running)
            {
                // Try to set tracking origin to Floor
                if (subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor))
                {
                    Debug.Log("✅ Set InputSubsystem tracking origin to Floor");
                }
                else
                {
                    Debug.LogWarning("Failed to set tracking origin to Floor");
                }

                // Force recenter
                subsystem.TryRecenter();
            }
        }

        // Additional XRGeneralSettings check
        if (XRGeneralSettings.Instance != null)
        {
            var xrManager = XRGeneralSettings.Instance.Manager;
            if (xrManager != null && xrManager.activeLoader != null)
            {
                Debug.Log("XR loader active, tracking reset initiated");
            }
        }
    }

    private void TryRecenterXR()
    {
        var inputSubsystems = new List<XRInputSubsystem>();
        UnityEngine.SubsystemManager.GetSubsystems(inputSubsystems);

        foreach (var subsystem in inputSubsystems)
        {
            if (subsystem.running)
            {
                subsystem.TryRecenter();
            }
        }
    }

    private void ResetCameraTransform()
    {
        // AVOID XROrigin reference - use generic camera reset
        // This bypasses the assembly reference issue
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            // Find XROrigin parent and reset local transforms
            Transform xrParent = FindXRParent(mainCam.transform);
            if (xrParent != null)
            {
                mainCam.transform.SetParent(xrParent);
                mainCam.transform.localPosition = Vector3.zero;
                mainCam.transform.localRotation = Quaternion.identity;
                xrParent.localPosition = Vector3.zero;
                xrParent.localRotation = Quaternion.identity;
                Debug.Log("Reset camera within XR parent hierarchy");
            }
            else
            {
                // Fallback: World space reset
                mainCam.transform.SetParent(null);
                mainCam.transform.position = Vector3.zero;
                mainCam.transform.rotation = Quaternion.identity;
                Debug.Log("Reset camera to world origin");
            }
        }

        // Reset all XR-related transforms in scene
        var xrTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t.name.ToLower().Contains("xr") || t.name.ToLower().Contains("origin"));

        foreach (var xrTransform in xrTransforms)
        {
            xrTransform.localPosition = Vector3.zero;
            xrTransform.localRotation = Quaternion.identity;
        }
    }

    private Transform FindXRParent(Transform child)
    {
        Transform current = child.parent;
        while (current != null)
        {
            if (current.name.ToLower().Contains("xr") || current.name.ToLower().Contains("origin"))
                return current;
            current = current.parent;
        }
        return null;
    }

    private void VerifyTrackingOrigin()
    {
        var inputSubsystems = new List<XRInputSubsystem>();
        UnityEngine.SubsystemManager.GetSubsystems(inputSubsystems);

        foreach (var subsystem in inputSubsystems)
        {
            if (subsystem.running)
            {
                var originMode = subsystem.GetTrackingOriginMode();
                Debug.Log($"XR Subsystem tracking origin: {originMode}");

                if (originMode == TrackingOriginModeFlags.Floor)
                {
                    Debug.Log("✅ Tracking origin verified as Floor");
                }
                else
                {
                    Debug.LogWarning($"❌ Tracking origin is {originMode}, expected Floor");
                }
            }
        }
    }

    void Update()
    {
        if (!hasResetTracking) return;

        var inputSubsystems = new List<XRInputSubsystem>();
        UnityEngine.SubsystemManager.GetSubsystems(inputSubsystems);

        bool originCorrect = true;
        foreach (var subsystem in inputSubsystems)
        {
            if (subsystem.running && subsystem.GetTrackingOriginMode() != TrackingOriginModeFlags.Floor)
            {
                originCorrect = false;
                break;
            }
        }

        if (!originCorrect)
        {
            Debug.LogWarning("Tracking origin corrupted! Forcing reset...");
            hasResetTracking = false;
            StartCoroutine(ForceXRTrackingReset());
        }
    }

    // Debug method
    [ContextMenu("Debug Tracking State")]
    private void DebugTrackingState()
    {
        var subsystems = new List<XRInputSubsystem>();
        UnityEngine.SubsystemManager.GetSubsystems(subsystems);

        foreach (var sub in subsystems)
        {
            Debug.Log($"Subsystem: {sub.GetType().Name}, Running: {sub.running}, Origin: {sub.GetTrackingOriginMode()}");
        }
    }
}
