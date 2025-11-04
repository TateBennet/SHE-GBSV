using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class XRTransitionCleaner : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "Your360Scene"; // set in Inspector

    private void Start()
    {
        StartCoroutine(CleanThenLoad());
    }

    private IEnumerator CleanThenLoad()
    {
        // 1) let Unity finish loading this scene
        yield return null;

        // 2) kill any Auto Hand objects that stayed alive as DontDestroyOnLoad
        //    (we are in a different scene now, so Auto Hand is not mid-click)
        var allBehaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        foreach (var mb in allBehaviours)
        {
            if (mb == null) continue;
            var ns = mb.GetType().Namespace;
            if (!string.IsNullOrEmpty(ns) && ns.StartsWith("Autohand"))
            {
                // nuke the whole GameObject – most Auto Hand bits sit together
                Destroy(mb.gameObject);
            }
        }

        // 3) also kill hourglass / loading things Auto Hand left around
        var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in allGos)
        {
            if (go == null) continue;
            var lower = go.name.ToLowerInvariant();
            if (lower.Contains("hourglass") || lower.Contains("loading"))
            {
                Destroy(go);
            }
        }

        // 4) wait one more frame so the destroys apply
        yield return null;

        // 5) now load your actual XRI scene
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }
}
