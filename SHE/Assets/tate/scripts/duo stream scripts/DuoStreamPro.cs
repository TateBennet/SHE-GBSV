using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class DuoStreamPro : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Primary video sphere/player (starts first).")]
    public VideoPlayer playerA;
    [Tooltip("Secondary video sphere/player (used to preload next video).")]
    public VideoPlayer playerB;

    [Header("Video Files (assign manually in Inspector)")]
    public List<string> videoFiles = new List<string>();

    [Header("Targets (360 sphere renderers)")]
    public Renderer sphereA;
    public Renderer sphereB;

    [Header("State (read-only)")]
    [SerializeField] private int currentVideoIndex = -1;
    [SerializeField] private string currentVideoName = "";

    // Which set is active
    public bool usingA = true;

    // Events
    /// <summary> Fired when the active player has prepared and is paused on frame 0 (ready to start). </summary>
    public event Action OnVideoPreparedAndPaused;
    /// <summary> Fired when the active player actually begins playback. </summary>
    public event Action OnVideoStarted;
    public event Action OnVideoPaused;
    public event Action OnVideoResumed;
    public event Action OnVideoRestarted;
    public event Action OnVideoSeeked;
    public event Action OnVideoChanged;

    // Convenience
    public VideoPlayer GetActivePlayer() => usingA ? playerA : playerB;
    public VideoPlayer GetStandbyPlayer() => usingA ? playerB : playerA;
    public Renderer GetActiveSphere() => usingA ? sphereA : sphereB;
    public Renderer GetStandbySphere() => usingA ? sphereB : sphereA;

    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        EnsureMediaPermission();
#endif
        // Reduce initial latency on Android
        if (playerA) { playerA.waitForFirstFrame = false; playerA.skipOnDrop = true; }
        if (playerB) { playerB.waitForFirstFrame = false; playerB.skipOnDrop = true; }
    }

    private void Start()
    {
        if (videoFiles.Count > 0)
        {
            // Prepare first video fully and pause on frame 0
            StartCoroutine(PrepareActivateAndPause(0, /*makeActive*/ true));
            // Also start preloading the next, if any
            if (videoFiles.Count > 1)
                PreloadVideo(1);
        }
    }

    /// <summary>
    /// Prepares the given index on the active path (or swaps to it), binds texture, enables correct sphere,
    /// pauses on frame 0, and fires OnVideoPreparedAndPaused.
    /// </summary>
    private IEnumerator PrepareActivateAndPause(int index, bool makeActive)
    {
        if (index < 0 || index >= videoFiles.Count) yield break;

        // Decide which player is becoming active for this index
        if (makeActive)
        {
            // If the currently "active" slot isn't the one we want, we still load into ActivePlayer
            // by ensuring usingA flag lines up with which sphere we want to show.
            // We'll load URL into ActivePlayer (as returned below).
        }

        var activePlayer = GetActivePlayer();
        var activeSphere = GetActiveSphere();

        // Load URL into the active player
        activePlayer.Stop();
        activePlayer.url = videoFiles[index];
        activePlayer.Prepare();

        // Wait until prepared
        const float timeout = 45f;
        float t = 0f;
        // Wait until prepared
        // Wait until prepared
        while (!activePlayer.isPrepared && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!activePlayer.isPrepared)
        {
            Debug.LogWarning($"PrepareActivateAndPause: timed out preparing {videoFiles[index]}");
            yield break;
        }

        // Bind texture and show correct sphere
        TryBindTextureTo(activePlayer, activeSphere);
        activeSphere.enabled = true;
        GetStandbySphere().enabled = false;

        // 🧠 Give the decoder time to fill its internal buffer
        yield return new WaitForSecondsRealtime(0.25f);

        // Pause and force frame 0 to be ready and visible
        activePlayer.Pause();
        activePlayer.frame = 0;

        // Allow the renderer one more frame to update
        yield return null;

        // Notify that we're fully prepared and paused
        usingA = (activePlayer == playerA);
        currentVideoIndex = index;
        currentVideoName = Path.GetFileNameWithoutExtension(videoFiles[index]);
        OnVideoChanged?.Invoke();
        OnVideoPreparedAndPaused?.Invoke();



        usingA = (activePlayer == playerA);
        currentVideoIndex = index;
        currentVideoName = Path.GetFileNameWithoutExtension(videoFiles[index]);

        // Notify systems that we are fully prepared and paused on frame 0
        OnVideoChanged?.Invoke();

        // 🔁 Start preloading the next video while this one is paused/ready
        int nextIndex = (index + 1) % videoFiles.Count;
        if (videoFiles.Count > 1)
            StartCoroutine(PreloadOnStandby(nextIndex));


        OnVideoPreparedAndPaused?.Invoke();

    }

    private int? standbyPreparingIndex = null;
    private Coroutine preloadRoutine;

    public void PreloadVideo(int index)
    {
        // If we're already preloading this same video, ignore duplicate call
        if (standbyPreparingIndex == index)
        {
            Debug.Log($"PreloadVideo: Already preloading index {index}, skipping duplicate.");
            return;
        }

        // Stop any previous preload routine (different index)
        if (preloadRoutine != null)
            StopCoroutine(preloadRoutine);

        standbyPreparingIndex = index;
        preloadRoutine = StartCoroutine(PreloadOnStandby(index));
    }


    private IEnumerator PreloadOnStandby(int index)
    {
        var standby = GetStandbyPlayer();
        var standbySphere = GetStandbySphere();

        standby.Stop();
        standby.url = videoFiles[index];
        standby.Prepare();

        float timeout = 20f;
        float t = 0f;
        while (!standby.isPrepared && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!standby.isPrepared)
        {
            Debug.LogWarning($"PreloadOnStandby: timed out preparing {videoFiles[index]}");
            yield break;
        }

        // Bind the texture so a swap is instant
        TryBindTextureTo(standby, standbySphere);
        // Don't play yet — standby remains hidden and paused (frame 0 by default after Prepare+Pause if you want)
        standby.Pause();
        standby.frame = 0;
        yield return null;

        standbyPreparingIndex = null;
    }

    /// <summary>
    /// Swap to the standby (already prepared), update indices, and fire "prepared & paused" — no playback yet.
    /// External systems should then schedule a DSP start for both audio and video at the same time.
    /// </summary>
    public void ActivatePreloadedAsActive(int indexJustLoaded)
    {
        // Visually swap spheres
        GetActiveSphere().enabled = false;
        GetStandbySphere().enabled = true;

        // Flip active flag
        usingA = !usingA;

        // Update current video info
        currentVideoIndex = indexJustLoaded;
        currentVideoName = Path.GetFileNameWithoutExtension(videoFiles[currentVideoIndex]);

        // Fire events: we've "changed" the active clip and it's paused on frame 0
        OnVideoChanged?.Invoke();
        OnVideoPreparedAndPaused?.Invoke();
    }

    /// <summary>
    /// Play the current active (already prepared & paused) video at an exact DSP time.
    /// </summary>
    public void BeginActiveAtDSP(double scheduledDspTime)
    {
        StartCoroutine(BeginActiveAtDSP_Co(scheduledDspTime));
    }

    private IEnumerator BeginActiveAtDSP_Co(double scheduledDspTime)
    {
        double wait = scheduledDspTime - AudioSettings.dspTime;
        if (wait > 0)
            yield return new WaitForSecondsRealtime((float)wait);

        var vp = GetActivePlayer();
        vp.Play();
        OnVideoStarted?.Invoke();
    }

    /// <summary>
    /// Helper to prepare and pause a specific index as active (no playback),
    /// and optionally preload the next one by index.
    /// </summary>
    public void PlayVideoByIndex(int index, int? nextIndexOverride = null)
    {
        StartCoroutine(PlayVideoByIndex_Co(index, nextIndexOverride));
    }

    private IEnumerator PlayVideoByIndex_Co(int index, int? nextIndexOverride)
    {
        yield return PrepareActivateAndPause(index, true);
        // After prepared & paused, external system (AudioManager) should schedule a joint start.
        // Preload next
        int next = nextIndexOverride ?? ((index + 1) % videoFiles.Count);
        if (videoFiles.Count > 1)
            StartCoroutine(PreloadOnStandby(next));
    }

    public void PlayNextVideo(int nextIndex)
    {
        //if (videoFiles.Count == 0) return;
        //int next = (currentVideoIndex + 1) % videoFiles.Count;
        //StartCoroutine(PrepareActivateAndPause(next, true));
        //int preloadNext = (next + 1) % videoFiles.Count;
        //if (videoFiles.Count > 1)
        //    StartCoroutine(PreloadOnStandby(preloadNext));

        // No need to prepare from scratch — standby already loaded
        ActivatePreloadedAsActive(nextIndex);

    }

    public void PauseVideo()
    {
        var vp = GetActivePlayer();
        if (vp.isPlaying) { vp.Pause(); OnVideoPaused?.Invoke(); }
    }

    public void ResumeVideo()
    {
        var vp = GetActivePlayer();
        if (!vp.isPlaying) { vp.Play(); OnVideoResumed?.Invoke(); }
    }

    public void StopVideo() => GetActivePlayer().Stop();

    public void RestartVideo()
    {
        var vp = GetActivePlayer();
        vp.time = 0;
        vp.Play();
        OnVideoRestarted?.Invoke();
    }

    public void SeekVideo(double newTime)
    {
        var vp = GetActivePlayer();
        vp.time = newTime;
        vp.Play();
        OnVideoSeeked?.Invoke();
    }

    public int GetCurrentVideoIndex() => currentVideoIndex;
    public string GetCurrentVideoName() => currentVideoName;

    private void TryBindTextureTo(VideoPlayer vp, Renderer target)
    {
        if (!vp || !target) return;
        var tex = vp.texture;
        if (!tex) return;
        var mat = target.material;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void EnsureMediaPermission()
    {
        try
        {
            int sdkInt = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
            if (sdkInt >= 33)
            {
                const string READ_MEDIA_VIDEO = "android.permission.READ_MEDIA_VIDEO";
                if (!Permission.HasUserAuthorizedPermission(READ_MEDIA_VIDEO))
                    Permission.RequestUserPermission(READ_MEDIA_VIDEO);
            }
            else
            {
                if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
                    Permission.RequestUserPermission(Permission.ExternalStorageRead);
            }
        }
        catch { }
    }
#endif
}
