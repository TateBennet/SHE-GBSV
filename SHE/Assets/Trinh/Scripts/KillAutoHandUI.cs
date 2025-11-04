using UnityEngine;

// Run just after the camera cleanup
[DefaultExecutionOrder(-480)]
public class KillAutoHandUI : MonoBehaviour
{
    void Awake()
    {
        // Look for Auto Hand UI-related behaviours by name
        var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in allBehaviours)
        {
            if (mb == null) continue;

            string n = mb.GetType().Name;

            // these are common in Auto Hand UI
            if (n.Contains("HandCanvasPointer") ||
                n.Contains("AutoInputModule"))
            {
                mb.gameObject.SetActive(false);
            }
        }
    }
}
