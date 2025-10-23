using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTapper : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The name of the scene to load (must be added to Build Settings).")]
    public string sceneToLoad;

    [Tooltip("Only trigger once per collision.")]
    public bool loadOnce = true;

    private bool hasLoaded = false;

    // Use this if your collider has 'Is Trigger' enabled
    private void OnTriggerEnter(Collider other)
    {
        if (loadOnce && hasLoaded) return;

        if (other.CompareTag("pointer"))
        {
            Debug.Log($"Pointer collided — loading scene: {sceneToLoad}");
            hasLoaded = true;
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
