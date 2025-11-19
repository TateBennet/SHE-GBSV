using UnityEngine;

public class SequentialAudioPlayer : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Clips to Play in Order")]
    public AudioClip[] audioClips;

    [Header("Settings")]
    public bool playOnStart = true;   // Auto-start when scene begins
    public bool loopSequence = false; // Loop the entire list

    private int currentIndex = 0;

    private void Start()
    {
        if (playOnStart)
        {
            StartSequence();
        }
    }

    public void StartSequence()
    {
        if (audioClips.Length == 0)
        {
            Debug.LogWarning("No audio clips assigned!");
            return;
        }

        currentIndex = 0;
        PlayNextClip();
    }

    private void PlayNextClip()
    {
        if (currentIndex >= audioClips.Length)
        {
            if (loopSequence)
            {
                currentIndex = 0;
            }
            else
            {
                return; // Stop after final clip
            }
        }

        AudioClip clip = audioClips[currentIndex];
        audioSource.clip = clip;
        audioSource.Play();

        // Schedule the next clip
        Invoke(nameof(HandleClipEnd), clip.length);
    }

    private void HandleClipEnd()
    {
        currentIndex++;
        PlayNextClip();
    }
}