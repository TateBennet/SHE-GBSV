using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class StreamVideos : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    // Folder with your videos
    public string videoFolder = @"C:\GBSV";

    public List<string> videoFiles = new List<string>
    {
        "VolleyballSceneV2B1.mp4",
        "VolleyballSceneV2B2.mp4",
        "VolleyballSceneV2BC.mp4"
    };

    [Header("Pause UI (placed in scene)")]
    [Tooltip("Root GameObject of your world-space UI (panel with the Next button). Keep it in the scene.")]
    public GameObject pauseUIRoot;            // ← assign your existing UI object in scene
    [Tooltip("Usually the Main Camera (player head). Used if autoPlaceUI is enabled.")]
    public Transform uiAnchor;                // ← assign Main Camera
    public bool autoPlaceUI = true;           // place UI in front of player on pause
    public float uiDistance = 1.2f;
    public Vector3 uiOffset = new Vector3(0, -0.05f, 0);
    public bool faceAnchor = true;            // rotate to face player (with 180° flip fix)

    private int currentIndex = 0;

    void Awake()
    {
        if (!videoPlayer) videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.prepareCompleted += OnPrepared;
    }

    void Start()
    {
        // Ensure UI starts hidden (so OnClick wiring on the scene object is preserved)
        if (pauseUIRoot) pauseUIRoot.SetActive(false);

        if (videoFiles.Count > 0) PlayVideo(currentIndex);
        else Debug.LogError("No video files listed in playlist!");
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
        HidePauseUI();

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
        vp.Pause();
        //put showui() here for automated on every clip
        Debug.Log("Video ended, paused. Showing pause UI...");
    }

    // Called by your in-scene UI Button (keeps OnClick wiring)
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
            HidePauseUI();
        }
    }

    public void ShowPauseUI()
    {
        if (!pauseUIRoot) return;

        if (autoPlaceUI && uiAnchor)
        {
            // place in front of anchor
            Vector3 fwd = uiAnchor.forward;
            Vector3 pos = uiAnchor.position + fwd * uiDistance + uiOffset;
            pauseUIRoot.transform.position = pos;

            if (faceAnchor)
            {
                // Face the player, keep upright, then flip 180° so UI front faces camera
                Vector3 lookDir = (uiAnchor.position - pauseUIRoot.transform.position);
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 1e-6f)
                {
                    pauseUIRoot.transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                    pauseUIRoot.transform.rotation *= Quaternion.Euler(0f, 180f, 0f);
                }
            }
        }

        pauseUIRoot.SetActive(true);
    }

    void HidePauseUI()
    {
        if (pauseUIRoot) pauseUIRoot.SetActive(false);
    }
}
