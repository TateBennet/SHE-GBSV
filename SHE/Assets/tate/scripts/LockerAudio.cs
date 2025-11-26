using UnityEngine;
using UnityEngine.Video;

public class LockerAudio : MonoBehaviour
{
    [Header("References")]
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    [Header("Clips")]
    public VideoClip videoClip;
    public AudioClip audioClip;

    [Header("Next Animation")]
    public Animator nextAnimator;
    public string nextAnimationName;

    [Header("Objects to Disable at End")]
    public GameObject[] objectsToDisable;
    public float disableDelay = 0f;

    [Header("UI To Enable When Audio Finishes")]
    [Tooltip("Any UI (e.g. locker UI) that should turn ON when the second animation runs.")]
    public GameObject[] uiToEnable;

    [Tooltip("If true, all UI in 'uiToEnable' will be forced OFF at startup.")]
    public bool hideUiOnStart = true;

    private bool hasTriggeredNext = false;
    private bool audioStarted = false;
    private bool wasPlaying = false;

    public GameObject keepObj;

    // 👉 Make sure UI starts hidden if we want it hidden
    private void Awake()
    {
        if (hideUiOnStart)
        {
            SetUiActive(false);
        }
    }

    public void Play()
    {
        hasTriggeredNext = false;
        audioStarted = false;
        wasPlaying = false;

        // 🎥 Play video
        if (videoPlayer != null && videoClip != null)
        {
            videoPlayer.clip = videoClip;
            videoPlayer.Play();
        }

        // 🔊 Play audio
        if (audioSource != null && audioClip != null)
        {
            audioSource.clip = audioClip;
            audioSource.Play();
            audioStarted = true;
        }
    }

    void Update()
    {
        if (audioSource == null) return;

        // Detect when it stops playing after being active
        if (wasPlaying && !audioSource.isPlaying)
        {
            OnAudioComplete();
            wasPlaying = false; // Prevent repeated triggers
        }
        else if (audioSource.isPlaying)
        {
            wasPlaying = true;
        }
    }

    private void OnAudioComplete()
    {
        if (hasTriggeredNext) return;   // safety, but doesn't change normal behavior
        hasTriggeredNext = true;

        Debug.Log("✅ Audio finished!");

        // 🎬 Trigger next animation (locker move / phone reach, etc.)
        if (nextAnimator != null && !string.IsNullOrEmpty(nextAnimationName))
            nextAnimator.Play(nextAnimationName);

        // 🧩 Enable locker UI (or other UI) at the SAME time as that second animation
        SetUiActive(true);

        // 🚫 Disable objects (if configured)
        if (objectsToDisable != null && objectsToDisable.Length > 0)
            Invoke(nameof(DisableObjects), disableDelay);
    }

    private void SetUiActive(bool value)
    {
        if (uiToEnable == null) return;

        foreach (var ui in uiToEnable)
        {
            if (ui != null)
                ui.SetActive(value);
        }
    }

    public void Unparent()
    {
        if (keepObj != null)
            keepObj.transform.SetParent(null, true); // Keep world position
    }

    private void DisableObjects()
    {
        foreach (GameObject obj in objectsToDisable)
            if (obj != null)
                obj.SetActive(false);

        Debug.Log("Objects disabled after audio finished.");
    }
}