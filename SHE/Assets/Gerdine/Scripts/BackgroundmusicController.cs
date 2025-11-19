using UnityEngine;

public class VideoIndexAudioSync : MonoBehaviour
{
    [Header("References")]
    public DuoStreamPro duoStreamPro;   // Drag your DuoStreamPro object here

    [Header("Audio Sources")]
    public AudioSource firstAudioSource;   // For video index 0 (plays once)
    public AudioSource secondAudioSource;  // For video index 1 (loops)

    [Header("Settings")]
    public bool loopSecondAudio = true;    // Loop the second source

    private void OnEnable()
    {
        if (duoStreamPro != null)
        {
            // Subscribe to video index changes
            duoStreamPro.OnVideoChanged += HandleVideoChanged;
        }
        else
        {
            Debug.LogError("VideoIndexAudioSync: DuoStreamPro reference not assigned.");
        }
    }

    private void OnDisable()
    {
        if (duoStreamPro != null)
        {
            duoStreamPro.OnVideoChanged -= HandleVideoChanged;
        }
    }

    private void Start()
    {
        if (secondAudioSource != null)
        {
            secondAudioSource.loop = loopSecondAudio;
        }
    }

    private void HandleVideoChanged()
    {
        if (duoStreamPro == null) return;

        int index = duoStreamPro.GetCurrentVideoIndex();
        // Debug.Log("VideoIndexAudioSync: Current video index = " + index);

        switch (index)
        {
            case 0:
                // Video element 0 active ? play first audio once
                PlayFirstAudio();
                break;

            case 1:
                // Video element 1 active ? play & loop second audio
                PlaySecondAudio();
                break;

            default:
                // For other indices, you can stop both or leave as-is
                // StopAllAudio();
                break;
        }
    }

    private void PlayFirstAudio()
    {
        if (secondAudioSource != null)
            secondAudioSource.Stop();

        if (firstAudioSource != null)
        {
            firstAudioSource.loop = false;
            firstAudioSource.Play();
        }
    }

    private void PlaySecondAudio()
    {
        if (firstAudioSource != null)
            firstAudioSource.Stop();

        if (secondAudioSource != null)
        {
            secondAudioSource.loop = loopSecondAudio;
            secondAudioSource.Play();
        }
    }

    private void StopAllAudio()
    {
        if (firstAudioSource != null)
            firstAudioSource.Stop();
        if (secondAudioSource != null)
            secondAudioSource.Stop();
    }
}