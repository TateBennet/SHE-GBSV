using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
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

    [Header("Timing")]
    public float prepareTimeout = 30f;
    public float primeDelay = 0.05f;

    private int activeSlot = 0;
    private int currentIndex = -1;
    private readonly int[] loadedIndex = { -1, -1, -1 };

    public event Action OnVideoPreparedAndPaused;
    public event Action OnVideoStarted;
    public event Action OnVideoPaused;
    public event Action OnVideoResumed;
    public event Action OnVideoChanged;
    VideoPlayer[] players;

    void Awake()
    {
        players = new[] { playerA, playerB, playerC };

        foreach (var p in players)
        {
            p.playOnAwake = false;
            p.skipOnDrop = true;
            p.waitForFirstFrame = true;
            p.renderMode = VideoRenderMode.APIOnly;
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
    public string GetCurrentVideoName() => Path.GetFileNameWithoutExtension(videoFiles[currentIndex]);

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

        BindTexture(players[slot]);
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

        yield return new WaitForSecondsRealtime(primeDelay);

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

    public void ReplayActive()
    {
        var vp = GetActivePlayer();
        if (!vp) return;

        vp.Pause();
        vp.time = 0;
        vp.Play();
    }
}
