using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VideoAudioGroup
{
    [Tooltip("Name of the video this set belongs to (match loosely to the video file name).")]
    public string videoName;

    [Tooltip("Audio sources to enable when this video plays.")]
    public List<AudioSource> audioSources = new List<AudioSource>();
}

public class ProAudioMngr : MonoBehaviour
{
    [Header("References")]
    public ProVidMngr videoStreamManager;

    [Header("Audio Groups")]
    public List<VideoAudioGroup> videoAudioGroups = new List<VideoAudioGroup>();

    [Header("Auto-Sync Settings")]
    [Tooltip("How long to actively correct timing at video start.")]
    public float syncWindowDuration = 0.5f;
    [Tooltip("Max allowed mismatch before correction (seconds).")]
    public float syncTolerance = 0.03f;

    private List<AudioSource> activeSources = new List<AudioSource>();
    private bool syncing = false;
    private float syncTimer = 0f;

    void Start()
    {
        if (videoStreamManager == null)
        {
            Debug.LogWarning("AudioManager: No VideoStreamManager assigned.");
            return;
        }

        videoStreamManager.OnVideoChanged += UpdateAudioForCurrentVideo;
        videoStreamManager.OnVideoPaused += PauseAllAudio;
        videoStreamManager.OnVideoResumed += ResumeAllAudio;
        videoStreamManager.OnVideoRestarted += RestartAudio;
        videoStreamManager.OnVideoSeeked += RestartAudio;
    }

    public void UpdateAudioForCurrentVideo()
    {
        foreach (var src in activeSources)
            if (src) src.gameObject.SetActive(false);

        activeSources.Clear();

        string currentName = NormalizeName(videoStreamManager.GetCurrentVideoName());
        if (string.IsNullOrEmpty(currentName)) return;

        VideoAudioGroup group = videoAudioGroups.Find(g =>
            NormalizeName(g.videoName) == currentName);

        if (group != null)
        {
            foreach (var src in group.audioSources)
            {
                if (src)
                {
                    src.gameObject.SetActive(true);
                    src.Play();
                    activeSources.Add(src);
                }
            }

            // Begin short sync window
            syncing = true;
            syncTimer = 0f;
        }
        else
        {
            Debug.Log($"No audio group found for video '{currentName}'");
        }
    }

    void Update()
    {
        if (!syncing || videoStreamManager == null || !videoStreamManager.IsPlaying())
            return;

        double videoTime = videoStreamManager.videoPlayer.time;
        syncTimer += Time.unscaledDeltaTime;

        foreach (var src in activeSources)
        {
            if (src && src.clip != null && src.isPlaying)
            {
                float diff = (float)(videoTime - src.time);
                if (Mathf.Abs(diff) > syncTolerance)
                    src.time = (float)videoTime;
            }
        }

        if (syncTimer >= syncWindowDuration)
            syncing = false;
    }

    public void PauseAllAudio()
    {
        foreach (var src in activeSources)
            if (src && src.isPlaying)
                src.Pause();
    }

    public void ResumeAllAudio()
    {
        foreach (var src in activeSources)
            if (src && !src.isPlaying)
                src.UnPause();
    }

    public void RestartAudio()
    {
        foreach (var src in activeSources)
        {
            if (src)
            {
                src.Stop();
                src.time = (float)videoStreamManager.videoPlayer.time;
                src.Play();
            }
        }
    }

    private string NormalizeName(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.Trim().ToLower().Replace("_", "").Replace(" ", "");
    }
}
