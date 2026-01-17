using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class TriStreamPro : MonoBehaviour
{
    [Header("Players")]
    public VideoPlayer playerA;
    public VideoPlayer playerB;
    public VideoPlayer playerC;

    [Header("Output Sphere")]
    public Renderer sphere;

    [Header("Video Files")]
    public List<string> videoFiles = new();

    [Serializable]
    public class VideoStartOverride
    {
        [Tooltip("Index into videoFiles (same index you pass to CommitBranch).")]
        public int videoIndex;

        [Tooltip("Start time (seconds) when this video becomes active.")]
        public double startTimeSeconds = 0;
    }

    [Header("Start Time Overrides (optional)")]
    [Tooltip("If a video index is listed here, it will start at startTimeSeconds instead of 0.")]
    public List<VideoStartOverride> startTimeOverrides = new();

    [Header("Timing")]
    public float prepareTimeout = 30f;
    public float primeDelay = 0.05f;

    private int activeSlot = 0;
    private int currentIndex = -1;
    private readonly int[] loadedIndex = { -1, -1, -1 };

    // NEW: expose the active video's start offset so audio can match it
    private double currentStartOffsetSeconds = 0;
    public double GetCurrentStartOffsetSeconds() => currentStartOffsetSeconds;

    public event Action OnVideoPreparedAndPaused;
    public event Action OnVideoStarted;
    public event Action OnVideoPaused;
    public event Action OnVideoResumed;
    public event Action OnVideoChanged;

    private VideoPlayer[] players;

    void Awake()
    {
        players = new[] { playerA, playerB, playerC };

        foreach (var p in players)
        {
            p.playOnAwake = false;
            p.skipOnDrop = true;
            p.waitForFirstFrame = true;
            p.renderMode = VideoRenderMode.APIOnly;

            // Optional safety: ensure we don't loop unless you explicitly want it.
            // (Won't affect most setups, but keeps behavior predictable.)
            // p.isLooping = false;
        }
    }

    void Start()
    {
        StartCoroutine(LoadIntoSlotAndActivate(0, 0));
    }

    // ---------------- PUBLIC API ----------------

    public void PreloadPrimary(int index)
    {
        PrepareIfNeeded(NextSlot(), index);
    }

    public void PreloadSecondary(int index)
    {
        PrepareIfNeeded(ThirdSlot(), index);
    }

    public void CommitBranch(int index)
    {
        int slot = FindSlot(index);
        if (slot < 0) slot = NextSlot();

        StartCoroutine(LoadIntoSlotAndActivate(slot, index));
    }

    public VideoPlayer GetActivePlayer() => players[activeSlot];
    public int GetCurrentVideoIndex() => currentIndex;

    public string GetCurrentVideoName()
    {
        if (currentIndex < 0 || currentIndex >= videoFiles.Count) return string.Empty;
        return Path.GetFileNameWithoutExtension(videoFiles[currentIndex]);
    }

    public void BeginActiveAtDSP(double dsp)
    {
        StartCoroutine(BeginAtDSP(dsp));
    }

    // ---------------- CORE LOGIC ----------------

    IEnumerator LoadIntoSlotAndActivate(int slot, int index)
    {
        yield return PrepareIntoSlot(slot, index);

        activeSlot = slot;
        currentIndex = index;

        var vp = players[slot];

        // NEW: start-time override (defaults to 0 if not configured)
        double startTime = GetStartTimeForIndex(index);
        currentStartOffsetSeconds = startTime;

        if (startTime > 0)
        {
            // Seek while paused (video is paused after PrepareIntoSlot priming)
            vp.time = startTime;

            // Prime texture at the seeked time so the first rendered frame matches.
            // IMPORTANT: Play() can advance time slightly, so we re-apply the exact seek after.
            vp.Play();
            yield return null;
            vp.Pause();

            // Re-apply exact seek after priming to avoid tiny drift
            vp.time = startTime;
        }
        else
        {
            // Preserve existing behavior
            vp.time = 0;
        }

        // Wait until the video has a valid texture after seek/prime.
        // This prevents a 1–2 frame “black flash” when the clip starts with black.
        yield return null; // at least one frame after prime
        while (vp.texture == null)
            yield return null;

        BindTexture(vp);

        OnVideoChanged?.Invoke();
        OnVideoPreparedAndPaused?.Invoke();
    }

    IEnumerator PrepareIntoSlot(int slot, int index)
    {
        if (loadedIndex[slot] == index) yield break;

        var vp = players[slot];
        vp.Stop();
        vp.url = videoFiles[index];
        vp.Prepare();

        float t = 0;
        while (!vp.isPrepared && t < prepareTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // If prepare times out, don't proceed with priming/marking as loaded.
        if (!vp.isPrepared) yield break;

        yield return new WaitForSecondsRealtime(primeDelay);

        // Prime first frame so texture is valid
        vp.Play();
        yield return null;
        vp.Pause();
        vp.frame = 0;

        loadedIndex[slot] = index;
    }

    IEnumerator BeginAtDSP(double dsp)
    {
        double wait = dsp - AudioSettings.dspTime;
        if (wait > 0) yield return new WaitForSecondsRealtime((float)wait);

        players[activeSlot].Play();
        OnVideoStarted?.Invoke();
    }

    // ---------------- HELPERS ----------------

    void PrepareIfNeeded(int slot, int index)
    {
        if (loadedIndex[slot] != index)
            StartCoroutine(PrepareIntoSlot(slot, index));
    }

    void BindTexture(VideoPlayer vp)
    {
        var mat = sphere.material;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", vp.texture);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", vp.texture);
    }

    int NextSlot() => (activeSlot + 1) % 3;
    int ThirdSlot() => (activeSlot + 2) % 3;

    int FindSlot(int index)
    {
        for (int i = 0; i < 3; i++)
            if (loadedIndex[i] == index) return i;
        return -1;
    }

    public void PauseVideo()
    {
        players[activeSlot].Pause();
        OnVideoPaused?.Invoke();
    }

    public void ResumeVideo()
    {
        players[activeSlot].Play();
        OnVideoResumed?.Invoke();
    }

    public void ReplayActiveResynced()
    {
        StartCoroutine(ReplayActiveResynced_Co());
    }

    private double GetStartTimeForIndex(int index)
    {
        if (startTimeOverrides == null) return 0;

        for (int i = 0; i < startTimeOverrides.Count; i++)
        {
            var o = startTimeOverrides[i];
            if (o != null && o.videoIndex == index)
                return Math.Max(0, o.startTimeSeconds);
        }

        return 0;
    }

    private IEnumerator ReplayActiveResynced_Co()
    {
        var vp = GetActivePlayer();
        if (!vp) yield break;

        // Replay should respect the current video's configured start offset too
        double startTime = GetStartTimeForIndex(currentIndex);
        currentStartOffsetSeconds = startTime;

        vp.Pause();
        vp.time = startTime;

        // Prime at the seeked time
        vp.Play();
        yield return null;
        vp.Pause();

        // Re-apply exact seek after priming
        vp.time = startTime;
        yield return null;

        OnVideoChanged?.Invoke();
        OnVideoPreparedAndPaused?.Invoke();
    }
}
