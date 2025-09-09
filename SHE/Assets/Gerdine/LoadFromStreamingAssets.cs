using UnityEngine;
using UnityEngine.Video;
using System.IO;

public class LoadFromStreamingAssets : MonoBehaviour
{
    public VideoPlayer vp;
    public string fileName = "my360.mp4";
    void Start()
    {
        if (!vp) vp = GetComponent<VideoPlayer>();
        vp.source = VideoSource.Url;
        vp.url = Path.Combine(Application.streamingAssetsPath, fileName);
        vp.Play();
    }
}