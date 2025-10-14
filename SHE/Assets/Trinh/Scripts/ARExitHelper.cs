using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;




public class ARExitHelper : MonoBehaviour
{
    [Tooltip("Frames to wait after disabling AR so the compositor fully detaches.")]
    public int settleFrames = 2;

    [Tooltip("Also destroy any Camera that has ARCameraBackground (belt & suspenders).")]
    public bool destroyARCamera = true;

    [Tooltip("Minimal logs (useful even in release).")]
    public bool minimalLog = true;

    public void OnClickLoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("ARExitHelper: No scene name provided.");
            return;
        }
        StartCoroutine(CoTearDownARAndLoad(sceneName));
    }

    IEnumerator CoTearDownARAndLoad(string sceneName)
    {
        // Make sure timeScale isn't paused by UI.
        Time.timeScale = 1f;

        // 1) Disable/destroy AR Foundation bits ONLY. Keep XR running!
        TearDownARFoundation(destroyARCamera);

        // 2) Let the render graph settle for a couple frames.
        for (int i = 0; i < Mathf.Max(0, settleFrames); i++) yield return null;

        if (minimalLog) Debug.Log("[ARExit] Loading 360 scene (Single): " + sceneName);

        // 3) Load the 360 scene cleanly.
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    void TearDownARFoundation(bool alsoDestroyARCamera)
    {
        int disabled = 0;

        // Stop ARSession (don’t touch XR input/display).
        foreach (var s in FindObjectsByType<ARSession>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            try
            {
                if (s.subsystem != null && s.subsystem.running) s.subsystem.Stop();
                s.enabled = false;
                s.Reset();
            }
            catch { }
            disabled++;
        }

        // Disable all AR managers that could re-enable passthrough/AR background.
        disabled += DisableAll<ARCameraBackground>(alsoDestroyARCamera);
        disabled += DisableAll<ARCameraManager>();
        disabled += DisableAll<ARPlaneManager>();
        disabled += DisableAll<ARRaycastManager>();
        disabled += DisableAll<ARPointCloudManager>();
        disabled += DisableAll<ARFaceManager>();
        disabled += DisableAll<ARHumanBodyManager>();
        disabled += DisableAll<AROcclusionManager>();
        disabled += DisableAll<AREnvironmentProbeManager>();
        disabled += DisableAll<ARAnchorManager>();

        if (alsoDestroyARCamera)
        {
            var bgs = FindObjectsByType<ARCameraBackground>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var bg in bgs)
            {
                if (!bg) continue;
                var cam = bg.GetComponent<Camera>();
                if (cam)
                {
                    // Make sure it can’t composite anymore.
                    cam.targetTexture = null;
                    cam.clearFlags = CameraClearFlags.Skybox;
                    cam.enabled = false;
                    // Nuke it to be 100% sure no DontDestroyOnLoad camera lingers.
                    Object.Destroy(cam.gameObject);
                }
            }
        }

        if (minimalLog) Debug.Log("[ARExit] Disabled/Killed AR components ≈ " + disabled);
    }

    int DisableAll<T>(bool destroyIfCameraBackground = false) where T : Behaviour
    {
        int count = 0;
        var items = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in items)
        {
            if (!c) continue;
            c.enabled = false;

            // If ARCameraBackground, optionally neuter its camera too.
            if (destroyIfCameraBackground && c is ARCameraBackground bg)
            {
                var cam = bg.GetComponent<Camera>();
                if (cam)
                {
                    cam.targetTexture = null;
                    cam.clearFlags = CameraClearFlags.Skybox;
                    cam.enabled = false;
                }
            }
            count++;
        }
        return count;
    }
}
