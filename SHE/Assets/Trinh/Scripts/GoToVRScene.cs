using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToVRScene : MonoBehaviour
{
    [SerializeField] private string transitionSceneName = "XRTransition"; // name you'll create

    public void OnClickGo()
    {
        SceneManager.LoadScene(transitionSceneName, LoadSceneMode.Single);
    }
}
