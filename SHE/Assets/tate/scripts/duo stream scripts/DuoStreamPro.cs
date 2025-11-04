using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR;

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

    [Header("Playback Info (read-only)")]
    [SerializeField] private int currentVideoIndex = -1;
    [SerializeField] private string currentVideoName = "";

    [Header("Targets (360 sphere renderers)")]
    public Renderer sphereA;
    public Renderer sphereB;

    // Convenience accessors
    public VideoPlayer GetActivePlayer() => usingA ? playerA : playerB;
    public VideoPlayer GetStandbyPlayer() => usingA ? playerB : playerA;
    public Renderer GetActiveSphere() => usingA ? sphereA : sphereB;
    public Renderer GetStandbySphere() => usingA ? sphereB : sphereA;


    public event Action OnVideoChanged;
    public event Action OnVideoPaused;
    public event Action OnVideoResumed;
    public event Action OnVideoRestarted;
    public event Action OnVideoSeeked;

    private bool usingA = true; // which player is currently active

    private VideoPlayer ActivePlayer => usingA ? playerA : playerB;
    private VideoPlayer StandbyPlayer => usingA ? playerB : playerA;


    private int? nextVideoOverride = null;

    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        EnsureMediaPermission();
#endif
    }

    //private void Start()
    //{
    //    if (videoFiles.Count > 0)
    //        PlayVideoByIndex(0, 1);
    //}

    private void Start()
    {
        if (videoFiles.Count > 0)
            StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        // Wait 1–2 seconds for XR to fully initialize
        yield return new WaitForSeconds(2f);
        PlayVideoByIndex(0,1);
    }


    public void PlayVideoByIndex(int index, int? nextIndexOverride = null)
    {
        if (index < 0 || index >= videoFiles.Count)
        {
            Debug.LogWarning("Invalid video index.");
            return;
        }

        StartCoroutine(PrepareAndPlay(index, nextIndexOverride));
    }


    private IEnumerator PrepareAndPlay(int index, int? nextIndexOverride)
    {
        currentVideoIndex = index;
        currentVideoName = Path.GetFileNameWithoutExtension(videoFiles[index]);

        var active = ActivePlayer;
        var activeSphere = GetActiveSphere();

        active.Stop();
        active.url = videoFiles[index];
        active.Prepare();

        float timeout = 12f;
        float timer = 0f;
        while (!active.isPrepared && timer < timeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!active.isPrepared)
        {
            Debug.LogWarning("Video prepare timed out: " + videoFiles[index]);
            yield break;
        }

        // Bind texture
        TryBindTextureTo(active, activeSphere);

        // Activate correct sphere
        activeSphere.enabled = true;
        GetStandbySphere().enabled = false;

        active.Play();
        OnVideoChanged?.Invoke();

        // Preload next
        PrepareNextVideo(nextIndexOverride);
    }


    private void PrepareNextVideo(int? nextIndexOverride = null)
    {
        if (videoFiles.Count <= 1) return;

        int nextIndex = nextIndexOverride.HasValue
            ? nextIndexOverride.Value
            : (currentVideoIndex + 1) % videoFiles.Count;

        StartCoroutine(PreloadOnStandby(nextIndex));
    }

    private IEnumerator PreloadOnStandby(int index)
    {
        var standby = StandbyPlayer;
        var standbySphere = GetStandbySphere();

        standby.Stop();
        standby.url = videoFiles[index];
        standby.Prepare();

        float timeout = 12f;
        float timer = 0f;
        while (!standby.isPrepared && timer < timeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!standby.isPrepared)
        {
            Debug.LogWarning("Next video failed to prepare: " + videoFiles[index]);
            yield break;
        }

        TryBindTextureTo(standby, standbySphere);
    }

    public void PlayNextVideo()
    {
        if (videoFiles.Count == 0) return;

        var standby = StandbyPlayer;
        var standbySphere = GetStandbySphere();

        if (!standby.isPrepared || standby.texture == null)
        {
            Debug.Log("Standby video not ready yet, falling back to direct load.");
            PlayVideoByIndex((currentVideoIndex + 1) % videoFiles.Count);
            return;
        }

        // swap visible spheres
        GetActiveSphere().enabled = false;
        standbySphere.enabled = true;

        // swap active flags
        usingA = !usingA;

        standby.Play();
        currentVideoIndex = (currentVideoIndex + 1) % videoFiles.Count;
        currentVideoName = Path.GetFileNameWithoutExtension(videoFiles[currentVideoIndex]);
        OnVideoChanged?.Invoke();

        PrepareNextVideo();
    }


    public void PlayPreviousVideo()
    {
        if (videoFiles.Count == 0) return;

        int prev = (currentVideoIndex - 1 + videoFiles.Count) % videoFiles.Count;
        PlayVideoByIndex(prev);
    }

    public void PauseVideo()
    {
        if (ActivePlayer.isPlaying)
        {
            ActivePlayer.Pause();
            OnVideoPaused?.Invoke();
        }
    }

    public void ResumeVideo()
    {
        if (!ActivePlayer.isPlaying)
        {
            ActivePlayer.Play();
            OnVideoResumed?.Invoke();
        }
    }

    public void StopVideo() => ActivePlayer.Stop();

    public void RestartVideo()
    {
        ActivePlayer.time = 0;
        ActivePlayer.Play();
        OnVideoRestarted?.Invoke();
    }

    public void SeekVideo(double newTime)
    {
        ActivePlayer.time = newTime;
        ActivePlayer.Play();
        OnVideoSeeked?.Invoke();
    }

    public int GetCurrentVideoIndex() => currentVideoIndex;
    public string GetCurrentVideoName() => currentVideoName;
    public bool IsPlaying() => ActivePlayer.isPlaying;

    private void TryBindTextureTo(VideoPlayer vp, Renderer target)
    {
        if (vp.texture == null || target == null) return;

        var mat = target.material;
        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", vp.texture);
        if (mat.HasProperty("_MainTex"))
            mat.SetTexture("_MainTex", vp.texture);
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
