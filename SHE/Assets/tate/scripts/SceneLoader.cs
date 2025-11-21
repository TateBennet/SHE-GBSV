using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{

    public static bool finishedA = false;
    public static bool finishedB = false;

    // Call this from your button, pass the scene name you want to load.
    public void LoadScene()
    {
        if(finishedA && finishedB)
        {
            SceneManager.LoadScene("EndScene");
        }
        else if (!finishedA || !finishedB)
        {
            SceneManager.LoadScene("IntroFinal");
        }
    }

    public void FinishedSocialMedia()
    {
        finishedA = true;
    }

    public void FinishedVolleyBall() 
    {  
        finishedB = true; 
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
