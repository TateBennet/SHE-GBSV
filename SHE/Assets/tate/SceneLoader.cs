using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Call this from your button, pass the scene name you want to load.
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Or, if you want a button that always loads a specific scene:
    [SerializeField] private string targetScene;

    public void LoadConfiguredScene()
    {
        if (!string.IsNullOrEmpty(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogWarning("SceneLoader: No target scene set!");
        }
    }
}
