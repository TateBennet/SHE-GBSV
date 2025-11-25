using UnityEngine;
using System.Collections;

public class TimedSceneManager : MonoBehaviour
{
    [Header("Start Behaviour")]
    [Tooltip("If true, sequence starts automatically on scene load (old behaviour). If false, you must call BeginSequence() from a button.")]
    public bool autoStartOnAwake = false;

    [Header("Plane Crossfade (Videos)")]
    public Renderer firstPlaneRenderer;
    public Renderer secondPlaneRenderer;
    public float planeFadeStartTime = 10f;
    public float planeFadeDuration = 2f;
    public bool disableFirstPlaneAfterFade = true;

    [Header("Title Text (stays until end)")]
    public GameObject titleText;
    public float titleAppearTime = 0f; // seconds from SEQUENCE start

    [Header("Other Texts (timed show/hide)")]
    public GameObject[] textObjects;
    public float[] textAppearTimes;      // when each should appear (from SEQUENCE start)
    public float[] textVisibleDurations; // how long they stay visible (>0 = auto-hide)

    Material _firstMat;
    Material _secondMat;

    bool _sequenceStarted = false;

    void Start()
    {
        // --- plane setup ---
        if (firstPlaneRenderer != null)
        {
            firstPlaneRenderer.material = new Material(firstPlaneRenderer.material);
            _firstMat = firstPlaneRenderer.material;
            SetAlpha(_firstMat, 1f);
            firstPlaneRenderer.gameObject.SetActive(true);
        }

        if (secondPlaneRenderer != null)
        {
            secondPlaneRenderer.material = new Material(secondPlaneRenderer.material);
            _secondMat = secondPlaneRenderer.material;
            SetAlpha(_secondMat, 0f);
            secondPlaneRenderer.gameObject.SetActive(true);
        }

        // --- text setup ---

        // Title off at start
        if (titleText != null)
            titleText.SetActive(false);

        // Other texts off at start
        if (textObjects != null)
        {
            foreach (var t in textObjects)
                if (t != null) t.SetActive(false);
        }

        // Basic array length check for other texts
        if (textObjects != null && textAppearTimes != null && textVisibleDurations != null)
        {
            if (textObjects.Length != textAppearTimes.Length ||
                textObjects.Length != textVisibleDurations.Length)
            {
                Debug.LogError("Text arrays must all have the same length!", this);
            }
        }

        // Only auto-start if you explicitly want that
        if (autoStartOnAwake)
        {
            BeginSequence();
        }
    }

    /// <summary>
    /// Call this from your button to start the entire timed sequence.
    /// </summary>
    public void BeginSequence()
    {
        if (_sequenceStarted)
            return;

        _sequenceStarted = true;

        StartCoroutine(CrossfadePlanesRoutine());
        StartCoroutine(HandleTitleText());
        StartCoroutine(HandleAllTextsRoutine());
    }

    // ---------- planes ----------

    IEnumerator CrossfadePlanesRoutine()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, planeFadeStartTime));

        float t = 0f;
        while (t < planeFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / planeFadeDuration);

            if (_firstMat != null) SetAlpha(_firstMat, 1f - lerp);
            if (_secondMat != null) SetAlpha(_secondMat, lerp);

            yield return null;
        }

        if (_firstMat != null) SetAlpha(_firstMat, 0f);
        if (_secondMat != null) SetAlpha(_secondMat, 1f);

        if (disableFirstPlaneAfterFade && firstPlaneRenderer != null)
            firstPlaneRenderer.gameObject.SetActive(false);
    }

    void SetAlpha(Material mat, float alpha)
    {
        if (mat == null) return;

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

    // ---------- title text (shows once, stays on) ----------

    IEnumerator HandleTitleText()
    {
        if (titleText == null)
            yield break;

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, titleAppearTime));

        titleText.SetActive(true); // enable once, never touched again
    }

    // ---------- other texts (appear, then hide) ----------

    IEnumerator HandleAllTextsRoutine()
    {
        if (textObjects == null || textAppearTimes == null || textVisibleDurations == null)
            yield break;

        if (textObjects.Length != textAppearTimes.Length ||
            textObjects.Length != textVisibleDurations.Length)
            yield break;

        for (int i = 0; i < textObjects.Length; i++)
        {
            if (textObjects[i] != null)
                StartCoroutine(HandleSingleText(i));
        }
    }

    IEnumerator HandleSingleText(int index)
    {
        GameObject textObj = textObjects[index];
        float appearTime = Mathf.Max(0f, textAppearTimes[index]);
        float visibleDuration = textVisibleDurations[index];

        yield return new WaitForSecondsRealtime(appearTime);

        if (textObj != null)
            textObj.SetActive(true);

        if (visibleDuration <= 0f)
            yield break; // stay on forever

        yield return new WaitForSecondsRealtime(visibleDuration);

        if (textObj != null)
            textObj.SetActive(false);
    }
}