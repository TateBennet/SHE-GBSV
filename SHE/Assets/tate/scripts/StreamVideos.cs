using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class StreamVideos : MonoBehaviour
{
    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Video Folder + Files")]
    public string videoFolder = @"C:\GBSV";
    public List<string> videoFiles = new List<string>
    {
        "VolleyballSceneV2B1.mp4",
        "VolleyballSceneV2B2.mp4",
        "VolleyballSceneV2BC.mp4"
    };

    private int currentIndex = 0;

    void Awake()
    {
        if (!videoPlayer) videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.prepareCompleted += OnPrepared;
    }

    void Start()
    {
        if (videoFiles.Count > 0)
            PlayVideo(currentIndex);
        else
            Debug.LogError("No video files listed in playlist!");
    }

    void OnDestroy()
    {
        if (videoPlayer)
        {
            videoPlayer.loopPointReached -= OnVideoEnd;
            videoPlayer.prepareCompleted -= OnPrepared;
        }
    }

    void PlayVideo(int index)
    {
        if (index < 0 || index >= videoFiles.Count)
        {
            Debug.LogError("Index out of range.");
            return;
        }

        string path = Path.Combine(videoFolder, videoFiles[index]);
        if (!File.Exists(path))
        {
            Debug.LogError("Video not found: " + path);
            return;
        }

        Debug.Log("Preparing video: " + path);
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = path;
        videoPlayer.Pause();
        videoPlayer.Prepare(); // OnPrepared -> Play
    }

    void OnPrepared(VideoPlayer vp)
    {
        vp.Play();
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        Debug.Log("Video ended.");
        PlayNext();
    }

    public void PlayNext()
    {
        currentIndex++;
        if (currentIndex < videoFiles.Count)
        {
            PlayVideo(currentIndex);
        }
        else
        {
            Debug.Log("Playlist finished!");
        }
    }

    public void PlayPrevious()
    {
        currentIndex--;
        if (currentIndex >= 0)
        {
            PlayVideo(currentIndex);
        }
        else
        {
            Debug.Log("Already at the first video!");
            currentIndex = 0;
        }
    }

    public void PauseVideo()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            Debug.Log("Video paused.");
        }
    }

    public void ResumeVideo()
    {
        if (!videoPlayer.isPlaying)
        {
            videoPlayer.Play();
            Debug.Log("Video resumed.");
        }
    }

    public void TogglePlayPause()
    {
        if (videoPlayer.isPlaying) PauseVideo();
        else ResumeVideo();
    }
}