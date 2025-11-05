using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils; // ✅ Needed for XROrigin in Unity 6
using UnityEngine.XR.Interaction.Toolkit; // still needed for other XR Toolkit components


public class SceneOrchestrator : MonoBehaviour
{
    // set these in the inspector on XR Transition
    [Header("Scene Names")]
    [SerializeField] private string homeScene = "XR Transition";          // the one that loads first
    [SerializeField] private string autoHandScene = "LockerRoom";         // Auto Hand scene
    [SerializeField] private string xr360Scene = "tate scene duo stream"; // 360/XRI scene

    public static SceneOrchestrator Instance { get; private set; }

    private void Awake()
    {
        // simple singleton for easy access from the other scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // we assume we are already IN "XR Transition"
        // so we just load the AutoHand scene on top
        StartCoroutine(LoadAutoHandAdditive());
    }

    private IEnumerator LoadAutoHandAdditive()
    {
        // only load if it's not already loaded
        if (!IsSceneLoaded(autoHandScene))
        {
            var op = SceneManager.LoadSceneAsync(autoHandScene, LoadSceneMode.Additive);
            while (!op.isDone)
                yield return null;
        }

        // OPTIONAL: if your XR rig in XR Transition should be off while AutoHand is active,
        // you can disable it here. Just tag it or find by name.
        DisableXRTransitionRig(true);
    }

    public void GoToXR360()
    {
        // this is what your button in LockerRoom should call
        StartCoroutine(SwapAutoHandForXR360());
    }

    private IEnumerator SwapAutoHandForXR360()
    {
        // 1) unload the AutoHand scene
        if (IsSceneLoaded(autoHandScene))
        {
            var unload = SceneManager.UnloadSceneAsync(autoHandScene);
            while (unload != null && !unload.isDone)
                yield return null;
        }

        // 2) re-enable the XRI rig from XR Transition
        DisableXRTransitionRig(false);

        // 3) load the 360 scene additively
        if (!IsSceneLoaded(xr360Scene))
        {
            var load = SceneManager.LoadSceneAsync(xr360Scene, LoadSceneMode.Additive);
            while (!load.isDone)
                yield return null;
        }

        // at this point:
        // XR Transition + tate scene duo stream = loaded
        // LockerRoom = gone
    }

    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.name == sceneName)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Enable/disable the XRI rig that lives in XR Transition.
    /// Adjust the lookup to match your actual rig name/tag.
    /// </summary>
    private void DisableXRTransitionRig(bool disable)
    {
        // try to find a rig in the root of the active scene
        // NOTE: Unity 6 wants the newer API:
        var xri = Object.FindFirstObjectByType<XROrigin>();
        if (xri != null)
        {
            xri.gameObject.SetActive(!disable);
        }
        else
        {
            var go = GameObject.Find("XR Origin") ?? GameObject.Find("XR Rig");
            if (go != null)
                go.SetActive(!disable);
        }
    }
}
