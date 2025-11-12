using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class DuoVideoAudioGroup
{
    [Tooltip("Match to the current video name (loose match is okay).")]
    public string videoName;
    [Tooltip("Spatialized audio sources to play with this video.")]
    public List<AudioSource> audioSources = new List<AudioSource>();
}

public class DuoAudioManager : MonoBehaviour
{
    [Header("References")]
    public DuoStreamPro videoStreamManager;

    [Header("Audio Groups")]
    public List<DuoVideoAudioGroup> duoVideoAudioGroups = new List<DuoVideoAudioGroup>();

    [Header("Sync Start")]
    [Tooltip("Lead-in buffer before starting (DSP seconds). 0.10–0.20 is typical.")]
    public double startLeadSeconds = 0.12;

    private List<AudioSource> activeSources = new List<AudioSource>();
    private VideoPlayer currentPlayer;
    private DuoVideoAudioGroup _pendingGroup;

    void Start()
    {
        if (!videoStreamManager)
        {
            Debug.LogWarning("DuoAudioManager: Missing DuoStreamPro reference!");
            return;
        }

        // Wire to the new event: video fully prepared & paused on frame 0
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

        // Stop old audio
        foreach (var s in activeSources)
            if (s) s.Stop();
        activeSources.Clear();

        // Resolve group by current video name
        string key = Normalize(videoStreamManager.GetCurrentVideoName());
        _pendingGroup = duoVideoAudioGroups.Find(g => Normalize(g.videoName) == key);
    }

    private void HandlePreparedAndPaused()
    {
        if (currentPlayer == null || _pendingGroup == null)
        {
            Debug.Log("DuoAudioManager: No player or group when prepared event fired.");
            return;
        }

        // Arm audio, do not play yet
        foreach (var s in _pendingGroup.audioSources)
        {
            if (!s) continue;
            s.Stop();
            s.time = 0f;
            activeSources.Add(s);
        }

        // Compute joint start time on DSP clock
        double dspNow = AudioSettings.dspTime;

        // Start audio a little later so the video has a small head start
        double videoStart = dspNow + 0.05;   // video begins decoding now
        double audioStart = dspNow + 0.08;   // audio starts 0.2s later

        // Schedule both
        videoStreamManager.BeginActiveAtDSP(videoStart);
        foreach (var s in activeSources)
            s.PlayScheduled(audioStart);

    }

    private void HandleVideoPaused()
    {
        foreach (var s in activeSources)
        {
            if (s && s.isPlaying)
                s.Pause();
        }
    }

    private void HandleVideoResumed()
    {
        foreach (var s in activeSources)
        {
            if (s && !s.isPlaying)
                s.UnPause();
        }
    }

    private string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Trim().ToLower().Replace("_", "").Replace(" ", "");
    }
}
