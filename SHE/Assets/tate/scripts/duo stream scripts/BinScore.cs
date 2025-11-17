using UnityEngine;
using UnityEngine.SceneManagement;

public class BinScore : MonoBehaviour
{
    public string sceneToLoad = "NextScene";
    private bool loadedScene = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            if (loadedScene == true)
                return;
            Debug.Log("ball scored, loading volleyball scene");
            SceneManager.LoadScene(sceneToLoad);
            loadedScene = true;
        }
    }
}