using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class XRModeDirector : MonoBehaviour
{
    public static XRModeDirector I { get; private set; }

    [Header("Prefabs")]
    [Tooltip("Your AR rig prefab (contains ARSession, ARSessionOrigin/XROrigin w/AR managers, ARCameraBackground, etc.)")]
    public GameObject arRigPrefab;

    [Tooltip("Your classic XR rig prefab for 360 scenes (NO AR components).")]
    public GameObject vrRigPrefab;

    [Header("Optional")]
    [Tooltip("Frames to wait after activating the new camera before tearing down old camera(s).")]
    public int settleFrames = 2;

    GameObject _currentRig;  // either AR or VR instance
    string _currentMode = ""; // "AR" or "VR"

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    // ----- PUBLIC API -----
    public void EnterAR(string arSceneName)
    {
        StartCoroutine(CoEnterAR(arSceneName));
    }

    public void EnterVR360(string vrSceneName)
    {
        StartCoroutine(CoEnterVR(vrSceneName));
    }

    // ----- COs -----
    IEnumerator CoEnterAR(string sceneName)
    {
        // Load scene
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        // Spawn AR rig
        SpawnRig(arRigPrefab, "AR");

        // Ensure AR is enabled; VR-only bits gone
        KillAllVROnlyBitsInActiveScene();
        yield return ForcePromoteRigCamera();

        _currentMode = "AR";
        Debug.Log("[Director] Entered AR");
    }

    IEnumerator CoEnterVR(string sceneName)
    {
        // Load scene
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        // Spawn VR rig
        SpawnRig(vrRigPrefab, "VR");

        // Kill all AR paths in the active scene (if any were dragged in)
        HardKillARInActiveScene();

        // Make sure our VR camera is MainCamera and rendering before the compositor switches
        yield return ForcePromoteRigCamera();

        _currentMode = "VR";
        Debug.Log("[Director] Entered VR (360)");
    }

    // ----- Helpers -----
    void SpawnRig(GameObject prefab, string label)
    {
        if (!prefab)
        {
            Debug.LogError($"[Director] Missing {label} rig prefab.");
            return;
        }

        // Destroy prior rig if any
        if (_currentRig) Destroy(_currentRig);

        _currentRig = Instantiate(prefab);
        _currentRig.name = $"{label}Rig_Runtime";

        // Make sure exactly one active camera ends up as MainCamera
        DemoteAllMainCameras();
        var cams = _currentRig.GetComponentsInChildren<Camera>(true);
        if (cams.Length > 0)
        {
            cams[0].enabled = true;
            cams[0].tag = "MainCamera";
            EnsureSingleAudioListener(cams[0]);
        }
    }

    IEnumerator ForcePromoteRigCamera()
    {
        // Let one or two frames render from our rig camera to make compositor swap sources
        var cam = Camera.main;
        for (int i = 0; i < Mathf.Max(1, settleFrames); i++)
        {
            yield return new WaitForEndOfFrame();
#if UNITY_6000_0_OR_NEWER || UNITY_2022_3_OR_NEWER
            Camera.SetupCurrent(cam);
#endif
            if (cam) cam.Render();
        }
    }

    static void DemoteAllMainCameras()
    {
        var cams = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in cams) if (c.CompareTag("MainCamera")) c.tag = "Untagged";
    }

    static void EnsureSingleAudioListener(Camera winner)
    {
        var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var al in listeners) if (al && al.gameObject != winner.gameObject) al.enabled = false;
        var my = winner.GetComponent<AudioListener>() ?? winner.gameObject.AddComponent<AudioListener>();
        my.enabled = true;
    }

    static void HardKillARInActiveScene()
    {
        var s = SceneManager.GetActiveScene();

        // Stop AR session(s)
        foreach (var ar in FindObjectsByType<ARSession>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ar.gameObject.scene != s) continue;
            try { if (ar.subsystem != null && ar.subsystem.running) ar.subsystem.Stop(); } catch { }
            ar.enabled = false;
            try { ar.Reset(); } catch { }
            Destroy(ar.gameObject); // kill the root that typically owns AR managers
        }

        // Disable any stray AR managers/backgrounds (paranoia)
        DisableAllInScene<ARCameraBackground>(s);
        DisableAllInScene<ARCameraManager>(s);
        DisableAllInScene<ARPlaneManager>(s);
        DisableAllInScene<ARRaycastManager>(s);
        DisableAllInScene<ARPointCloudManager>(s);
        DisableAllInScene<AROcclusionManager>(s);
        DisableAllInScene<AREnvironmentProbeManager>(s);
        DisableAllInScene<ARAnchorManager>(s);

        Debug.Log("[Director] Hard-killed AR in active scene (if any).");
    }

    static void KillAllVROnlyBitsInActiveScene()
    {
        // If you have VR-only helpers you accidentally dropped in AR scenes, disable them here.
        // (left blank intentionally; hook your own components if needed)
    }

    static void DisableAllInScene<T>(Scene target) where T : Behaviour
    {
        var items = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in items)
        {
            if (!c) continue;
            if (c.gameObject.scene != target) continue;
            c.enabled = false;
        }
    }
}
