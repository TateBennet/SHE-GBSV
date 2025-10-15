using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class VideoAudioSet
{
    public string name;
    public AudioClip[] clips;
}

[System.Serializable]
public class AudioSourcePositionSet
{
    public string name;
    public Vector3[] positions;
    //public Transform[] positions; for tranform based inspector

}

public class AudioMngr : MonoBehaviour
{
    [Header("References")]
    public VideoPlayer videoPlayer;
    public QuestVideoPlaylist playlistScript;
    public AudioSource[] audioSources;  // manually assigned, anywhere in the scene

    [Header("Audio Sets (clips for each video)")]
    public List<VideoAudioSet> videoAudioSets = new List<VideoAudioSet>();

    [Header("Source Positions (spatial layout per video)")]
    public List<AudioSourcePositionSet> sourcePositionSets = new List<AudioSourcePositionSet>();

    [Header("Sync")]
    public double dspLeadSeconds = 0.1;
    public float startDelay = 0.5f;

    private int currentVideoIndex = -1;
    private bool lastPlaying = false;
    private bool audioLoaded = false;
    private bool isStartingAudio = false;

    private void Start()
    {
        if (!videoPlayer)
        {
            Debug.LogError("AudioManager: No VideoPlayer assigned!");
            enabled = false;
            return;
        }

        foreach (var src in audioSources)
        {
            if (src == null) continue;
            src.playOnAwake = false;
            src.Stop();
        }

        Debug.Log("🎧 AudioManager initialized and listening for video changes.");
    }

    private void Update()
    {
        if (playlistScript == null || videoPlayer == null)
            return;

        int newIndex = playlistScript.GetCurrentIndex();

        // Detect new video
        if ((newIndex != currentVideoIndex && videoPlayer.isPrepared && videoPlayer.isPlaying) && !isStartingAudio)
        {
            currentVideoIndex = newIndex;
            Debug.Log($"🔄 Detected new video index {currentVideoIndex}, scheduling audio start...");
            StopAllAudio();
            StartCoroutine(PlayAudioDelayed(currentVideoIndex));
        }

        // Detect pause/resume
        if (lastPlaying && !videoPlayer.isPlaying)
        {
            PauseAllAudio();
        }
        else if (!lastPlaying && videoPlayer.isPlaying && audioLoaded)
        {
            ResumeAllAudio();
        }

        lastPlaying = videoPlayer.isPlaying;
    }

    private System.Collections.IEnumerator PlayAudioDelayed(int index)
    {
        isStartingAudio = true;
        yield return new WaitForSeconds(startDelay);

        RepositionAudioSources(index);
        PlayAudioSet(index);

        isStartingAudio = false;
        audioLoaded = true;
    }

    private void RepositionAudioSources(int index)
    {
        if (index < 0 || index >= sourcePositionSets.Count)
        {
            Debug.LogWarning($"AudioManager: No position set for video index {index}");
            return;
        }

        var set = sourcePositionSets[index];
        var positions = set.positions;

        for (int i = 0; i < audioSources.Length && i < positions.Length; i++)
        {
            if (audioSources[i] == null) continue;
            audioSources[i].transform.position = positions[i]; // ✅ world-space now
            Debug.Log($"📍 Moved {audioSources[i].name} to {positions[i]} for video {index}");
        }
    }

    //private void RepositionAudioSources(int index)
    //{
    //    if (index < 0 || index >= sourcePositionSets.Count)
    //    {
    //        Debug.LogWarning($"AudioManager: No position set for video index {index}");
    //        return;
    //    }

    //    var set = sourcePositionSets[index];
    //    var transforms = set.positions;

    //    for (int i = 0; i < audioSources.Length && i < transforms.Length; i++)
    //    {
    //        if (audioSources[i] == null || transforms[i] == null) continue;

    //        audioSources[i].transform.position = transforms[i].position;
    //        audioSources[i].transform.rotation = transforms[i].rotation; // optional: match facing direction
    //        Debug.Log($"📍 {audioSources[i].name} moved to {transforms[i].name}");
    //    }
    //}

    private void PlayAudioSet(int index)
    {
        if (index < 0 || index >= videoAudioSets.Count)
        {
            Debug.LogWarning($"AudioManager: No audio set for index {index}");
            return;
        }

        var set = videoAudioSets[index];
        var clips = set.clips;

        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"AudioManager: Empty audio set for video {index}");
            return;
        }

        double dspStart = AudioSettings.dspTime + dspLeadSeconds;
        Debug.Log($"🎵 Scheduling {clips.Length} clips for playback on video {index}");

        for (int i = 0; i < audioSources.Length && i < clips.Length; i++)
        {
            var src = audioSources[i];
            var clip = clips[i];
            if (src == null || clip == null) continue;

            src.clip = clip;
            src.PlayScheduled(dspStart);
            Debug.Log($"▶️ Playing clip {clip.name} on AudioSource {i}");
        }
    }

    private void PauseAllAudio()
    {
        foreach (var src in audioSources)
        {
            if (src != null && src.isPlaying)
                src.Pause();
        }
        Debug.Log("⏸️ Audio paused");
    }

    private void ResumeAllAudio()
    {
        foreach (var src in audioSources)
        {
            if (src != null && src.clip != null && !src.isPlaying)
                src.UnPause();
        }
        Debug.Log("▶️ Audio resumed");
    }

    private void StopAllAudio()
    {
        foreach (var src in audioSources)
        {
            if (src == null) continue;
            src.Stop();
            src.clip = null;
        }
        Debug.Log("🛑 Audio stopped");
    }
}
