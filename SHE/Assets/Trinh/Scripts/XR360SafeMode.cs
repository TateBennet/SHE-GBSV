using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;

public class XR360SafeMode : MonoBehaviour
{
    [Header("Assign (optional)")]
    [Tooltip("Your main camera. If left empty, Camera.main is used.")]
    public Camera targetCamera;

    [Tooltip("Your 360 video sphere root. If set, will be unparented and pinned to world space (not head).")]
    public Transform videoSphere;

    [Header("Behavior")]
    [Tooltip("Zero out camera local position every LateUpdate to enforce rotation-only.")]
    public bool zeroCameraPosEachFrame = true;

    [Tooltip("Force TrackedPoseDriver to RotationOnly (Input System if present, else legacy).")]
    public bool forceRotationOnlyDriver = true;

    [Tooltip("Disable any AR Foundation components that might have leaked in.")]
    public bool nukeARComponents = true;

    [Tooltip("Recenter XR after scene load (helps clear floor/device offsets).")]
    public bool recenterOnStart = true;

    [Header("Status Overlay")]
    public bool showOnScreenStatus = true;
    [Range(1, 6)] public int overlayLines = 3;
    string _status = "";
    float _statusUntil = 0f;

    void Awake()
    {
        if (!targetCamera) targetCamera = Camera.main;

        if (nukeARComponents) NukeARFoundationEverywhere();
        ForceXRToRotationOnly();

        if (videoSphere)
        {
            if (videoSphere.parent != null) videoSphere.SetParent(null, true);
            // Keep authored pose; if you need hard origin, uncomment:
            // videoSphere.position = Vector3.zero;
            // videoSphere.rotation = Quaternion.identity;
        }

        Note("360Safe: Awake");
    }

    System.Collections.IEnumerator Start()
    {
        // Let subsystems settle one frame then recenter if desired
        yield return null;
        if (recenterOnStart) { SafeRecenterXR(); Note("360Safe: Recentered"); }
    }

    void LateUpdate()
    {
        if (!targetCamera) return;

        if (zeroCameraPosEachFrame)
        {
            var t = targetCamera.transform;
            t.localPosition = Vector3.zero; // crush positional drift every frame
        }
    }

    // ------------------ Core Fixes ------------------

    void ForceXRToRotationOnly()
    {
        if (!forceRotationOnlyDriver || !targetCamera) return;

        // Remove deprecated ARPoseDriver if present (by name to avoid hard ref)
        var maybeArPose = targetCamera.GetComponent("UnityEngine.XR.ARFoundation.ARPoseDriver") as MonoBehaviour;
        if (maybeArPose) Destroy(maybeArPose);

        // Prefer Input System’s TrackedPoseDriver (if that package is present)
        var tpdIS = targetCamera.GetComponent("UnityEngine.InputSystem.XR.TrackedPoseDriver") as MonoBehaviour;
        if (tpdIS != null)
        {
            var tpType = tpdIS.GetType();
            var prop = tpType.GetProperty("trackingType");
            if (prop != null)
            {
                // enum value 1 == RotationOnly for InputSystem.XR.TrackedPoseDriver
                object rotationOnly = System.Enum.ToObject(prop.PropertyType, 1);
                prop.SetValue(tpdIS, rotationOnly);
            }
            Note("360Safe: IS TrackedPoseDriver -> RotationOnly");
        }
        else
        {
            // Legacy SpatialTracking fallback
            var legacy = targetCamera.GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
            if (!legacy) legacy = targetCamera.gameObject.AddComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
            legacy.trackingType = UnityEngine.SpatialTracking.TrackedPoseDriver.TrackingType.RotationOnly;
            Note("360Safe: Legacy TrackedPoseDriver -> RotationOnly");
        }

        // Also zero transform at setup
        targetCamera.transform.localPosition = Vector3.zero;
        targetCamera.transform.localRotation = Quaternion.identity;
    }

    void NukeARFoundationEverywhere()
    {
        int killed = 0;
        killed += DisableAll<ARSession>();
        killed += DisableAll<ARCameraBackground>();
        killed += DisableAll<ARCameraManager>();
        killed += DisableAll<ARPlaneManager>();
        killed += DisableAll<ARPointCloudManager>();
        killed += DisableAll<ARFaceManager>();
        killed += DisableAll<ARHumanBodyManager>();
        killed += DisableAll<AROcclusionManager>();
        killed += DisableAll<ARRaycastManager>();
        killed += DisableAll<AREnvironmentProbeManager>();
        killed += DisableAll<ARAnchorManager>();

        var origins = FindObjectsByType<XROrigin>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var o in origins)
        {
            o.enabled = false;
            o.gameObject.SetActive(false);
            killed++;
        }

        Note($"360Safe: Disabled AR/XR comps ≈ {killed}");
    }

    int DisableAll<T>() where T : Behaviour
    {
        int count = 0;
        var items = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in items)
        {
            if (!c) continue;
            c.enabled = false;

            if (c is ARSession s)
            {
                try
                {
                    var sub = s.subsystem;
                    if (sub != null && sub.running) sub.Stop();
                    s.Reset();
                }
                catch { }
            }
            count++;
        }
        return count;
    }

    void SafeRecenterXR()
    {
        // ✅ FIXED: use GetSubsystems instead of obsolete GetInstances
        var subs = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subs);

        foreach (var sub in subs)
        {
            if (sub == null) continue;
            sub.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
            sub.TryRecenter();
        }
    }

    // ------------------ Minimal on-screen status (works in builds) ------------------
    void Note(string line)
    {
        if (!showOnScreenStatus) return;
        _status = AppendLineClamped(_status, line, overlayLines);
        _statusUntil = Time.unscaledTime + 8f; // visible for a few seconds
    }

    string AppendLineClamped(string existing, string line, int maxLines)
    {
        var list = new List<string>(existing.Split('\n'));
        if (list.Count == 1 && string.IsNullOrEmpty(list[0])) list.Clear();
        list.Add(line);
        while (list.Count > maxLines) list.RemoveAt(0);
        return string.Join("\n", list);
    }

    void OnGUI()
    {
        if (!showOnScreenStatus) return;
        if (Time.unscaledTime > _statusUntil) return;

        var style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.normal.textColor = Color.white;

        var bg = new Color(0, 0, 0, 0.55f);
        var rect = new Rect(16, 16, Screen.width * 0.9f, 20 * overlayLines + 10);

        // draw bg
        var oldColor = GUI.color;
        GUI.color = bg;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = oldColor;

        GUI.Label(new Rect(20, 20, rect.width - 8, rect.height - 8), _status, style);
    }
}