using UnityEngine;

public class PlayTextSFX : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip sfx;

    public void PlaySFX()
    {
        audioSource.PlayOneShot(sfx);
    }
}
