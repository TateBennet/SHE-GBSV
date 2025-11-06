using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Video;

[System.Serializable]
public class DuoVideoAudioGroup
{
    [Tooltip("Name of the video this set belongs to (match loosely to the video file name).")]
    public string videoName;
    [Tooltip("Audio sources to play when this video plays.")]
    public List<AudioSource> audioSources = new List<AudioSource>();
}

public class DuoAudioManager : MonoBehaviour
{
    [Header("References")]
    public DuoStreamPro videoStreamManager;

    [Header("Audio Groups")]
    public List<DuoVideoAudioGroup> duoVideoAudioGroups = new List<DuoVideoAudioGroup>();

    [Header("Sync Settings")]
    [Tooltip("Maximum allowed drift before correcting (seconds).")]
    public float syncTolerance = 0.03f;

    private List<AudioSource> activeSources = new List<AudioSource>();
    private VideoPlayer currentPlayer;
    private DuoVideoAudioGroup _pendingGroup;

    private bool syncingActive = false;

    void Start()
    {
        if (!videoStreamManager)
        {
            Debug.LogWarning("DuoAudioManager: Missing DuoStreamPro reference!");
            return;
        }

        videoStreamManager.OnVideoChanged += HandleVideoChanged;
        videoStreamManager.OnVideoPaused += PauseAll;

        currentPlayer = videoStreamManager.GetActivePlayer();
        if (currentPlayer) HandleVideoChanged();
    }

    private void OnDestroy()
    {
        if (!videoStreamManager) return;

        videoStreamManager.OnVideoChanged -= HandleVideoChanged;
        videoStreamManager.OnVideoPaused -= PauseAll;
    }

    private void HandleVideoChanged()
    {
        currentPlayer = videoStreamManager.GetActivePlayer();

        foreach (var src in activeSources)
            if (src) src.Stop();
        activeSources.Clear();

        string name = Normalize(videoStreamManager.GetCurrentVideoName());
        if (string.IsNullOrEmpty(name)) return;

        var group = duoVideoAudioGroups.Find(g => Normalize(g.videoName) == name);
        if (group == null)
        {
            Debug.Log($"DuoAudioManager: No audio group found for video '{name}'");
            return;
        }

        _pendingGroup = group;
        StartAudioThenVideo();
    }

    private void StartAudioThenVideo()
    {
        if (_pendingGroup == null || currentPlayer == null)
        {
            Debug.LogWarning("No pending audio group or player when trying to start playback.");
            return;
        }

        // 🔊 1. Schedule audio precisely on DSP clock
        double dspNow = AudioSettings.dspTime;
        double scheduledStart = dspNow + 0.1; // small buffer

        foreach (var src in _pendingGroup.audioSources)
        {
            if (!src) continue;
            src.Stop();
            src.time = 0f;
            src.PlayScheduled(scheduledStart);
            activeSources.Add(src);
        }

        // 🎥 2. Start video immediately (it will catch up)
        currentPlayer.Play();

        // 3. Begin sync loop
        syncingActive = true;
        Debug.Log($"🎬 Audio scheduled at {scheduledStart:F3}s DSP; Sync enabled.");
    }

    private void PauseAll()
    {
        foreach (var src in activeSources)
            if (src && src.isPlaying)
                src.Pause();
    }

    private void ResumeAll()
    {
        foreach (var src in activeSources)
            if (src && !src.isPlaying)
                src.UnPause();
    }

    private void RestartAll()
    {
        foreach (var src in activeSources)
        {
            if (!src) continue;
            src.Stop();
            src.time = 0f;
            src.Play();
        }
        syncingActive = true;
    }

    void Update()
    {
        if (!syncingActive || currentPlayer == null || activeSources.Count == 0) return;

        // just use the first audio source as master
        var src = activeSources[0];
        if (!src || !src.isPlaying) return;

        double videoTime = currentPlayer.time;
        double audioTime = src.time;   // this exists per AudioSource

        float drift = (float)(videoTime - audioTime);

        if (Mathf.Abs(drift) > syncTolerance)
        {
            currentPlayer.time = Mathf.Clamp((float)audioTime, 0f, (float)currentPlayer.length);
            currentPlayer.Play();
            Debug.Log($"🔧 Corrected drift: {drift:F3}s");
        }
        else
        {
            syncingActive = false;
            Debug.Log($"✅ Locked sync (drift {drift:F3}s)");
        }
    }

    private string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Trim().ToLower().Replace("_", "").Replace(" ", "");
    }
}
