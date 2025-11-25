using UnityEngine;

public class StartSceneButton : MonoBehaviour
{
    [Header("Scene Manager to Start")]
    public TimedSceneManager timedSceneManager;

    [Header("Objects to Hide When Started (optional)")]
    public GameObject[] objectsToDisableOnStart;   // e.g. the button, intro canvas, etc.

    private bool _hasStarted = false;

    // Hook this up to your UI Button OnClick (or VR button OnPressed)
    public void OnStartButtonPressed()
    {
        if (_hasStarted)
            return;

        _hasStarted = true;

        if (timedSceneManager != null)
        {
            timedSceneManager.BeginSequence();
        }
        else
        {
            Debug.LogWarning("StartSceneButton: No TimedSceneManager assigned!");
        }

        if (objectsToDisableOnStart != null)
        {
            foreach (var obj in objectsToDisableOnStart)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
    }
}