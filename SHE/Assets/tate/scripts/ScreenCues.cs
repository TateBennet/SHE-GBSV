using System.IO;
using System;
using UnityEngine;
using UnityEngine.Video;

public class ScreenCues : MonoBehaviour
{
    [Header("Phone Screen")]
    public Renderer phoneRenderer;
    public int screenMaterialIndex = 1;
    public Texture homeTexture;

    [Header("Video")]
    public VideoPlayer video;                // uses .url (streamed from disk/http)
    public float pollInterval = 0.06f;

    [Header("External video controller (optional)")]
    public StreamVideos videoManager;        // has PlayNext()

    [Header("Cues (match by file name without extension)")]
    public Cue[] cues;

    public enum CueType { ShowScreen, Pause, Resume, NextVideo }

    [Serializable]
    public class Cue
    {
        [Tooltip("File name without extension, e.g. 'scene1' for scene1.mp4")]
        public string clipKey;

        [Tooltip("Time (seconds) within that clip to trigger this cue.")]
        public double timeSeconds;

        public CueType type = CueType.ShowScreen;

        [Header("ShowScreen fields")]
        public Texture screenTexture;
        public Collider triggerCollider;     // enabled for this step

        [Header("NextVideo fields")]
        public float nextVideoDelay = 0f;    // optional delay before PlayNext()
    }

    // --- internal ---
    Material _screenMat;
    float _nextPoll;
    string _currentClipKey = "";
    bool[] _fired;                           // each cue triggers once
    int _activeScreenCue = -1;               // -1 = none
    bool _pausedByCue = false;               // only resume if we paused it here
    bool _preparedOnce = false;

    void Awake()
    {
        if (!phoneRenderer || !video)
        {
            Debug.LogError("PhoneCueTimelineByUrl: Assign phoneRenderer and video.");
            enabled = false; return;
        }

        var mats = phoneRenderer.materials;
        if (screenMaterialIndex < 0 || screenMaterialIndex >= mats.Length)
        {
            Debug.LogError("PhoneCueTimelineByUrl: screenMaterialIndex out of range.");
            enabled = false; return;
        }
        _screenMat = mats[screenMaterialIndex];

        _fired = new bool[cues.Length];
        foreach (var c in cues)
            if (c != null && c.triggerCollider) c.triggerCollider.enabled = false;

        SetTexture(homeTexture);

        video.prepareCompleted += OnPrepared;
        video.loopPointReached += OnLoopOrEnd;
    }

    void OnDestroy()
    {
        if (video != null)
        {
            video.prepareCompleted -= OnPrepared;
            video.loopPointReached -= OnLoopOrEnd;
        }
    }

    void OnPrepared(VideoPlayer vp)
    {
        _preparedOnce = true;
        _currentClipKey = ExtractClipKey(vp.url);
        ResetActiveScreen();
        SetTexture(homeTexture);
        // Don’t automatically unfire cues here; they’re per run unless you want replay behavior.
    }

    void OnLoopOrEnd(VideoPlayer vp)
    {
        ResetActiveScreen();
        SetTexture(homeTexture);
        _pausedByCue = false;
    }

    void Update()
    {
        if (!video) return;
        if (Time.time < _nextPoll) return;
        _nextPoll = Time.time + pollInterval;

        if (!_preparedOnce)
            _currentClipKey = ExtractClipKey(video.url);

        // when paused, time won't advance, but we still allow button taps, and cue polling is harmless
        double t = video.time;

        // if a ShowScreen cue is active, wait for button tap
        if (_activeScreenCue >= 0) return;

        // find the next unfired cue that matches current clipKey and time
        for (int i = 0; i < cues.Length; i++)
        {
            if (_fired[i]) continue;
            var cue = cues[i];
            if (!ClipKeyEquals(cue.clipKey, _currentClipKey)) continue;
            if (t >= cue.timeSeconds)
            {
                TriggerCue(i);
                break;
            }
        }
    }

    void TriggerCue(int index)
    {
        var cue = cues[index];
        _fired[index] = true;

        switch (cue.type)
        {
            case CueType.ShowScreen:
                SetTexture(cue.screenTexture);
                if (cue.triggerCollider) cue.triggerCollider.enabled = true;
                _activeScreenCue = index;
                break;

            case CueType.Pause:
                if (video.isPlaying)
                {
                    video.Pause();
                    _pausedByCue = true;
                }
                break;

            case CueType.Resume:
                ResumeVideoIfPaused();
                break;

            case CueType.NextVideo:
                if (videoManager != null)
                {
                    if (cue.nextVideoDelay > 0f)
                        Invoke(nameof(CallNextVideo), cue.nextVideoDelay);
                    else
                        CallNextVideo();
                }
                break;
        }
    }

    void CallNextVideo()
    {
        videoManager?.PlayNext();
    }

    void ResetActiveScreen()
    {
        if (_activeScreenCue >= 0)
        {
            var cue = cues[_activeScreenCue];
            if (cue.triggerCollider) cue.triggerCollider.enabled = false;
            _activeScreenCue = -1;
        }
    }

    void SetTexture(Texture tex)
    {
        if (_screenMat == null) return;
        _screenMat.SetTexture("_BaseMap", tex);
        _screenMat.SetTexture("_MainTex", tex);
    }

    static string ExtractClipKey(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        try
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            string fileName = uri.IsAbsoluteUri ? Path.GetFileName(uri.LocalPath) : Path.GetFileName(url);
            return Path.GetFileNameWithoutExtension(fileName) ?? "";
        }
        catch
        {
            string fileName = Path.GetFileName(url);
            return Path.GetFileNameWithoutExtension(fileName) ?? "";
        }
    }

    static bool ClipKeyEquals(string a, string b)
    {
        return string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    // --- Public API for buttons ---

    /// Called by a ShowScreen collider when tapped.
    /// If returnToHome==true, we switch to home texture; otherwise we keep the screen up.
    /// Also resumes the video if it was paused by a Pause cue (or by showing a cue, if you add that).
    public void NotifyScreenTapped(Collider pressed, bool returnToHome)
    {
        if (_activeScreenCue < 0) return;

        var cue = cues[_activeScreenCue];
        if (cue.triggerCollider != pressed) return;

        // disable collider for this screen cue
        if (cue.triggerCollider) cue.triggerCollider.enabled = false;
        _activeScreenCue = -1;

        if (_pausedByCue) ResumeVideoIfPaused();

        if (returnToHome) SetTexture(homeTexture);
    }

    /// Can be called by any button/cue to resume if paused by a Pause cue.
    public void ResumeVideoIfPaused()
    {
        if (_pausedByCue && video != null)
        {
            video.Play();
            _pausedByCue = false;
        }
    }
}
