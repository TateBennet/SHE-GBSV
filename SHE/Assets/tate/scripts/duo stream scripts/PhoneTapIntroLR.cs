using UnityEngine;

public class PhoneTapIntroLR : MonoBehaviour
{
    public PhoneScreenSwitchIntro phone; // Drag the phone parent here
    public PlayTextSFX confirmtap;
    private bool hasBeenTapped = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasBeenTapped && other.CompareTag("pointer"))
        {
            hasBeenTapped = true;          // prevent multiple triggers
            confirmtap.PlaySFX();
            phone.TapButton();
            gameObject.SetActive(false);   // disable this button right away
        }
    }

    private void OnEnable()
    {
        hasBeenTapped = false; // reset when re-enabled for the next screen
    }
}
