using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ExitAutoHandScene : MonoBehaviour
{
    [SerializeField] private string nextSceneName;   // set this in Inspector

    public void ExitToNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[ExitAutoHandScene] No scene name set.");
            return;
        }

        StartCoroutine(DoExit());
    }

    private IEnumerator DoExit()
    {
        // 1. Disable Auto Hand behaviours so they stop processing this frame
        var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in allBehaviours)
        {
            if (mb == null) continue;
            var ns = mb.GetType().Namespace;
            if (string.IsNullOrEmpty(ns)) continue;

            if (ns.StartsWith("Autohand"))
            {
                // most Auto Hand components inherit from Behaviour, so we can disable them
                if (mb is Behaviour b)
                    b.enabled = false;
            }
        }

        // 2. If the EventSystem is using Auto Hand's input module, disable the EventSystem
        var es = EventSystem.current;
        if (es != null)
        {
            // check for a component called Autohand.AutoInputModule on the same GameObject
            var autoInput = es.GetComponents<Component>()
                              .FirstOrDefault(c => c != null && c.GetType().FullName == "Autohand.AutoInputModule");
            if (autoInput != null)
            {
                es.enabled = false;
            }
        }

        // 3. Deactivate “hourglass” / “loading” objects that Auto Hand marked DontDestroyOnLoad
        // Resources.FindObjectsOfTypeAll is still valid in Unity 6 for runtime discovery
        var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in allGos)
        {
            if (go == null) continue;
            string lower = go.name.ToLowerInvariant();
            if (lower.Contains("hourglass") || lower.Contains("loading"))
            {
                go.SetActive(false);
            }
        }

        // 4. Wait one frame so the disabled components finish this frame cleanly
        yield return null;

        // 5. Now load your clean XRI / 360 scene
        SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
    }
}
