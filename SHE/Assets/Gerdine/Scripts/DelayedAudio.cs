using UnityEngine;

public class DelayedAudioStart : MonoBehaviour
{
    public float delay = 5f; // seconds before audio starts
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Start the coroutine that waits before playing
        StartCoroutine(PlayAfterDelay());
    }

    System.Collections.IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(delay);
        audioSource.Play();
    }
}