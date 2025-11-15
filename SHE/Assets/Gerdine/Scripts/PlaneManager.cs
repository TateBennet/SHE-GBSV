using UnityEngine;
using System.Collections;

public class ThreePlaneFader : MonoBehaviour
{
    [Header("Plane Renderers (assign in Inspector)")]
    public Renderer plane1Renderer;
    public Renderer plane2Renderer;
    public Renderer plane3Renderer;

    [Header("Timing")]
    public float initialDelay = 0f;
    public float fadeDuration = 2f;
    public float visibleDuration = 3f;

    Material _mat1;
    Material _mat2;
    Material _mat3;

    void Start()
    {
        // Duplicate materials so alpha edits don’t affect shared materials
        if (plane1Renderer != null)
        {
            plane1Renderer.material = new Material(plane1Renderer.material);
            _mat1 = plane1Renderer.material;
            SetAlpha(_mat1, 0f);
            plane1Renderer.gameObject.SetActive(true);
        }

        if (plane2Renderer != null)
        {
            plane2Renderer.material = new Material(plane2Renderer.material);
            _mat2 = plane2Renderer.material;
            SetAlpha(_mat2, 0f);
            plane2Renderer.gameObject.SetActive(true);
        }

        if (plane3Renderer != null)
        {
            plane3Renderer.material = new Material(plane3Renderer.material);
            _mat3 = plane3Renderer.material;
            SetAlpha(_mat3, 0f);
            plane3Renderer.gameObject.SetActive(true);
        }

        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        if (initialDelay > 0f)
            yield return new WaitForSecondsRealtime(initialDelay);

        // PHASE 1: FADE IN PLANE 1
        yield return FadeRoutine(_mat1, 0f, 1f, fadeDuration);
        yield return new WaitForSecondsRealtime(visibleDuration);

        // PHASE 2: FADE FROM PLANE 1 INTO PLANE 2
        yield return FadeRoutine(_mat1, 1f, 0f, fadeDuration);
        yield return FadeRoutine(_mat2, 0f, 1f, fadeDuration);
        yield return new WaitForSecondsRealtime(visibleDuration);

        // PHASE 3: FADE FROM PLANE 2 INTO PLANE 3
        yield return FadeRoutine(_mat2, 1f, 0f, fadeDuration);
        yield return FadeRoutine(_mat3, 0f, 1f, fadeDuration);
        yield return new WaitForSecondsRealtime(visibleDuration);

        // PHASE 4: FADE FROM PLANE 3 BACK TO PLANE 1 (final)
        yield return FadeRoutine(_mat3, 1f, 0f, fadeDuration);
        yield return FadeRoutine(_mat1, 0f, 1f, fadeDuration);

        // Final state: Plane 1 fully visible, others hidden
        SetAlpha(_mat1, 1f);
        SetAlpha(_mat2, 0f);
        SetAlpha(_mat3, 0f);
    }

    IEnumerator FadeRoutine(Material mat, float startAlpha, float endAlpha, float duration)
    {
        if (mat == null || duration <= 0f)
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