using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeOnPressed : MonoBehaviour
{
    [Header("Scene To Load")]
    public string sceneName = "NextSceneName";

    private void Start()
    {
        Debug.Log("SceneChangeOnPressed: Start() on " + gameObject.name);
    }

    // Hook this to PhysicsGadgetButton.OnPressed in the Inspector
    public void ChangeScene()
    {
        Debug.Log("SceneChangeOnPressed: ChangeScene() called on " + gameObject.name);

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneChangeOnPressed: No scene name assigned!");
            return;
        }

        Debug.Log("SceneChangeOnPressed: Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}