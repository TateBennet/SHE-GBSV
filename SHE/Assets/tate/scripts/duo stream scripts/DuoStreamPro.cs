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

    // which player is currently active
    private bool usingA = true;

    // track which index is actually prepared on each player
    private int preparedIndexA = -1;
    private int preparedIndexB = -1;

    public event Action OnVideoChanged;
    public event Action OnVideoPaused;



    // convenience
    private VideoPlayer ActivePlayer => usingA ? playerA : playerB;
    private VideoPlayer StandbyPlayer => usingA ? playerB : playerA;
    private Renderer ActiveSphere => usingA ? sphereA : sphereB;
    private Renderer StandbySphere => usingA ? sphereB : sphereA;

    public VideoPlayer GetActivePlayer() => ActivePlayer;
    public VideoPlayer GetStandbyPlayer() => StandbyPlayer;
    public Renderer GetActiveSphere() => ActiveSphere;
    public Renderer GetStandbySphere() => StandbySphere;

    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        EnsureMediaPermission();
#endif
    }

    private void Start()
    {
        playerA.playbackSpeed = 1.0f;
        playerB.playbackSpeed = 1.0f;
        playerA.skipOnDrop = true;
        playerB.skipOnDrop = true;

        if (videoFiles.Count > 0)
            StartCoroutine(PrepareAndStartFirstVideo());
    }

    /// <summary>
    /// Prepares the first video fully before starting playback.
    /// </summary>
    private IEnumerator PrepareAndStartFirstVideo()
    {
        int firstIndex = 0;

        Debug.Log("🎥 Preparing first video: " + videoFiles[firstIndex]);

        var firstPlayer = playerA;
        var firstSphere = sphereA;

        firstPlayer.url = videoFiles[firstIndex];
        firstPlayer.Prepare();

        float timeout = 30f;
        float timer = 0f;
        while (!firstPlayer.isPrepared && timer < timeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!firstPlayer.isPrepared)
        {
            Debug.LogWarning("❌ First video failed to prepare: " + videoFiles[firstIndex]);
            yield break;
        }

        Debug.Log("✅ First video prepared. Starting playback...");

        // Bind texture to sphere
        TryBindTextureTo(firstPlayer, firstSphere);
        firstSphere.enabled = true;
        sphereB.enabled = false;

        usingA = true;
        currentVideoIndex = firstIndex;
        currentVideoName = Path.GetFileNameWithoutExtension(videoFiles[firstIndex]);

        firstPlayer.Play();
        OnVideoChanged?.Invoke();

        // Preload the next one
        PrepareNextVideo(1);
    }

    /// <summary>
    /// Public way to ask for a clip by index. This now attempts an instant swap
    /// if the standby player already has that clip prepared.
    /// </summary>
    public void PlayVideoByIndex(int index, int? nextIndexOverride = null)
    {
        if (index < 0 || index >= videoFiles.Count)
        {
            Debug.LogWarning("Invalid video index.");
            return;
        }

        // FAST PATH: is the standby already prepared with this clip?
        if (IsStandbyPreparedFor(index))
        {
            InstantSwapToStandby(index, nextIndexOverride);
            return;
        }

        // otherwise do the normal prepare+play path
        StartCoroutine(PrepareAndPlay(index, nextIndexOverride));
    }

    /// <summary>
    /// Explicit public preload – useful for interaction scripts
    /// that know they'll want a specific clip in a couple seconds.
    /// </summary>
    public void PreloadVideo(int index)
    {
        if (index < 0 || index >= videoFiles.Count) return;

        // already current
        if (index == currentVideoIndex) return;

        // already on standby?
        if (IsStandbyPreparedFor(index)) return;

        // preload it on the standby player
        StartCoroutine(PreloadOnStandby(index));
    }

    private IEnumerator PrepareAndPlay(int index, int? nextIndexOverride)
    {
        currentVideoIndex = index;
        currentVideoName = Path.GetFileNameWithoutExtension(videoFiles[index]);

        var active = ActivePlayer;
        var activeSphere = ActiveSphere;

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

        // mark prepared index for the active player
        SetPreparedIndexFor(active, index);

        // Bind texture
        TryBindTextureTo(active, activeSphere);

        // show active sphere, hide the other
        activeSphere.enabled = true;
        StandbySphere.enabled = false;

        active.Play();
        OnVideoChanged?.Invoke();

        // Preload whatever should be next
        PrepareNextVideo(nextIndexOverride);
    }

    /// <summary>
    /// Called whenever we successfully swap to the standby that already had the clip.
    /// </summary>
    private void InstantSwapToStandby(int index, int? nextIndexOverride)
    {
        // hide current sphere
        ActiveSphere.enabled = false;
        // show standby sphere
        StandbySphere.enabled = true;

        // flip active flag
        usingA = !usingA;

        // actually play
        ActivePlayer.Play();

        currentVideoIndex = index;
        currentVideoName = Path.GetFileNameWithoutExtension(videoFiles[index]);
        OnVideoChanged?.Invoke();

        // active player is definitely prepared for this index
        SetPreparedIndexFor(ActivePlayer, index);

        // load next
        PrepareNextVideo(nextIndexOverride);
    }

    private void PrepareNextVideo(int? nextIndexOverride = null)
    {
        if (videoFiles.Count <= 1) return;

        int nextIndex = nextIndexOverride.HasValue
            ? nextIndexOverride.Value
            : (currentVideoIndex + 1) % videoFiles.Count;

        // don't double-preload if it's already on standby
        if (IsStandbyPreparedFor(nextIndex)) return;

        StartCoroutine(PreloadOnStandby(nextIndex));
    }

    private IEnumerator PreloadOnStandby(int index)
    {
        var standby = StandbyPlayer;
        var standbySphere = StandbySphere;

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

        // mark which index is on this standby
        SetPreparedIndexFor(standby, index);

        // we don't show this sphere yet, but we *do* bind so texture is ready
        TryBindTextureTo(standby, standbySphere);
    }

    /// <summary>
    /// Original "next" behaviour – still works and benefits from the tracking.
    /// </summary>
    public void PlayNextVideo()
    {
        if (videoFiles.Count == 0) return;

        var standby = StandbyPlayer;
        var standbySphere = StandbySphere;

        if (!standby.isPrepared || standby.texture == null)
        {
            Debug.Log("Standby video not ready yet, falling back to direct load.");
            PlayVideoByIndex((currentVideoIndex + 1) % videoFiles.Count);
            return;
        }

        // swap visible spheres
        ActiveSphere.enabled = false;
        standbySphere.enabled = true;

        // swap active
        usingA = !usingA;

        standby.Play();
        currentVideoIndex = (currentVideoIndex + 1) % videoFiles.Count;
        currentVideoName = Path.GetFileNameWithoutExtension(videoFiles[currentVideoIndex]);
        OnVideoChanged?.Invoke();

        // mark active as prepared for current
        SetPreparedIndexFor(ActivePlayer, currentVideoIndex);

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

    public void StopVideo() => ActivePlayer.Stop();

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

    private bool IsStandbyPreparedFor(int index)
    {
        // if active is A, standby is B
        if (usingA)
        {
            return preparedIndexB == index && playerB.isPrepared;
        }
        else
        {
            return preparedIndexA == index && playerA.isPrepared;
        }
    }

    private void SetPreparedIndexFor(VideoPlayer vp, int index)
    {
        if (vp == playerA)
            preparedIndexA = index;
        else if (vp == playerB)
            preparedIndexB = index;
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
