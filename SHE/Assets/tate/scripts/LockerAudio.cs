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

    private bool hasTriggeredNext = false;
    private bool audioStarted = false;
    private bool wasPlaying = false;

    public GameObject keepObj;

    public void Play()
    {
        hasTriggeredNext = false;
        audioStarted = false;

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
        Debug.Log("✅ Audio finished!");

        // 🎬 Trigger next animation
        if (nextAnimator != null && !string.IsNullOrEmpty(nextAnimationName))
            nextAnimator.Play(nextAnimationName);

        // 🚫 Disable objects
        if (objectsToDisable != null && objectsToDisable.Length > 0)
            Invoke(nameof(DisableObjects), disableDelay);
    }

    public void Unparent()
    {
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