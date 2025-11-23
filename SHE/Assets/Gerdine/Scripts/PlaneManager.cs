using UnityEngine;
using System.Collections;

public class MultiPlaneFader : MonoBehaviour
{
    [Header("Plane Renderers (assign in Inspector, in order)")]
    public Renderer[] planeRenderers;

    [Header("Timing")]
    public float initialDelay = 0f;
    public float fadeDuration = 2f;

    [Tooltip("If left empty or wrong size, defaultVisibleDuration is used for all planes.")]
    public float[] visibleDurations;
    public float defaultVisibleDuration = 3f;

    private Material[] _planeMats;

    void Start()
    {
        if (planeRenderers == null || planeRenderers.Length == 0)
        {
            Debug.LogWarning("MultiPlaneFader: No planeRenderers assigned.");
            return;
        }

        _planeMats = new Material[planeRenderers.Length];

        // Duplicate materials and start all planes invisible but active
        for (int i = 0; i < planeRenderers.Length; i++)
        {
            if (planeRenderers[i] == null) continue;

            planeRenderers[i].material = new Material(planeRenderers[i].material);
            _planeMats[i] = planeRenderers[i].material;

            SetAlpha(_planeMats[i], 0f);
            planeRenderers[i].gameObject.SetActive(true);
        }

        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        if (initialDelay > 0f)
            yield return new WaitForSecondsRealtime(initialDelay);

        int count = _planeMats.Length;
        if (count == 0 || _planeMats[0] == null)
            yield break;

        // 1) Fade in first plane
        yield return FadeRoutine(_planeMats[0], 0f, 1f, fadeDuration);
        yield return new WaitForSecondsRealtime(GetVisibleDuration(0));

        // 2) Go through plane 0 -> 1 -> 2 -> ... -> last
        for (int i = 0; i < count - 1; i++)
        {
            Material current = _planeMats[i];
            Material next = _planeMats[i + 1];

            if (current != null)
                yield return FadeRoutine(current, 1f, 0f, fadeDuration);

            if (next != null)
            {
                yield return FadeRoutine(next, 0f, 1f, fadeDuration);
                yield return new WaitForSecondsRealtime(GetVisibleDuration(i + 1));
            }
        }

        // 3) Fade from last plane back to first and stay there
        int lastIndex = count - 1;
        Material lastMat = _planeMats[lastIndex];

        if (lastMat != null)
            yield return FadeRoutine(lastMat, 1f, 0f, fadeDuration);

        yield return FadeRoutine(_planeMats[0], 0f, 1f, fadeDuration);

        // Final state: first plane visible, all others hidden
        for (int i = 0; i < count; i++)
        {
            if (_planeMats[i] == null) continue;
            SetAlpha(_planeMats[i], (i == 0) ? 1f : 0f);
        }
    }

    float GetVisibleDuration(int index)
    {
        if (visibleDurations != null &&
            visibleDurations.Length == _planeMats.Length &&
            index >= 0 && index < visibleDurations.Length)
        {
            return Mathf.Max(0f, visibleDurations[index]);
        }

        return Mathf.Max(0f, defaultVisibleDuration);
    }

    IEnumerator FadeRoutine(Material mat, float startAlpha, float endAlpha, float duration)
    {
        if (mat == null)
            yield break;

        if (duration <= 0f)
        {
            SetAlpha(mat, endAlpha);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            float a = Mathf.Lerp(startAlpha, endAlpha, lerp);
            SetAlpha(mat, a);
            yield return null;
        }

        SetAlpha(mat, endAlpha);
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
}