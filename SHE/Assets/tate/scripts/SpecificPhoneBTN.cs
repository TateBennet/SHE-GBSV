using UnityEngine;
using UnityEngine.Video;

public class SpecificPhoneBTN : MonoBehaviour
{
    public PhoneMaterialSwitch phone; // Drag the phone parent here
    public VideoPlayer player;
    public GameObject phonebtn;
    private bool hasBeenTapped = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasBeenTapped && other.CompareTag("pointer"))
        {
            if (!player.isPlaying) player.Play();
            hasBeenTapped = true;          // prevent multiple triggers
            phone.TapButton();
            gameObject.SetActive(false);   // disable this button right away
            phonebtn.SetActive(true);
        }
    }

    private void OnEnable()
    {
        hasBeenTapped = false; // reset when re-enabled for the next screen
    }
}
