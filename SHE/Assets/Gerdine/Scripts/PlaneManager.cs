using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class TwoVideoPlaneSequenceWithFade : MonoBehaviour
{
    [Header("Plane Objects (these should contain a Renderer)")]
    public Renderer plane1Renderer;
    public Renderer plane2Renderer;

    [Header("Video Players")]
    public VideoPlayer video1;
    public VideoPlayer video2;

    [Header("Fade Settings")]
    public float fadeDuration = 1.5f;   // how long each fade should take

    private Material mat1;
    private Material mat2;

    private void Start()
    {
        if (!plane1Renderer || !plane2Renderer || !video1 || !video2)
        {
            Debug.LogWarning("TwoVideoPlaneSequenceWithFade: Please assign both plane renderers and both VideoPlayers.");
            return;
        }

        // Duplicate materials so alpha changes don't affect shared materials
        plane1Renderer.material = new Material(plane1Renderer.material);
        plane2Renderer.material = new Material(plane2Renderer.material);

        mat1 = plane1Renderer.material;
        mat2 = plane2Renderer.material;

        SetAlpha(mat1, 0f);
        SetAlpha(mat2, 0f);

        // We control the flow, so no auto-looping
        video1.isLooping = false;
        video2.isLooping = false;

        // Subscribe to "finished" events
        video1.loopPointReached += OnVideo1Finished;
        video2.loopPointReached += OnVideo2Finished;

        // Start sequence: fade in plane 1 + play video 1
        StartCoroutine(StartVideo1Routine());
    }

    private void OnDestroy()
    {
        if (video1 != null) video1.loopPointReached -= OnVideo1Finished;
        if (video2 != null) video2.loopPointReached -= OnVideo2Finished;
    }

    // --- START VIDEOS ---

    IEnumerator StartVideo1Routine()
    {
        plane1Renderer.gameObject.SetActive(true);
        plane2Renderer.gameObject.SetActive(true); // keep active so crossfades work

        SetAlpha(mat1, 0f);
        SetAlpha(mat2, 0f);

        video1.time = 0;
        video1.Play();

        // Fade in plane 1
        yield return Fade(mat1, 0f, 1f);
    }

    IEnumerator StartVideo2Routine()
    {
        // Video 1 has finished, now fade from plane 1 to plane 2 and start video 2
        plane1Renderer.gameObject.SetActive(true);
        plane2Renderer.gameObject.SetActive(true);

        // Make sure plane2 starts invisible
        SetAlpha(mat2, 0f);

        // Start video 2 from beginning
        video2.time = 0;
        video2.Play();

        // Crossfade visuals from plane 1 -> plane 2
        yield return Crossfade(mat1, mat2);
    }

    // --- EVENTS ---

    private void OnVideo1Finished(VideoPlayer vp)
    {
        // When video 1 ends, go to video 2 (with fade)
        StartCoroutine(StartVideo2Routine());
    }

    private void OnVideo2Finished(VideoPlayer vp)
    {
        // When video 2 ends, fade back to plane 1 and STOP (no loop)
        StartCoroutine(FadeBackToPlane1AndStop());
    }

    // --- FINAL FADE BACK & STOP ---

    IEnumerator FadeBackToPlane1AndStop()
    {
        // We assume video1 has already finished and its last frame is on mat1
        // If you prefer the first frame, you can set video1.time = 0; video1.Play(); video1.Pause();

        // Make sure both planes are active for the fade
        plane1Renderer.gameObject.SetActive(true);
        plane2Renderer.gameObject.SetActive(true);

        // Crossfade from plane 2 (currently visible) back to plane 1
        yield return Crossfade(mat2, mat1);

        // Stop video2 so nothing else plays
        video2.Stop();

        // Optional: you can also stop video1 here if you like
        // video1.Stop();

        // Final state:
        // - Plane 1 fully visible
        // - Plane 2 invisible (you can also disable its GameObject)
        SetAlpha(mat1, 1f);
        SetAlpha(mat2, 0f);
        plane1Renderer.gameObject.SetActive(true);
        plane2Renderer.gameObject.SetActive(false);
    }

    // --- FADE HELPERS ---

    IEnumerator Fade(Material mat, float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, t / fadeDuration);
            SetAlpha(mat, a);
            yield return null;
        }
        SetAlpha(mat, to);
    }

    IEnumerator Crossfade(Material fromMat, Material toMat)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = t / fadeDuration;

            SetAlpha(fromMat, Mathf.Lerp(1f, 0f, lerp));
            SetAlpha(toMat, Mathf.Lerp(0f, 1f, lerp));

            yield return null;
        }

        SetAlpha(fromMat, 0f);
        SetAlpha(toMat, 1f);
    }

    void SetAlpha(Material mat, float alpha)
    {
        if (!mat) return;

        if (mat.HasProperty("_BaseColor"))
        {
            Color c = mat.GetColor("_BaseColor");
            c.a = alpha;
            mat.SetColor("_BaseColor", c);
        }
        else if (mat.HasProperty("_Color"))
        {
            Color c = mat.GetColor("_Color");
            c.a = alpha;
            mat.SetColor("_Color", c);
        }
    }
}