using UnityEngine;

public class VballSFX : MonoBehaviour
{
    public AudioClip hitSound;

    public AudioSource audioSource;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Hand"))
        {
            audioSource.PlayOneShot(hitSound);
        }
    }
}
