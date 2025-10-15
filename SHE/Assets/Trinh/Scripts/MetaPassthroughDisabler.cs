using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;

public class MetaPassthroughDisabler : MonoBehaviour
{
    private bool passthroughDisabled = false;

    void Start()
    {
        StartCoroutine(ForcePassthroughDisable());
    }

    private IEnumerator ForcePassthroughDisable()
    {
        yield return new WaitForSeconds(0.1f);

        DisableMetaPassthrough();
        yield return new WaitForSeconds(0.1f);
        ForceOpenXRTrackingReset();
        yield return new WaitForSeconds(0.1f);
        KillARSubsystems();

        passthroughDisabled = true;
        Debug.Log("✅ Meta Passthrough disabled + XR reset complete");
    }

    private void DisableMetaPassthrough()
    {
        // Meta XR SDK v78 - Disable OVRPassthroughLayer
        var passthroughLayers = Object.FindObjectsByType<OVRPassthroughLayer>(FindObjectsSortMode.None);
        foreach (var layer in passthroughLayers)
        {
            layer.enabled = false;
            if (layer != null)
            {
                layer.hidden = true;
                Debug.Log("Disabled OVRPassthroughLayer");
            }
        }

        // Disable OVRManager passthrough
        if (OVRManager.instance != null)
        {
            OVRManager.instance.isInsightPassthroughEnabled = false;
            Debug.Log("✅ OVRManager passthrough disabled");
        }

        // Reset OVR cameras
        var ovrCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
            .Where(cam => cam != null && (cam.name.Contains("OVRCamera") || cam.GetComponent<OVRCameraRig>() != null));

        foreach (var cam in ovrCameras)
        {
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                Debug.Log($"Reset OVR camera: {cam.name}");
            }
        }

        // Generic OVR component cleanup
        var ovrComponents = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(c => c != null && c.GetType().Name.StartsWith("OVR") &&
                       (c.GetType().Name.ToLower().Contains("pass") ||
                        c.GetType().Name.ToLower().Contains("insight")));

        foreach (var comp in ovrComponents)
        {
            if (comp != null)
            {
                comp.enabled = false;
                Debug.Log($"Disabled OVR component: {comp.GetType().Name}");
            }
        }
    }

    private void ForceOpenXRTrackingReset()
    {
        var inputSubsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(inputSubsystems);

        foreach (var subsystem in inputSubsystems)
        {
            if (subsystem != null && subsystem.running)
            {
                subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
                subsystem.TryRecenter();
                Debug.Log($"Reset {subsystem.GetType().Name} to Floor tracking");
            }
        }
    }

    private void KillARSubsystems()
    {
        // AR Foundation cleanup - Unity 6 safe version
        var arSession = Object.FindFirstObjectByType<ARSession>();
        if (arSession != null)
        {
            arSession.enabled = false;
            if (arSession.gameObject != null)
                DestroyImmediate(arSession.gameObject);
            Debug.Log("Destroyed ARSession");
        }

        // Find all ARSessions
        var allARSessions = Object.FindObjectsByType<ARSession>(FindObjectsSortMode.None);
        foreach (var session in allARSessions)
        {
            if (session != null)
            {
                session.enabled = false;
                if (session.gameObject != null)
                    DestroyImmediate(session.gameObject);
            }
        }

        // AR Camera Background cleanup
        var arBackgrounds = Object.FindObjectsByType<ARCameraBackground>(FindObjectsSortMode.None);
        foreach (var bg in arBackgrounds)
        {
            if (bg != null)
            {
                bg.enabled = false;
                if (bg.gameObject != null)
                    DestroyImmediate(bg.gameObject);
                Debug.Log("Destroyed ARCameraBackground");
            }
        }

        // Kill AR trackables and managers - null-safe
        var arComponents = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(c => c != null &&
                       c.GetType().Name.StartsWith("AR") &&
                       (c.GetType().Name.Contains("Trackable") ||
                        c.GetType().Name.Contains("Manager") ||
                        c.GetType().Name.Contains("Plane") ||
                        c.GetType().Name.Contains("Anchor")));

        foreach (var arComp in arComponents)
        {
            if (arComp != null)
            {
                arComp.enabled = false;
                if (arComp.gameObject != null)
                    DestroyImmediate(arComp.gameObject);
                Debug.Log($"Destroyed AR component: {arComp.GetType().Name}");
            }
        }
    }

    void Update()
    {
        if (OVRManager.instance != null && OVRManager.instance.isInsightPassthroughEnabled && passthroughDisabled)
        {
            Debug.LogWarning("❌ Passthrough reactivated! Force disabling...");
            StartCoroutine(ForcePassthroughDisable());
            passthroughDisabled = false;
        }
    }

    [ContextMenu("Force Passthrough Disable")]
    private void DebugForceDisable()
    {
        StartCoroutine(ForcePassthroughDisable());
    }
}
