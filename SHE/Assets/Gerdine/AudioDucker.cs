using UnityEngine;
using System.Collections;

public class AutoAudioDucker : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource backgroundAudio;   // music / ambience
    public AudioSource voiceOverAudio;    // narration

    [Header("Settings")]
    public float fadeDuration = 1.0f;           // fade time
    public float backgroundDuckVolume = 0.2f;   // volume during voiceover

    private float originalBackgroundVolume;
    private bool isDucking = false;

    void Start()
    {
        if (backgroundAudio != null)
            originalBackgroundVolume = backgroundAudio.volume;

        // Start monitoring for voiceover changes
        StartCoroutine(MonitorVoiceOver());
    }

    IEnumerator MonitorVoiceOver()
    {
        while (true)
        {
            // If VO starts playing ? fade down
            if (voiceOverAudio.isPlaying && !isDucking)
            {
                StartCoroutine(FadeBackground(backgroundDuckVolume));
                isDucking = true;
            }

            // If VO has stopped ? fade back up
            if (!voiceOverAudio.isPlaying && isDucking)
            {
                StartCoroutine(FadeBackground(originalBackgroundVolume));
                isDucking = false;
            }

            yield return null; // check every frame
        }
    }

    IEnumerator FadeBackground(float targetVolume)
    {
        float start = backgroundAudio.volume;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            backgroundAudio.volume = Mathf.Lerp(start, targetVolume, t / fadeDuration);
            yield return null;
        }

        backgroundAudio.volume = targetVolume;
    }
}
