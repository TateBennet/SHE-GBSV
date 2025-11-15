using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneButton : MonoBehaviour
{
    public string sceneToLoad = "MainSceneNameHere";

    public void LoadNextScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}