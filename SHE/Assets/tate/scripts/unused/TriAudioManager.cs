using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class TriVideoAudioGroup
{
    public string videoName;
    public List<AudioSource> audioSources = new List<AudioSource>();
}

public class TriAudioManager : MonoBehaviour
{
    [Header("References")]
    public TriStreamPro videoStreamManager;

    [Header("Audio Groups")]
    public List<TriVideoAudioGroup> duoVideoAudioGroups = new List<TriVideoAudioGroup>();

    [Header("Sync Start")]
    [Tooltip("Lead-in buffer before audio start (DSP seconds). 0.10–0.20 typical.")]
    public double startLeadSeconds = 0.12;

    [Tooltip("Start video this many seconds BEFORE audio (DSP seconds).")]
    public double videoHeadStartSeconds = 0.03;

    private List<AudioSource> activeSources = new List<AudioSource>();
    private VideoPlayer currentPlayer;
    private TriVideoAudioGroup _pendingGroup;

    void Start()
    {
        if (!videoStreamManager)
        {
            Debug.LogWarning("TriAudioManager: Missing TriStreamPro reference!");
            return;
        }

        videoStreamManager.OnVideoChanged += HandleVideoChanged;
        videoStreamManager.OnVideoPreparedAndPaused += HandlePreparedAndPaused;
        videoStreamManager.OnVideoPaused += HandleVideoPaused;
        videoStreamManager.OnVideoResumed += HandleVideoResumed;

        currentPlayer = videoStreamManager.GetActivePlayer();
    }

    private void OnDestroy()
    {
        if (!videoStreamManager) return;

        videoStreamManager.OnVideoChanged -= HandleVideoChanged;
        videoStreamManager.OnVideoPreparedAndPaused -= HandlePreparedAndPaused;
        videoStreamManager.OnVideoPaused -= HandleVideoPaused;
        videoStreamManager.OnVideoResumed -= HandleVideoResumed;
    }

    private void HandleVideoChanged()
    {
        currentPlayer = videoStreamManager.GetActivePlayer();

        foreach (var s in activeSources)
            if (s) s.Stop();
        activeSources.Clear();

        string key = Normalize(videoStreamManager.GetCurrentVideoName());

        // exact normalized match
        _pendingGroup = duoVideoAudioGroups.Find(g => Normalize(g.videoName) == key);
    }

    private void HandlePreparedAndPaused()
    {
        if (currentPlayer == null || _pendingGroup == null)
        {
            Debug.Log("TriAudioManager: No player or group when prepared event fired.");
            return;
        }

        // NEW: match audio start time to the video's start offset
        double offset = videoStreamManager.GetCurrentStartOffsetSeconds();

        // Arm audio (don’t play yet)
        foreach (var s in _pendingGroup.audioSources)
        {
            if (!s) continue;

            s.Stop();

            // Apply the same offset to every source in the group.
            // Clamp defensively to avoid errors if offset exceeds clip length.
            if (s.clip != null)
            {
                float maxT = Mathf.Max(0f, s.clip.length - 0.01f);
                if (s.clip != null)
                {
                    int samples = Mathf.Clamp(
                        (int)(offset * s.clip.frequency),
                        0,
                        Mathf.Max(0, s.clip.samples - 1)
                    );
                    s.timeSamples = samples;
                }
            }
            else
            {
                s.time = 0f;
            }

            activeSources.Add(s);
        }

        double dspNow = AudioSettings.dspTime;

        // Schedule a unified start
        double audioStart = dspNow + startLeadSeconds;
        double videoStart = audioStart - Mathf.Max(0f, (float)videoHeadStartSeconds);

        videoStreamManager.BeginActiveAtDSP(videoStart);
        foreach (var s in activeSources)
            s.PlayScheduled(audioStart);
    }

    private void HandleVideoPaused()
    {
        foreach (var s in activeSources)
            if (s && s.isPlaying) s.Pause();
    }

    private void HandleVideoResumed()
    {
        foreach (var s in activeSources)
            if (s && !s.isPlaying) s.UnPause();
    }

    private string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Trim().ToLower().Replace("_", "").Replace(" ", "");
    }
}
