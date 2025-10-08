using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class TimedBallServe : MonoBehaviour
{
    [Serializable]
    public class Cue
    {
        [Tooltip("Time in seconds in the video to fire this cue.")]
        public double timeSeconds = 1.0;

        [Tooltip("What to invoke when the playhead reaches this cue.")]
        public UnityEvent onCue;

        [Header("Repeat (optional)")]
        public bool repeat = false;
        public double repeatInterval = 1.0;

        // runtime
        [NonSerialized] public bool armed;
        [NonSerialized] public double nextFireTime;
    }

    [Serializable]
    public class ClipCues
    {
        [Header("Match rules (prefer URL filename match)")]
        [Tooltip("If set, cues apply when this exact VideoClip is playing (useful only if you use VideoClip assets).")]
        public VideoClip clip;

        [Tooltip("Match by URL substring or filename. Example: 'volleyballscenev2b1.mp4'")]
        public string urlContains;

        [Header("Cues for this video")]
        public List<Cue> cues = new List<Cue>();
    }

    [Header("References")]
    public VideoPlayer videoPlayer;

    [Header("Per-video cue sets")]
    public List<ClipCues> perVideoCues = new List<ClipCues>();

    [Header("Timing / Robustness")]
    [Tooltip("Time window to catch a cue in case of frame drops or time jumps.")]
    public double toleranceSeconds = 0.05;

    [Tooltip("Re-arm cues when the clip loops or when time jumps backwards.")]
    public bool resetOnLoopOrSeekBack = true;

    // runtime
    ClipCues _activeSet;
    VideoClip _lastClip;
    string _lastUrl;
    double _lastTime;

    void Awake()
    {
        if (!videoPlayer) videoPlayer = FindObjectOfType<VideoPlayer>();
        if (videoPlayer)
        {
            videoPlayer.loopPointReached += OnLoop;
            videoPlayer.prepareCompleted += OnPrepared;
        }
        else
        {
            Debug.LogWarning("[TimedBallServe] No VideoPlayer found.");
        }
    }

    void OnDestroy()
    {
        if (videoPlayer)
        {
            videoPlayer.loopPointReached -= OnLoop;
            videoPlayer.prepareCompleted -= OnPrepared;
        }
    }

    void Start()
    {
        SelectActiveSetForCurrentVideo();
        ArmActiveCuesFrom(0);
        _lastTime = 0;
    }

    void Update()
    {
        if (!videoPlayer || !videoPlayer.isPrepared) return;

        // Detect clip/url change (playlist advancing, streaming URL switching)
        if (videoPlayer.clip != _lastClip || videoPlayer.url != _lastUrl)
        {
            SelectActiveSetForCurrentVideo();
            ArmActiveCuesFrom(0);
            _lastClip = videoPlayer.clip;
            _lastUrl = videoPlayer.url;
            _lastTime = 0;
        }

        double t = videoPlayer.time;

        // Detect seek backwards / loop
        if (resetOnLoopOrSeekBack && t + 1e-6 < _lastTime)
            ArmActiveCuesFrom(t);

        // Fire cues
        if (_activeSet != null && _activeSet.cues != null)
        {
            foreach (var cue in _activeSet.cues)
            {
                if (!cue.armed) continue;

                double target = cue.nextFireTime;
                bool crossed = (_lastTime <= target) && (t >= target - toleranceSeconds);

                if (crossed)
                {
                    try { cue.onCue?.Invoke(); }
                    catch (Exception e) { Debug.LogException(e, this); }

                    if (cue.repeat && cue.repeatInterval > 0)
                        cue.nextFireTime += cue.repeatInterval;
                    else
                        cue.armed = false; // one-shot
                }
            }
        }

        _lastTime = t;
    }

    void OnLoop(VideoPlayer vp)
    {
        if (resetOnLoopOrSeekBack)
            ArmActiveCuesFrom(0);
    }

    void OnPrepared(VideoPlayer vp)
    {
        // Ensure cues are ready as soon as the clip is prepared
        SelectActiveSetForCurrentVideo();
        ArmActiveCuesFrom(0);
    }

    void SelectActiveSetForCurrentVideo()
    {
        _activeSet = null;
        var currentClip = videoPlayer ? videoPlayer.clip : null;
        var currentUrl = videoPlayer && videoPlayer.url != null ? videoPlayer.url : string.Empty;

        string urlLower = currentUrl.ToLowerInvariant();
        string fileNameLower = ExtractLowerFileName(currentUrl);

        foreach (var set in perVideoCues)
        {
            bool clipMatch = set.clip != null && set.clip == currentClip;

            bool urlMatch = false;
            if (!clipMatch && !string.IsNullOrEmpty(set.urlContains))
            {
                string needle = set.urlContains.ToLowerInvariant();
                urlMatch = urlLower.Contains(needle) || fileNameLower.Contains(needle);
            }

            if (clipMatch || urlMatch)
            {
                _activeSet = set;
                Debug.Log($"[TimedBallServe] Active cue set selected. clipMatch={clipMatch}, urlMatch={urlMatch}, url='{currentUrl}', file='{fileNameLower}'");
                return;
            }
        }

        Debug.LogWarning($"[TimedBallServe] No matching cue set found. url='{currentUrl}', file='{fileNameLower}'. " +
                         $"Add a perVideoCues entry with urlContains like '{fileNameLower}'.");
    }

    void ArmActiveCuesFrom(double currentTime)
    {
        if (_activeSet == null) return;

        foreach (var cue in _activeSet.cues)
        {
            cue.armed = true;

            if (cue.repeat && cue.repeatInterval > 0)
            {
                if (currentTime <= cue.timeSeconds)
                    cue.nextFireTime = cue.timeSeconds;
                else
                {
                    var n = Math.Ceiling((currentTime - cue.timeSeconds) / cue.repeatInterval);
                    cue.nextFireTime = cue.timeSeconds + n * cue.repeatInterval;
                }
            }
            else
            {
                cue.nextFireTime = cue.timeSeconds;
            }
        }
    }

    static string ExtractLowerFileName(string url)
    {
        try
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;
            if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                url = url.Substring("file://".Length);

            url = Uri.UnescapeDataString(url);
            return Path.GetFileName(url).ToLowerInvariant();
        }
        catch { return string.Empty; }
    }
}
