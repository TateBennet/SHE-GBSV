using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class DuoCueSystem : MonoBehaviour
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
    [Tooltip("Reference to your ProVidMngr that manages both players.")]
    public DuoStreamPro videoManager;

    [Header("Per-video cue sets")]
    public List<ClipCues> perVideoCues = new List<ClipCues>();

    [Header("Timing / Robustness")]
    public double toleranceSeconds = 0.05;
    public bool resetOnLoopOrSeekBack = true;

    // runtime
    private VideoPlayer currentPlayer;
    private ClipCues _activeSet;
    private VideoClip _lastClip;
    private string _lastUrl;
    private double _lastTime;

    void Awake()
    {
        if (!videoManager)
        {
            videoManager = FindFirstObjectByType<DuoStreamPro>();

        }

        if (videoManager)
        {
            // Hook into the manager’s events
            videoManager.OnVideoChanged += HandleVideoChanged;
        }
    }

    void OnDestroy()
    {
        if (videoManager)
        {
            videoManager.OnVideoChanged -= HandleVideoChanged;
        }

        UnsubscribeFromCurrentPlayer();
    }

    void Start()
    {
        SwitchToActivePlayer();
        SelectActiveSetForCurrentVideo();
        ArmActiveCuesFrom(0);
        _lastTime = 0;
    }

    void Update()
    {
        if (!currentPlayer || !currentPlayer.isPrepared) return;

        // Detect clip/url change
        if (currentPlayer.clip != _lastClip || currentPlayer.url != _lastUrl)
        {
            SelectActiveSetForCurrentVideo();
            ArmActiveCuesFrom(0);
            _lastClip = currentPlayer.clip;
            _lastUrl = currentPlayer.url;
            _lastTime = 0;
        }

        double t = currentPlayer.time;

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
                bool crossed = (_lastTime <= target) && (t >= target - toleranceSeconds);

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
                        cue.armed = false;
                    }
                }
            }
        }

        _lastTime = t;
    }

    private void HandleVideoChanged()
    {
        // Fired by ProVidMngr whenever playback swaps
        SwitchToActivePlayer();
        SelectActiveSetForCurrentVideo();
        ArmActiveCuesFrom(0);
    }

    private void SwitchToActivePlayer()
    {
        UnsubscribeFromCurrentPlayer();

        if (videoManager == null)
        {
            Debug.LogWarning("TimedBallServe: Missing reference to ProVidMngr!");
            return;
        }

        currentPlayer = videoManager.GetActivePlayer();
        if (currentPlayer == null)
        {
            Debug.LogWarning("TimedBallServe: VideoManager has no active player reference!");
            return;
        }


        currentPlayer.loopPointReached += OnLoop;
        currentPlayer.prepareCompleted += OnPrepared;
    }

    private void UnsubscribeFromCurrentPlayer()
    {
        if (currentPlayer != null)
        {
            currentPlayer.loopPointReached -= OnLoop;
            currentPlayer.prepareCompleted -= OnPrepared;
        }
    }

    void OnLoop(VideoPlayer vp)
    {
        if (resetOnLoopOrSeekBack)
            ArmActiveCuesFrom(0);
    }

    void OnPrepared(VideoPlayer vp)
    {
        SelectActiveSetForCurrentVideo();
        ArmActiveCuesFrom(0);
    }

    void SelectActiveSetForCurrentVideo()
    {
        _activeSet = null;
        if (!currentPlayer) return;

        var currentClip = currentPlayer.clip;
        var currentUrl = currentPlayer.url ?? string.Empty;

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
}

