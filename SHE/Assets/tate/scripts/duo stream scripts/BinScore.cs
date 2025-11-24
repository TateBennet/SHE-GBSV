using UnityEngine;
using UnityEngine.SceneManagement;

public class BinScore : MonoBehaviour
{
    public string sceneToLoad = "NextScene";
    private bool loadedScene = false;
    public FadeToBlack fade;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            if (loadedScene == true)
                return;
            Debug.Log("ball scored, loading volleyball scene");
            StartCoroutine(LoadAfterDelay());
        }
    }

    private System.Collections.IEnumerator LoadAfterDelay()
    {
        fade.Blackout();
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(sceneToLoad);
        loadedScene = true;
    }
}