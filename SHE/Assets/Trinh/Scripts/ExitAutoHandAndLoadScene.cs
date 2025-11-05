using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitAutoHandAndLoadScene : MonoBehaviour
{
    [SerializeField] private string targetSceneName;

    // hook this to your button OnClick
    public void ExitAndLoad()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[ExitAutoHand] No target scene set.");
            return;
        }

        StartCoroutine(ExitAndLoadRoutine());
    }

    private IEnumerator ExitAndLoadRoutine()
    {
        // 1) Kill Auto Hand stuff that survives between scenes
        // We use the type names as strings so this compiles even if Auto Hand
        // is not in every build.
        var allMBs = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (var mb in allMBs)
        {
            if (mb == null) continue;
            string t = mb.GetType().FullName;

            // Auto Hand singletons / components we've seen in your logs
            if (t.StartsWith("Autohand.AutoInputModule") ||
                t.StartsWith("Autohand.HandCanvasPointer") ||
                t.StartsWith("Autohand.HandBase") ||
                t.StartsWith("Autohand.Hand") ||
                t.StartsWith("Autohand.OpenXRAutoHandTracking") ||
                t.StartsWith("Autohand.OpenXRAutoHandTrackingGrabber"))
            {
                // most of these are on their own GameObject, so kill the GO
                Debug.Log($"[ExitAutoHand] Destroying AutoHand object: {t}");
                Destroy(mb.gameObject);
            }
        }

        // 2) also good to clear extra AudioListeners so next scene has 1
        var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        bool keepFirst = true;
        foreach (var al in listeners)
        {
            if (keepFirst)
            {
                keepFirst = false;
            }
            else
            {
                Destroy(al);
            }
        }

        // let all the Destroy()s process
        yield return null;

        // 3) now load the XR scene cleanly
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }
}
