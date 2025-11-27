using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QuitWithFadeToThankYou : MonoBehaviour
{
    [Header("Fade Overlay (Full-Screen Black)")]
    [Tooltip("CanvasGroup on a full-screen black Image (Panel).")]
    public CanvasGroup fadeOverlay;

    [Header("Thank You Screen (High-Res Image)")]
    [Tooltip("CanvasGroup on your high-res 'Thank you' Image/Panel.")]
    public CanvasGroup thankYouScreen;

    [Header("Timings (Seconds)")]
    [Tooltip("Time to fade from game to black.")]
    public float fadeToBlackDuration = 1.5f;

    [Tooltip("Time to fade in the thank-you screen.")]
    public float thankYouFadeInDuration = 1f;

    [Tooltip("How long to keep the thank-you screen visible before quitting.")]
    public float thankYouDisplayTime = 2f;

    private bool isQuitting;

    /// <summary>
    /// Call this from your Quit button's OnClick event.
    /// </summary>
    public void OnQuitButtonPressed()
    {
        if (isQuitting) return;
        isQuitting = true;

        StartCoroutine(QuitSequence());
    }

    private IEnumerator QuitSequence()
    {
        // Make sure overlay exists
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.blocksRaycasts = true; // block input during fade
        }

        // 1) Fade to black
        yield return StartCoroutine(FadeCanvasGroup(fadeOverlay, 0f, 1f, fadeToBlackDuration));

        // 2) Fade in thank-you screen (your high-res image)
        if (thankYouScreen != null)
        {
            thankYouScreen.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(thankYouScreen, 0f, 1f, thankYouFadeInDuration));
            yield return new WaitForSecondsRealtime(thankYouDisplayTime);
        }
        else
        {
            // Hold on black briefly if no thank-you screen
            yield return new WaitForSecondsRealtime(0.5f);
        }

        // 3) Quit the game
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null)
            yield break;

        if (duration <= 0f)
        {
            cg.alpha = to;
            yield break;
        }

        cg.alpha = from;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime; // ignore timescale changes
            float t = Mathf.Clamp01(time / duration);
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        cg.alpha = to;
    }
}
