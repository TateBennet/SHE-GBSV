using UnityEngine;

public class VideoProgressCollider : MonoBehaviour
{
    public PhoneMaterialSwitch phone; // Drag the phone parent here
    public StreamVideos streamVideos;
    private bool hasBeenTapped = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasBeenTapped && other.CompareTag("pointer"))
        {
            hasBeenTapped = true;          // prevent multiple triggers
            phone.TapButton();
            gameObject.SetActive(false);   // disable this button right away
            streamVideos.PlayNext();
        }
    }

    private void OnEnable()
    {
        hasBeenTapped = false; // reset when re-enabled for the next screen
    }
}
