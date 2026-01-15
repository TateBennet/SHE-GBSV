using System.Collections;
using UnityEngine;

public class FadeToBlack : MonoBehaviour
{
    public Renderer fadePlaneRenderer;
    public float fadeDuration = 1f;
    public GameObject plane;

    public void Blackout()
    {
        plane.SetActive(true);
        StartCoroutine(Fade(1f));
    }

    public void TurnOffPlane()
    {
        StartCoroutine(WaitTurnOff());
    }

    public void WhiteOut()
    {
        StartCoroutine(Fade(0f));
    }

    private IEnumerator WaitTurnOff()
    {
        yield return new WaitForSeconds(1);
        plane.SetActive(false);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        Material mat = fadePlaneRenderer.material;
        Color startColor = mat.color;
        float startAlpha = startColor.a;
        float t = 0;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            mat.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        mat.color = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
    }
}
