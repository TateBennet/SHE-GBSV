using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class StreamVideos : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    // Folder on your desktop with all your test videos
    public string videoFolder = @"C:\GBSV";

    // Just list the file names in order
    public List<string> videoFiles = new List<string>
    {
        "VolleyballSceneV2B1.mp4",
        "VolleyballSceneV2B2.mp4",
        "VolleyballSceneV2BC.mp4"   
    };

    private int currentIndex = 0;

    void Start()
    {
        if (videoFiles.Count > 0)
        {
            PlayVideo(currentIndex);
            videoPlayer.loopPointReached += OnVideoEnd;
        }
        else
        {
            Debug.LogError("No video files listed in playlist!");
        }
    }

    void PlayVideo(int index)
    {
        string path = Path.Combine(videoFolder, videoFiles[index]);

        if (File.Exists(path))
        {
            Debug.Log("Playing video: " + path);
            videoPlayer.url = path;
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += (vp) => vp.Play();
        }
        else
        {
            Debug.LogError("Video not found: " + path);
        }
    }

    void OnVideoEnd(VideoPlayer vp)
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
}
