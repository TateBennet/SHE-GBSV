using UnityEngine;
using System.Collections;
public class IntroSequenceManager : MonoBehaviour
{
    [Header("Disclaimer")]
    [Tooltip("CanvasGroup on the disclaimer canvas.")]
    public CanvasGroup disclaimerCanvas;
    [Tooltip("Seconds the disclaimer stays visible before fading.")]
    public float holdTime = 5f;
    [Tooltip("Seconds the disclaimer takes to fade out.")]
    public float disclaimerFadeDuration = 1.5f;

    [Header("Scenario Canvases")]
    [Tooltip("CanvasGroup for Scenario A (e.g., Volleyball).")]
    public CanvasGroup scenarioCanvasA;
    [Tooltip("CanvasGroup for Scenario B (e.g., Social Media).")]
    public CanvasGroup scenarioCanvasB;

    [Header("Scenario Appearance")]
    [Tooltip("If true, scenario canvases will fade in; otherwise they pop in.")]
    public bool fadeInScenarios = true;
    [Tooltip("Seconds the scenario canvases take to fade in (if enabled).")]
    public float scenarioFadeInDuration = 1.0f;

    bool _running;

    void Awake()
    {
        // Ensure initial states
        if (disclaimerCanvas)
        {
            disclaimerCanvas.gameObject.SetActive(true);
            disclaimerCanvas.alpha = 1f;
        }

        // Start with both scenario canvases disabled/hidden
        SetupScenarioCanvas(scenarioCanvasA, false);
        SetupScenarioCanvas(scenarioCanvasB, false);
    }

    void OnEnable()
    {
        if (!_running) StartCoroutine(Co_Run());
    }

    void SetupScenarioCanvas(CanvasGroup cg, bool active)
    {
        if (!cg) return;
        cg.gameObject.SetActive(active);
        cg.alpha = active ? (fadeInScenarios ? 0f : 1f) : 0f;
        // If you have interactables on these canvases, you can also control
        // cg.interactable / cg.blocksRaycasts here as needed.
        cg.interactable = active;
        cg.blocksRaycasts = active;
    }

    IEnumerator Co_Run()
    {
        _running = true;

        // 1) Hold disclaimer visible
        yield return new WaitForSeconds(holdTime);

        // 2) Fade out disclaimer
        if (disclaimerCanvas && disclaimerFadeDuration > 0f)
        {
            float t = 0f;
            while (t < disclaimerFadeDuration)
            {
                t += Time.deltaTime;
                disclaimerCanvas.alpha = Mathf.Lerp(1f, 0f, t / disclaimerFadeDuration);
                yield return null;
            }
            disclaimerCanvas.alpha = 0f;
            disclaimerCanvas.gameObject.SetActive(false);
        }
        else if (disclaimerCanvas)
        {
            disclaimerCanvas.alpha = 0f;
            disclaimerCanvas.gameObject.SetActive(false);
        }

        // 3) Enable scenario canvases
        SetupScenarioCanvas(scenarioCanvasA, true);
        SetupScenarioCanvas(scenarioCanvasB, true);

        // 4) Optional fade-in for scenarios
        if (fadeInScenarios && scenarioFadeInDuration > 0f)
        {
            float t = 0f;
            float a0 = scenarioCanvasA ? scenarioCanvasA.alpha : 0f;
            float b0 = scenarioCanvasB ? scenarioCanvasB.alpha : 0f;

            // ensure they start at 0 alpha when fading in
            if (scenarioCanvasA) scenarioCanvasA.alpha = 0f;
            if (scenarioCanvasB) scenarioCanvasB.alpha = 0f;

            while (t < scenarioFadeInDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / scenarioFadeInDuration);
                if (scenarioCanvasA) scenarioCanvasA.alpha = a;
                if (scenarioCanvasB) scenarioCanvasB.alpha = a;
                yield return null;
            }
            if (scenarioCanvasA) scenarioCanvasA.alpha = 1f;
            if (scenarioCanvasB) scenarioCanvasB.alpha = 1f;
        }

        _running = false;
    }
}
