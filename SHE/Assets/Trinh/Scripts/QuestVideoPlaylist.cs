using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class QuestVideoPlaylist : MonoBehaviour
{
    [Header("Components")]
    public VideoPlayer videoPlayer;
    public Renderer targetSphere;                 // optional: inside-out 360 sphere (Unlit)
    public Renderer blackOverlay;                 // optional: Transparent/Unlit black for fades
    public TextMeshProUGUI statusText;            // optional: on-screen debug

    [Header("Playlist (filenames only, from /sdcard/Download)")]
    public List<string> files = new List<string>();  // e.g., {"Intro.mp4","Clip01.mp4",...}

    [Header("Fade")]
    public float fadeDuration = 0.5f;

    [Header("Scene Flow")]
    public string introSceneName = "Intro_Scene";   // 👈 Name of your intro scene in Build Settings
    public bool returnToIntroAtEnd = true;         // 👈 Toggle to return vs. loop

    private const string ROOT = "/sdcard/Download";
    private int _index = 0;
    private bool _textureBound = false;
    private Material _overlayMat;

    private void Awake()
    {
        if (!videoPlayer) videoPlayer = GetComponent<VideoPlayer>();
        if (!videoPlayer) { Log("❌ No VideoPlayer on this object."); enabled = false; return; }

        if (blackOverlay) _overlayMat = blackOverlay.material;
        SetOverlayAlpha(0f);

        videoPlayer.source = VideoSource.Url;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.errorReceived += (vp, msg) => Log($"❌ Video ERROR: {msg}\nURL:\n{vp.url}");
        videoPlayer.prepareCompleted += vp => { Log("👍 Prepared:\n" + vp.url); BindTextureIfReady(); };
        videoPlayer.loopPointReached += vp => StartCoroutine(PlayNext());

#if UNITY_ANDROID && !UNITY_EDITOR
        EnsureMediaPermission();  // Android 13+ READ_MEDIA_VIDEO
#endif
    }

    private IEnumerator Start()
    {
        if (files == null || files.Count == 0)
        {
            Log("❌ No filenames in playlist. Add them in the Inspector.");
            yield break;
        }

        _index = 0;
        yield return PlayIndex(_index, fadeIn: true);
    }

    public IEnumerator PlayNext()
    {
        yield return FadeOverlay(0f, 1f, fadeDuration);
        yield return FadeVolume(1f, 0f, fadeDuration * 0.8f);

        _index++;

        if (_index >= files.Count)
        {
            if (returnToIntroAtEnd && !string.IsNullOrEmpty(introSceneName))
            {
                Log($"🎬 Playlist finished → loading scene '{introSceneName}'");
                SceneManager.LoadScene(introSceneName);
                yield break;
            }
            else
            {
                _index = 0; // fallback: loop
            }
        }

        yield return PlayIndex(_index, fadeIn: true);
    }

    public void PauseVideo()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            Debug.Log("Video paused.");
        }
    }

    public void Next() => StartCoroutine(PlayNext());

    public void Prev()
    {
        StartCoroutine(CoPrev());
    }

    private IEnumerator CoPrev()
    {
        yield return FadeOverlay(0f, 1f, fadeDuration);
        yield return FadeVolume(1f, 0f, fadeDuration * 0.8f);
        _index = (_index - 1 + files.Count) % files.Count;
        yield return PlayIndex(_index, fadeIn: true);
    }

    private IEnumerator PlayIndex(int i, bool fadeIn)
    {
        string fileName = Path.GetFileName(files[i]);
        string abs = PathCombine(ROOT, fileName);
        string url = "file://" + abs.Replace(" ", "%20");

        _textureBound = false;
        videoPlayer.Stop();
        videoPlayer.url = url;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetDirectAudioVolume(0, 1f);

        Log("▶️ Loading:\n" + url);

        videoPlayer.Prepare();
        float t = 0f, timeout = 12f;
        while (!videoPlayer.isPrepared && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            Log("⏳ Prepare TIMED OUT. Check filename, case, codec.");
            yield break;
        }

        BindTextureIfReady();
        videoPlayer.Play();

        if (fadeIn)
        {
            videoPlayer.SetDirectAudioVolume(0, 0f);
            yield return FadeOverlay(1f, 0f, fadeDuration);
            yield return FadeVolume(0f, 1f, fadeDuration * 0.8f);
        }

        Log($"✅ Playing:\n{videoPlayer.url}");
    }

    private void BindTextureIfReady()
    {
        if (_textureBound || targetSphere == null) return;
        var tex = videoPlayer.texture;
        if (tex == null) return;

        var mat = targetSphere.material;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        _textureBound = true;
    }

    // ---------- Fades ----------
    private IEnumerator FadeOverlay(float from, float to, float dur)
    {
        if (_overlayMat == null) yield break;
        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            SetOverlayAlpha(Mathf.Lerp(from, to, e / dur));
            yield return null;
        }
        SetOverlayAlpha(to);
    }

    private IEnumerator FadeVolume(float from, float to, float dur)
    {
        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float v = Mathf.Lerp(from, to, e / dur);
            videoPlayer.SetDirectAudioVolume(0, v);
            yield return null;
        }
        videoPlayer.SetDirectAudioVolume(0, to);
    }

    private void SetOverlayAlpha(float a)
    {
        if (_overlayMat != null && _overlayMat.HasProperty("_Color"))
        {
            var c = _overlayMat.color; c.a = a; _overlayMat.color = c;
        }
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
                {
                    Log("Requesting READ_MEDIA_VIDEO permission…");
                    Permission.RequestUserPermission(READ_MEDIA_VIDEO);
                }
            }
            else
            {
                if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
                {
                    Log("Requesting READ_EXTERNAL_STORAGE permission…");
                    Permission.RequestUserPermission(Permission.ExternalStorageRead);
                }
            }
        }
        catch { }
    }
#endif

    // ---------- Helpers ----------
    private static string PathCombine(string a, string b)
    {
        string p = (a.TrimEnd('/') + "/" + b.TrimStart('/')).Replace('\\', '/');
        if (!p.StartsWith("/")) p = "/" + p;
        return p;
    }
    private static bool FileExistsSafe(string p)
    {
        try { return File.Exists(p); } catch { return false; }
    }

    private void Log(string msg)
    {
        Debug.Log("[PlaylistFromQuestDownload] " + msg);
        if (statusText) statusText.text = msg;
    }

    public int GetCurrentIndex() => _index;
}