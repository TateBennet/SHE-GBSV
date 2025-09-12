using UnityEngine;

public class DisclaimerHide : MonoBehaviour
{
    [Tooltip("Seconds the disclaimer stays fully visible before fading.")]
    public float holdTime = 5f;

    [Tooltip("Seconds it takes to fade out.")]
    public float fadeDuration = 1.5f;

    private CanvasGroup canvasGroup;
    private float timer = 0f;
    private bool fading = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f; // start fully visible
    }

    void Update()
    {
        if (!fading)
        {
            timer += Time.deltaTime;
            if (timer >= holdTime)
            {
                fading = true;
                timer = 0f;
            }
        }
        else
        {
            float t = timer / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            timer += Time.deltaTime;

            if (t >= 1f)
            {
                gameObject.SetActive(false); // hide completely after fade
            }
        }
    }
}
