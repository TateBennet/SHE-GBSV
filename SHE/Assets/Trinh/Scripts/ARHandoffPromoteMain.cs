using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class ARHandoffPromoteMain : MonoBehaviour
{
    [Tooltip("Optional; leave empty if this script lives in the AR Intro scene.")]
    public string arSceneName = "";

    [Tooltip("How many frames to render from the 360 camera before tearing down AR.")]
    public int framesOn360BeforeTearDown = 2;

    [Tooltip("How many frames to render after tearing down AR (safety).")]
    public int framesAfterTearDown = 1;

    bool _busy;

    public void Go(string nextScene)
    {
        if (_busy) return;
        if (string.IsNullOrEmpty(nextScene))
        {
            Debug.LogError("[Handoff] Next scene name is empty.");
            return;
        }
        StartCoroutine(CoGo(nextScene));
    }

    IEnumerator CoGo(string nextScene)
    {
        _busy = true;
        Time.timeScale = 1f;

        // AR scene (this scene by default)
        var arScene = string.IsNullOrEmpty(arSceneName) ? gameObject.scene : SceneManager.GetSceneByName(arSceneName);
        if (!arScene.IsValid()) arScene = gameObject.scene;

        // 1) Load 360 scene ADDITIVELY
        var op = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        var scene360 = SceneManager.GetSceneByName(nextScene);
        if (!scene360.IsValid())
        {
            Debug.LogError("[Handoff] 360 scene invalid after load.");
            yield break;
        }
        SceneManager.SetActiveScene(scene360);

        // 2) Promote a camera from the 360 scene to be THE MainCamera and ensure it renders
        Camera cam360 = FindFirstEnabledCameraInScene(scene360);
        if (!cam360)
        {
            Debug.LogError("[Handoff] No enabled Camera found in 360 scene.");
            yield break;
        }

        // Ensure it’s the only MainCamera
        DemoteAllMainCameras();
        cam360.tag = "MainCamera";
        cam360.enabled = true;
        cam360.clearFlags = (cam360.clearFlags == CameraClearFlags.Nothing) ? CameraClearFlags.Skybox : cam360.clearFlags;
        EnsureSingleAudioListener(cam360);

        // 2a) Render a couple of frames from the 360 camera BEFORE touching AR
        for (int i = 0; i < Mathf.Max(1, framesOn360BeforeTearDown); i++)
        {
            yield return new WaitForEndOfFrame();
#if UNITY_6000_0_OR_NEWER || UNITY_2022_3_OR_NEWER
            Camera.SetupCurrent(cam360);
#endif
            cam360.Render();
        }

        // 3) Hard-destroy AR sources in the AR scene
        HardKillARInScene(arScene);

        // 4) Safety frames after AR teardown
        for (int i = 0; i < Mathf.Max(0, framesAfterTearDown); i++)
        {
            yield return new WaitForEndOfFrame();
#if UNITY_6000_0_OR_NEWER || UNITY_2022_3_OR_NEWER
            Camera.SetupCurrent(cam360);
#endif
            cam360.Render();
        }

        // 5) Unload AR scene
        if (arScene.IsValid())
        {
            var unload = SceneManager.UnloadSceneAsync(arScene);
            while (unload != null && !unload.isDone) yield return null;
        }

        Debug.Log("[Handoff] Complete.");
        _busy = false;
    }

    // ————— helpers —————

    static Camera FindFirstEnabledCameraInScene(Scene s)
    {
        var cams = GameObject.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var c in cams)
        {
            if (c && c.enabled && c.gameObject.scene == s) return c;
        }
        return null;
    }

    static void DemoteAllMainCameras()
    {
        var cams = GameObject.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in cams)
        {
            if (c && c.CompareTag("MainCamera")) c.tag = "Untagged";
        }
    }

    static void EnsureSingleAudioListener(Camera winner)
    {
        // Disable other listeners, ensure one on the winning camera
        var listeners = GameObject.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var al in listeners)
        {
            if (!al) continue;
            if (al.gameObject != winner.gameObject) al.enabled = false;
        }
        var my = winner.GetComponent<AudioListener>();
        if (!my) my = winner.gameObject.AddComponent<AudioListener>();
        my.enabled = true;
    }

    static void HardKillARInScene(Scene targetScene)
    {
        // Stop ARSession(s)
        foreach (var s in FindObjectsByType<ARSession>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (s.gameObject.scene != targetScene) continue;
            try { if (s.subsystem != null && s.subsystem.running) s.subsystem.Stop(); } catch { }
            s.enabled = false;
            try { s.Reset(); } catch { }
        }

        // Disable all AR managers & AR backgrounds
        DisableAllInScene<ARCameraBackground>(targetScene);
        DisableAllInScene<ARCameraManager>(targetScene);
        DisableAllInScene<ARPlaneManager>(targetScene);
        DisableAllInScene<ARRaycastManager>(targetScene);
        DisableAllInScene<ARPointCloudManager>(targetScene);
        DisableAllInScene<ARFaceManager>(targetScene);
        DisableAllInScene<ARHumanBodyManager>(targetScene);
        DisableAllInScene<AROcclusionManager>(targetScene);
        DisableAllInScene<AREnvironmentProbeManager>(targetScene);
        DisableAllInScene<ARAnchorManager>(targetScene);

        // Kill any camera in that AR scene (so nothing can keep rendering)
        var cams = GameObject.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in cams)
        {
            if (c.gameObject.scene != targetScene) continue;
            c.targetTexture = null;
            c.enabled = false;
            Object.Destroy(c.gameObject);
        }
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
