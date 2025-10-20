using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class ProVidMngr : MonoBehaviour
{
    [Header("References")]
    public VideoPlayer videoPlayer;

    [Header("Video Files (assign manually in Inspector)")]
    public List<string> videoFiles = new List<string>();

    [Header("Playback Info (read-only)")]
    [SerializeField] private int currentVideoIndex = -1;
    [SerializeField] private string currentVideoName = "";

    public event Action OnVideoChanged;
    public event Action OnVideoPaused;
    public event Action OnVideoResumed;
    public event Action OnVideoRestarted;
    public event Action OnVideoSeeked;

    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    EnsureMediaPermission();
#endif
    }


    private void Start()
    {
        // ✅ Auto-play the first video if you want something right away
        if (videoFiles.Count > 0)
            PlayVideoByIndex(0);

    }

    public void PlayVideoByIndex(int index)
    {
        if (index < 0 || index >= videoFiles.Count)
        {
            Debug.LogWarning("Invalid video index.");
            return;
        }

        currentVideoIndex = index;
        currentVideoName = Path.GetFileNameWithoutExtension(videoFiles[index]);
        videoPlayer.url = videoFiles[index];

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare();
    }


    private void OnVideoPrepared(VideoPlayer vp)
    {

        // Get the Renderer on the same object as the VideoPlayer
        Renderer sphereRenderer = vp.GetComponent<Renderer>();

        if (sphereRenderer != null && vp.texture != null)
        {
            // Assign the decoded texture to whatever property the shader uses
            if (sphereRenderer.material.HasProperty("_BaseMap"))
                sphereRenderer.material.SetTexture("_BaseMap", vp.texture);
            else
                sphereRenderer.material.mainTexture = vp.texture;
        }
        else
        {
            Debug.LogWarning("No renderer or texture found for video output!");
        }

        vp.Play();
        OnVideoChanged?.Invoke(); // audio starts right away
    }

    public void PauseVideo()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            OnVideoPaused?.Invoke();
        }
    }

    public void ResumeVideo()
    {
        if (!videoPlayer.isPlaying)
        {
            videoPlayer.Play();
            OnVideoResumed?.Invoke();
        }
    }

    public void StopVideo() => videoPlayer.Stop();

    public void PlayNextVideo()
    {
        if (videoFiles.Count == 0) return;
        int next = (currentVideoIndex + 1) % videoFiles.Count;
        PlayVideoByIndex(next);
        

    }

    public void PlayPreviousVideo()
    {
        if (videoFiles.Count == 0) return;
        int prev = (currentVideoIndex - 1 + videoFiles.Count) % videoFiles.Count;
        PlayVideoByIndex(prev);

    }

    public void RestartVideo()
    {
        videoPlayer.time = 0;
        videoPlayer.Play();
        OnVideoRestarted?.Invoke();
    }

    public void SeekVideo(double newTime)
    {
        videoPlayer.time = newTime;
        videoPlayer.Play();
        OnVideoSeeked?.Invoke();
    }

    public int GetCurrentVideoIndex() => currentVideoIndex;
    public string GetCurrentVideoName() => currentVideoName;
    public bool IsPlaying() => videoPlayer.isPlaying;

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
                {
                    Permission.RequestUserPermission(READ_MEDIA_VIDEO);
                }
            }
            else
            {
                if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
                {
                    Permission.RequestUserPermission(Permission.ExternalStorageRead);
                }
            }
        }
        catch { }
    }
#endif

}

