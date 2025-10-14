using UnityEngine;

public class PhoneButtonCue : MonoBehaviour
{
    [Header("References")]
    public ScreenCues manager;
    public StreamVideos videoManager;      // optional, only used if callNextVideo = true
    public string handTag = "pointer";

    [Header("For ShowScreen cues")]
    public bool returnToHome = true;       // if false, keep the cue screen visible

    [Header("Optional extras")]
    public bool resumeVideoOnTap = false;  // resume if a Pause cue paused it
    public bool callNextVideo = false;     // call StreamVideos.PlayNext() after delay
    public float nextVideoDelay = 2f;

    Collider _col;
    bool _triggered = false;

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col) _col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag(handTag)) return;
        _triggered = true;

        // Tell manager: this was the tap for the active ShowScreen cue (if any)
        manager?.NotifyScreenTapped(_col, returnToHome);

        if (resumeVideoOnTap)
            manager?.ResumeVideoIfPaused();

        if (callNextVideo && videoManager != null)
            Invoke(nameof(CallNext), nextVideoDelay);
    }

    void CallNext()
    {
        videoManager.PlayNext();
    }
}