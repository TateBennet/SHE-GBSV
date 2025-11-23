using System;
using System.Collections;
using UnityEngine;

public class DuoStreamMusicCrossfader : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to your DuoStreamPro component in the scene.")]
    public DuoStreamPro duoStream;

    [Tooltip("Music that stays synced with the whole scene (starts silent).")]
    public AudioSource sceneMusic;

    [Tooltip("Music you hear at the beginning (fades out later).")]
    public AudioSource introMusic;

    [Header("Trigger Settings")]
    [Tooltip("Trigger crossfade when this video index becomes active. Use -1 to ignore index.")]
    public int triggerOnVideoIndex = -1;

    [Tooltip("OR trigger when the current video name CONTAINS this text (case-insensitive). Leave empty to ignore name match.")]
    public string triggerOnVideoNameContains = "";

    [Tooltip("Extra delay (in seconds) AFTER the target clip starts before beginning the crossfade.")]
    public float triggerDelayInVideo = 0f;

    [Header("Crossfade Settings")]
    [Tooltip("How long the fade between the two music tracks should take.")]
    public float fadeDuration = 5f;

    [Range(0f, 1f)]
    public float introStartVolume = 1f;

    [Range(0f, 1f)]
    public float sceneTargetVolume = 1f;

    [Tooltip("Use unscaled time (ignores Time.timeScale).")]
    public bool useUnscaledTime = true;

    private bool crossfadeStarted = false;

    void Start()
    {
        if (duoStream == null)
        {
            Debug.LogWarning("DuoStreamMusicCrossfader: No DuoStreamPro assigned.");
        }

        if (sceneMusic == null || introMusic == null)
        {
            Debug.LogWarning("DuoStreamMusicCrossfader: Please assign both AudioSources.");
            return;
        }

        // Set initial volumes
        introMusic.volume = introStartVolume;
        sceneMusic.volume = 0f;   // silent but playing to stay in sync

        // Make sure both tracks start from t = 0 (if you want full-scene sync)
        sceneMusic.time = 0f;
        introMusic.time = 0f;

        // Start both
        sceneMusic.Play();
        introMusic.Play();

        // Subscribe to DuoStream events
        if (duoStream != null)
        {
            // Use OnVideoStarted so we know playback actually began
            duoStream.OnVideoStarted += HandleVideoStarted;
        }
    }

    void OnDestroy()
    {
        if (duoStream != null)
        {
            duoStream.OnVideoStarted -= HandleVideoStarted;
        }
    }

    private void HandleVideoStarted()
    {
        if (crossfadeStarted || duoStream == null)
            return;

        int index = duoStream.GetCurrentVideoIndex();
        string name = duoStream.GetCurrentVideoName();

        bool indexMatches = (triggerOnVideoIndex >= 0 && index == triggerOnVideoIndex);
        bool nameMatches = !string.IsNullOrEmpty(triggerOnVideoNameContains) &&
                           !string.IsNullOrEmpty(name) &&
                           name.IndexOf(triggerOnVideoNameContains, StringComparison.OrdinalIgnoreCase) >= 0;

        if (indexMatches || nameMatches)
        {
            crossfadeStarted = true;
            StartCoroutine(CrossfadeRoutine());
        }
    }

    private IEnumerator CrossfadeRoutine()
    {
        // Optional delay INSIDE the target clip
        float delay = Mathf.Max(0f, triggerDelayInVideo);
        float elapsedDelay = 0f;
        while (elapsedDelay < delay)
        {
            elapsedDelay += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        // Now do the actual crossfade
        float elapsed = 0f;
        float introStart = introMusic.volume;
        float sceneStart = sceneMusic.volume;

        while (elapsed < fadeDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            introMusic.volume = Mathf.Lerp(introStart, 0f, t);
            sceneMusic.volume = Mathf.Lerp(sceneStart, sceneTargetVolume, t);

            yield return null;
        }

        // Snap to final values
        introMusic.volume = 0f;
        sceneMusic.volume = sceneTargetVolume;
    }
}