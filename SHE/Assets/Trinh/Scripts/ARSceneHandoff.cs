using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class ARSceneHandoff : MonoBehaviour
{
    [Tooltip("Optional: name of the AR intro scene (for logging/unload). Will auto-detect if empty.")]
    public string arSceneName = "";

    [Tooltip("How many frames to render with the 360 camera before nuking AR.")]
    public int framesOn360BeforeTearDown = 2;

    [Tooltip("How many frames to render after tearing AR down before unloading AR scene.")]
    public int framesAfterTearDown = 1;

    [Tooltip("Force set the 360 camera as current & render it manually for a frame.")]
    public bool forceManualRenderKick = true;

    bool _busy;

    public void Go(string nextSceneName)
    {
        if (_busy) return;
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[HandoffV2] Next scene name empty.");
            return;
        }
        StartCoroutine(CoHandoff(nextSceneName));
    }

    IEnumerator CoHandoff(string nextScene)
    {
        _busy = true;
        Time.timeScale = 1f;

        // Current (AR) scene:
        var arScene = string.IsNullOrEmpty(arSceneName) ? gameObject.scene : SceneManager.GetSceneByName(arSceneName);
        if (!arScene.IsValid()) arScene = gameObject.scene;

        // 1) Load 360 scene ADDITIVELY first.
        var load = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Additive);
        while (!load.isDone) yield return null;

        var scene360 = SceneManager.GetSceneByName(nextScene);
        if (!scene360.IsValid())
        {
            Debug.LogError("[HandoffV2] Loaded 360 scene invalid: " + nextScene);
            yield break;
        }
        SceneManager.SetActiveScene(scene360);

        // 2) Find the 360 scene’s main rendering camera.
        Camera cam360 = null;
        {
            Camera[] cams = GameObject.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var c in cams)
            {
                if (c && c.enabled && c.gameObject.scene == scene360)
                {
                    cam360 = c;
                    break;
                }
            }
        }

        if (!cam360)
        {
            Debug.LogWarning("[HandoffV2] No enabled camera found in 360 scene; proceeding but this will freeze display.");
        }
        else
        {
            // Ensure sane clear flags.
            if (cam360.clearFlags == CameraClearFlags.Nothing)
                cam360.clearFlags = CameraClearFlags.Skybox;

            // 2a) Render a couple frames from the 360 camera BEFORE touching AR.
            for (int i = 0; i < Mathf.Max(1, framesOn360BeforeTearDown); i++)
            {
                if (forceManualRenderKick)
                {
                    yield return new WaitForEndOfFrame();
#if UNITY_6000_0_OR_NEWER || UNITY_2022_3_OR_NEWER
                    Camera.SetupCurrent(cam360);
#endif
                    cam360.Render(); // explicit draw so compositor has a non-AR frame
                }
                else
                {
                    yield return null;
                }
            }
        }

        // 3) HARD TEAR-DOWN: kill all AR sources in the AR scene (so they cannot re-enable compositor).
        HardKillARInScene(arScene);

        // 4) Give compositor a frame to detach from AR completely.
        for (int i = 0; i < Mathf.Max(0, framesAfterTearDown); i++)
        {
            if (forceManualRenderKick && cam360)
            {
                yield return new WaitForEndOfFrame();
#if UNITY_6000_0_OR_NEWER || UNITY_2022_3_OR_NEWER
                Camera.SetupCurrent(cam360);
#endif
                cam360.Render();
            }
            else
            {
                yield return null;
            }
        }

        // 5) Unload the AR intro scene.
        if (arScene.IsValid())
        {
            var unload = SceneManager.UnloadSceneAsync(arScene);
            while (unload != null && !unload.isDone) yield return null;
        }

        _busy = false;
        Debug.Log("[HandoffV2] Complete.");
    }

    void HardKillARInScene(Scene targetScene)
    {
        // Disable ARSession(s) + reset
        foreach (var s in FindObjectsByType<ARSession>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (s.gameObject.scene != targetScene) continue;
            try { if (s.subsystem != null && s.subsystem.running) s.subsystem.Stop(); } catch { }
            s.enabled = false;
            try { s.Reset(); } catch { }
        }

        // Nuke all AR managers & background providers in that scene
        KillAllInScene<ARCameraBackground>(targetScene, destroyGO: false, alsoDisableCamera: true);
        KillAllInScene<ARCameraManager>(targetScene);
        KillAllInScene<ARPlaneManager>(targetScene);
        KillAllInScene<ARRaycastManager>(targetScene);
        KillAllInScene<ARPointCloudManager>(targetScene);
        KillAllInScene<ARFaceManager>(targetScene);
        KillAllInScene<ARHumanBodyManager>(targetScene);
        KillAllInScene<AROcclusionManager>(targetScene);
        KillAllInScene<AREnvironmentProbeManager>(targetScene);
        KillAllInScene<ARAnchorManager>(targetScene);

        // Finally, destroy any Camera in that scene that had AR background
        var bgs = FindObjectsByType<ARCameraBackground>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var bg in bgs)
        {
            if (!bg) continue;
            if (bg.gameObject.scene != targetScene) continue;

            var cam = bg.GetComponent<Camera>();
            if (cam)
            {
                cam.targetTexture = null;
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.enabled = false;
                Object.Destroy(cam.gameObject); // remove the AR camera entirely
            }
        }
    }

    void KillAllInScene<T>(Scene target, bool destroyGO = false, bool alsoDisableCamera = false) where T : Behaviour
    {
        var items = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in items)
        {
            if (!c) continue;
            if (c.gameObject.scene != target) continue;

            if (alsoDisableCamera && c is ARCameraBackground)
            {
                var cam = c.GetComponent<Camera>();
                if (cam)
                {
                    cam.targetTexture = null;
                    cam.clearFlags = CameraClearFlags.Skybox;
                    cam.enabled = false;
                }
            }

            c.enabled = false;
            if (destroyGO) Object.Destroy(c.gameObject);
        }
    }
}
