using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class ARSceneTransit : MonoBehaviour
{
    [Header("Scene to load")]
    public string nextSceneName;

    [Header("References in AR Intro Scene")]
    public Camera arCamera;          // Your AR camera (the one with ARCameraBackground)
    public Camera transitCamera;     // The dummy camera you created above

    [Header("Frames to wait")]
    public int framesOnTransitCamera = 2;

    void Reset()
    {
        // best-effort auto-find
        arCamera = Camera.main;
        if (!transitCamera)
            transitCamera = GameObject.Find("TransitCamera")?.GetComponent<Camera>();
    }

    public void Go()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("ARSceneTransit: nextSceneName not set.");
            return;
        }
        StartCoroutine(CoGo());
    }

    IEnumerator CoGo()
    {
        // 1) Make sure time isn't paused
        Time.timeScale = 1f;

        // 2) Enable a clean non-AR camera FIRST
        if (!transitCamera)
        {
            Debug.LogError("ARSceneTransit: TransitCamera is missing.");
            yield break;
        }
        transitCamera.cullingMask = 0;   // renders nothing, just needs to submit a clean frame
        transitCamera.enabled = true;

        // 3) Disable the AR camera object (so compositor detaches from AR layer)
        if (arCamera)
        {
            // Try to neuter AR background if present (without stopping XR input)
            var bg = arCamera.GetComponent<ARCameraBackground>();
            if (bg) bg.enabled = false;

            var camMgr = arCamera.GetComponent<ARCameraManager>();
            if (camMgr) camMgr.enabled = false;

            // now fully disable the AR camera so it stops submitting frames
            arCamera.enabled = false;
            arCamera.gameObject.SetActive(false);
        }

        // 4) Disable common AR managers in this scene (so they can't re-enable AR)
        DisableAllInScene<ARSession>();
        DisableAllInScene<ARPlaneManager>();
        DisableAllInScene<ARRaycastManager>();
        DisableAllInScene<ARPointCloudManager>();
        DisableAllInScene<ARFaceManager>();
        DisableAllInScene<ARHumanBodyManager>();
        DisableAllInScene<AROcclusionManager>();
        DisableAllInScene<AREnvironmentProbeManager>();
        DisableAllInScene<ARAnchorManager>();
        DisableAllInScene<ARCameraBackground>();
        DisableAllInScene<ARCameraManager>();

        // 5) Let the transit camera submit a couple of clean frames
        for (int i = 0; i < Mathf.Max(2, framesOnTransitCamera); i++)
            yield return null;

        // 6) Load the 360 scene SINGLE (clean switch)
        SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
    }

    static void DisableAllInScene<T>() where T : Behaviour
    {
        var items = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in items)
        {
            if (!c) continue;
            c.enabled = false;
        }
    }
}
