using System.Collections.Generic;
using System;
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
        [Header("Match rules (use either)")]
        [Tooltip("If set, cues apply when this exact VideoClip is playing.")]
        public VideoClip clip;

        [Tooltip("If clip is null, you can match by URL substring (for streaming/playlists). Leave empty for no URL match.")]
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

        // Detect clip/url change (e.g., playlist advancing)
        if (videoPlayer.clip != _lastClip || videoPlayer.url != _lastUrl)
        {
            SelectActiveSetForCurrentVideo();
            ArmActiveCuesFrom(0);
            _lastClip = videoPlayer.clip;
            _lastUrl = videoPlayer.url;
            _lastTime = 0;
        }

        double t = videoPlayer.time;

        // Detect seek backwards
        if (resetOnLoopOrSeekBack && t + 1e-6 < _lastTime)
            ArmActiveCuesFrom(t);

        // Fire cues
        if (_activeSet != null && _activeSet.cues != null)
        {
            foreach (var cue in _activeSet.cues)
            {
                if (!cue.armed) continue;

                double target = cue.nextFireTime;
                bool crossed =
                    (_lastTime <= target) && (t >= target - toleranceSeconds);

                if (crossed)
                {
                    try { cue.onCue?.Invoke(); }
                    catch (Exception e) { Debug.LogException(e, this); }

                    if (cue.repeat && cue.repeatInterval > 0)
                    {
                        cue.nextFireTime += cue.repeatInterval;
                    }
                    else
                    {
                        cue.armed = false; // one-shot
                    }
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
        // Some playlist controllers call Prepare on the next clip before play.
        // When the player switches, Update() will also detect it; this just
        // ensures cues are ready ASAP.
        SelectActiveSetForCurrentVideo();
        ArmActiveCuesFrom(0);
    }

    void SelectActiveSetForCurrentVideo()
    {
        _activeSet = null;
        var currentClip = videoPlayer.clip;
        var currentUrl = videoPlayer.url ?? string.Empty;

        foreach (var set in perVideoCues)
        {
            bool clipMatch = set.clip != null && set.clip == currentClip;
            bool urlMatch = !clipMatch && !string.IsNullOrEmpty(set.urlContains)
                             && currentUrl.IndexOf(set.urlContains, StringComparison.OrdinalIgnoreCase) >= 0;

            if (clipMatch || urlMatch)
            {
                _activeSet = set;
                break;
            }
        }
    }

    void ArmActiveCuesFrom(double currentTime)
    {
        if (_activeSet == null) return;

        foreach (var cue in _activeSet.cues)
        {
            cue.armed = true;

            if (cue.repeat && cue.repeatInterval > 0)
            {
                // Find next repeat time >= currentTime, starting from first timeSeconds
                if (currentTime <= cue.timeSeconds)
                {
                    cue.nextFireTime = cue.timeSeconds;
                }
                else
                {
                    var n = Math.Ceiling((currentTime - cue.timeSeconds) / cue.repeatInterval);
                    cue.nextFireTime = cue.timeSeconds + n * cue.repeatInterval;
                }
            }
            else
            {
                cue.nextFireTime = cue.timeSeconds;
                // If the one-shot cue time is already behind us after a seek, you can choose to:
                // - keep it armed (will fire if time goes backwards past it), or
                // - disarm it now. We'll keep it armed for simplicity.
            }
        }
    }
}
