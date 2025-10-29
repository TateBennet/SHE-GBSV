using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class DualSpherePlaylist : MonoBehaviour
{
    [Header("Players (both on this GO is fine)")]
    public VideoPlayer playerA;
    public VideoPlayer playerB;

    [Header("Targets (360 sphere renderers)")]
    public Renderer sphereA;
    public Renderer sphereB;

    [Header("Playlist (filenames only, in /sdcard/Download)")]
    public List<string> files = new List<string>();

    [Header("UI (optional)")]
    public TextMeshProUGUI statusText;

    [Header("Options")]
    public float prepareTimeout = 12f;
    public bool startAtIndexZero = true;

    const string ROOT = "/sdcard/Download";
    int index = 0;
    VideoPlayer _active;
    VideoPlayer _idle;
    Renderer _activeSphere;
    Renderer _idleSphere;
    bool _bindingDoneThisClip = false;

    void Awake()
    {
        if (!playerA || !playerB || !sphereA || !sphereB)
        {
            Log("❌ Assign PlayerA/PlayerB and SphereA/SphereB.");
            enabled = false;
            return;
        }

        // Make A the active pair to start
        _active = playerA;
        _idle = playerB;
        _activeSphere = sphereA;
        _idleSphere = sphereB;

        // Ensure spheres start in known visibility
        SetSphereVisible(_activeSphere, true);
        SetSphereVisible(_idleSphere, false);

        // Player common config
        SetupPlayer(playerA);
        SetupPlayer(playerB);

#if UNITY_ANDROID && !UNITY_EDITOR
        EnsureVideoPermission();
#endif
    }

    void SetupPlayer(VideoPlayer vp)
    {
        vp.source = VideoSource.Url;
        vp.renderMode = VideoRenderMode.APIOnly;
        vp.playOnAwake = false;
        vp.waitForFirstFrame = true;
        vp.isLooping = false;

        vp.prepareCompleted += OnPrepared;
        vp.errorReceived += OnError;
        vp.loopPointReached += OnLoopPoint;

        // we bind texture after prepare; do not set targetTexture here
    }

    void Start()
    {
        if (files == null || files.Count == 0)
        {
            Log("❌ No files in playlist.");
            return;
        }
        if (!startAtIndexZero)
            index = Mathf.Clamp(index, 0, files.Count - 1);
        else
            index = 0;

        StartCoroutine(PlayIndex(index));
    }

    IEnumerator PlayIndex(int i)
    {
        string fileName = Path.GetFileName(files[i].Trim());
        string abs = CombinePosix(ROOT, fileName);
        string url = "file://" + EscapeForUrl(abs);

        Log($"▶️ Preparing [{i + 1}/{files.Count}] {fileName}\n{abs}");

        _bindingDoneThisClip = false;
        _active.Stop();
        _active.url = url;
        _active.EnableAudioTrack(0, true);
        _active.SetDirectAudioVolume(0, 1f);
        _active.Prepare();

        // wait for prepare with timeout
        float t = 0f;
        while (!_active.isPrepared && t < prepareTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_active.isPrepared)
        {
            Log("⏳ Prepare timed out. Check name/codec/permission.");
            yield break;
        }

        // bind texture (in case prepare callback raced)
        TryBindTextureTo(_active, _activeSphere);

        _active.Play();
        Log($"✅ Playing: {fileName}");

        // Preload NEXT on the idle player, if any
        int next = (i + 1 < files.Count) ? i + 1 : -1;
        if (next >= 0)
        {
            StartCoroutine(PreloadOnIdle(next));
        }
    }

    IEnumerator PreloadOnIdle(int i)
    {
        string fileName = Path.GetFileName(files[i].Trim());
        string abs = CombinePosix(ROOT, fileName);
        string url = "file://" + EscapeForUrl(abs);

        _idle.Stop();
        _idle.url = url;
        _idle.EnableAudioTrack(0, true);
        _idle.SetDirectAudioVolume(0, 1f);
        _idle.Prepare();

        float t = 0f;
        while (!_idle.isPrepared && t < prepareTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_idle.isPrepared)
        {
            Log($"⚠️ Next clip failed to prepare: {fileName}");
        }
        else
        {
            // bind now so we can swap instantly
            TryBindTextureTo(_idle, _idleSphere);
            Log($"🟡 Next ready: {fileName}");
        }
    }

    void OnLoopPoint(VideoPlayer vp)
    {
        // When current finishes, if idle is prepared, swap immediately (no fade)
        int next = index + 1;
        if (next >= files.Count)
        {
            Log("🏁 Playlist complete.");
            return;
        }

        bool idleReady = _idle.isPrepared && _idle.texture != null;
        if (!idleReady)
        {
            Log("⏳ Idle not ready at loop point; attempting direct play of next.");
            index = next;
            StartCoroutine(PlayIndex(index));
            return;
        }

        // swap spheres: show idle, hide active
        SetSphereVisible(_activeSphere, false);
        SetSphereVisible(_idleSphere, true);

        // stop active to free decoder
        _active.Stop();

        // swap roles
        var tmpP = _active; _active = _idle; _idle = tmpP;
        var tmpR = _activeSphere; _activeSphere = _idleSphere; _idleSphere = tmpR;

        index = next;
        _bindingDoneThisClip = true; // was already bound during preload

        // ensure audio up, then play
        _active.SetDirectAudioVolume(0, 1f);
        _active.Play();

        // kick off preload of the next-after-next
        int further = index + 1;
        if (further < files.Count) StartCoroutine(PreloadOnIdle(further));

        Log($"➡️ Swapped to next: {Path.GetFileName(files[index])}");
    }

    void OnPrepared(VideoPlayer vp)
    {
        // Bind whichever player just prepared to its corresponding sphere
        if (vp == _active)
            TryBindTextureTo(_active, _activeSphere);
        else if (vp == _idle)
            TryBindTextureTo(_idle, _idleSphere);
    }

    void TryBindTextureTo(VideoPlayer vp, Renderer target)
    {
        if (vp.texture == null || target == null) return;

        var mat = target.material;
        // URP Unlit uses _BaseMap, Legacy Unlit uses _MainTex
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", vp.texture);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", vp.texture);
    }

    void SetSphereVisible(Renderer r, bool on)
    {
        if (!r) return;
        r.enabled = on;
        if (r.gameObject.activeSelf != on) r.gameObject.SetActive(on);
    }

    void OnError(VideoPlayer vp, string msg)
    {
        Log("❌ Video ERROR: " + msg + "\nURL:\n" + (vp ? vp.url : "(null)"));
    }

    string CombinePosix(string a, string b)
    {
        // no backslashes; safe for Android
        return (a.TrimEnd('/') + "/" + b.TrimStart('/')).Replace('\\', '/');
    }

    string EscapeForUrl(string absPath)
    {
        // only escape the filename segment; keep slashes intact
        // For simplicity here: escape spaces only
        return absPath.Replace(" ", "%20");
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void EnsureVideoPermission()
    {
        try
        {
            int sdkInt = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
            if (sdkInt >= 33)
            {
                const string READ_MEDIA_VIDEO = "android.permission.READ_MEDIA_VIDEO";
                if (!Permission.HasUserAuthorizedPermission(READ_MEDIA_VIDEO))
                {
                    Log("Requesting READ_MEDIA_VIDEO…");
                    Permission.RequestUserPermission(READ_MEDIA_VIDEO);
                }
            }
            else
            {
                if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
                {
                    Log("Requesting READ_EXTERNAL_STORAGE…");
                    Permission.RequestUserPermission(Permission.ExternalStorageRead);
                }
            }
        }
        catch (Exception) { }
    }
#endif

    void Log(string m)
    {
        Debug.Log("[DualSphere] " + m);
        if (statusText) statusText.text = m;
    }
}
