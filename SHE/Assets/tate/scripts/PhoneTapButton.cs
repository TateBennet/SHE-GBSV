using UnityEngine;
using UnityEngine.Video;

public class PhoneTapButton : MonoBehaviour
{
    public PhoneMaterialSwitch phone;      // your phone parent
    public PlayTextSFX confirmTap;
    public DuoStreamPro DSP;      // drag your DuoStreamPro here
    private bool hasBeenTapped = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasBeenTapped && other.CompareTag("pointer"))
        {
            // Get whichever player is currently active
            VideoPlayer activePlayer = DSP.GetActivePlayer();

            // Resume playback if paused
            if (activePlayer && !activePlayer.isPlaying)
            {
                DSP.ResumeVideo();
            }

            hasBeenTapped = true;
            confirmTap.PlaySFX();
            phone.TapButton();
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        hasBeenTapped = false;
    }
}
