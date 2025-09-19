using UnityEngine;
using System.Collections;

public class PhoneMaterialSwitch : MonoBehaviour
{
    [Header("Screen Settings")]
    public Texture[] screenTextures;       // One per screen
    public GameObject[] screenButtons;     // Matching colliders for each screen
    public Renderer phoneRenderer;         // Your phone’s Mesh Renderer
    public int screenMaterialIndex = 1;    // Which material slot is the screen
    public float fadeDuration = 0.5f;

    private int currentIndex = 0;
    private Material screenMaterial;
    private bool isFading = false;

    private void Start()
    {
        screenMaterial = phoneRenderer.materials[screenMaterialIndex];

        if (screenTextures.Length > 0)
            screenMaterial.SetTexture("_BaseMap", screenTextures[0]);

        ShowButton(0);
    }

    public void TapButton()  // Called by button scripts
    {
        if (!isFading)
            NextScreen();
    }

    private void NextScreen()
    {
        currentIndex = (currentIndex + 1) % screenTextures.Length;
        StartCoroutine(FadeToTexture(screenTextures[currentIndex]));
        ShowButton(currentIndex);
    }

    private IEnumerator FadeToTexture(Texture newTexture)
    {
        isFading = true;
        Color startColor = screenMaterial.color;

        // Fade out
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float normalized = t / fadeDuration;
            screenMaterial.color = new Color(startColor.r, startColor.g, startColor.b, 1f - normalized);
            yield return null;
        }

        // Swap texture
        screenMaterial.SetTexture("_BaseMap", newTexture);

        // Fade in
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float normalized = t / fadeDuration;
            screenMaterial.color = new Color(startColor.r, startColor.g, startColor.b, normalized);
            yield return null;
        }

        screenMaterial.color = Color.white;
        isFading = false;
    }

    private void ShowButton(int index)
    {
        for (int i = 0; i < screenButtons.Length; i++)
        {
            if (screenButtons[i] != null)
                screenButtons[i].SetActive(i == index);
        }
    }
}
