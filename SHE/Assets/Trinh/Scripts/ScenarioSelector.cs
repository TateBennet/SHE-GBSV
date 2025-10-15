using UnityEngine;
using UnityEngine.XR.ARFoundation; // For FindFirstObjectByType compatibility


public class ScenarioSelector : MonoBehaviour
{
    [SerializeField] private ARTerminationManager terminationManager;

    void Start()
    {
        // Auto-find the ARTerminationManager in the scene
        if (terminationManager == null)
        {
            terminationManager = FindFirstObjectByType<ARTerminationManager>();
            if (terminationManager == null)
            {
                Debug.LogError("ARTerminationManager not found in scene! Please add it to AR Intro scene.");
            }
            else
            {
                Debug.Log("Found ARTerminationManager automatically");
            }
        }
    }

    public void OnVolleyballSelected()
    {
        if (terminationManager != null)
        {
            Debug.Log("Transitioning to VolleyballScene");
            terminationManager.InitiateCleanTransition("tate scene");
        }
        else
        {
            Debug.LogError("ARTerminationManager is null! Cannot transition.");
        }
    }

    public void OnSocialMediaSelected()
    {
        if (terminationManager != null)
        {
            Debug.Log("Transitioning to SocialMediaScene");
            terminationManager.InitiateCleanTransition("social media");
        }
        else
        {
            Debug.LogError("ARTerminationManager is null! Cannot transition.");
        }
    }
}
