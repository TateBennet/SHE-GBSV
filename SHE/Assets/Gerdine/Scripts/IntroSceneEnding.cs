using UnityEngine;
using UnityEngine.SceneManagement;

public class LockerRoomSequenceManager : MonoBehaviour
{
    [Header("Voiceover")]
    public AudioSource voiceoverSource;

    [Header("UI")]
    public GameObject startButton;               // Shows up when VO is done

    [Header("Locker Room")]
    public GameObject lockerRoomContainer;       // Empty with locker room + Animator
    public Animator lockerRoomAnimator;          // Animator on that empty
    [Tooltip("Name of the animation state to wait for (e.g. 'BuildingSlow'). Leave empty to use whatever state plays first.")]
    public string animationStateName;

    [Header("Light")]
    public Light lockerRoomLight;                // Light to enable after animation is finished

    [Header("Scene Change")]
    public string nextSceneName;                 // Next scene to load
    public float delayAfterLight = 1f;           // Pause before switching scenes

    [Header("Objects to Disable When Start is Clicked")]
    public GameObject[] objectsToDisableOnStart; // Your 2 objects to hide

    private bool voiceoverHasStarted = false;
    private bool buttonShown = false;
    private bool sequenceStarted = false;

    private void Start()
    {
        // Button hidden at start
        if (startButton != null)
            startButton.SetActive(false);

        // Locker room hidden at start
        if (lockerRoomContainer != null)
            lockerRoomContainer.SetActive(false);

        // Light off at start (keep GameObject active so it can show later)
        if (lockerRoomLight != null)
        {
            lockerRoomLight.gameObject.SetActive(true);
            lockerRoomLight.enabled = false;
        }
    }

    private void Update()
    {
        if (voiceoverSource == null)
            return;

        // Detect when VO actually starts
        if (!voiceoverHasStarted && voiceoverSource.isPlaying)
        {
            voiceoverHasStarted = true;
        }

        // When VO has started and then stops ? show button once
        if (voiceoverHasStarted && !voiceoverSource.isPlaying && !buttonShown)
        {
            ShowStartButton();
        }
    }

    private void ShowStartButton()
    {
        buttonShown = true;

        if (startButton != null)
            startButton.SetActive(true);
    }

    // Hook this to the START BUTTON's OnClick()
    public void OnStartButtonClicked()
    {
        if (sequenceStarted)
            return;

        sequenceStarted = true;

        // Hide the button
        if (startButton != null)
            startButton.SetActive(false);

        // Disable the objects you don't want anymore
        if (objectsToDisableOnStart != null)
        {
            foreach (GameObject obj in objectsToDisableOnStart)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        // Enable the locker room (Animator default state will start)
        if (lockerRoomContainer != null)
            lockerRoomContainer.SetActive(true);

        // Wait for the animation to finish, then do light + scene
        if (lockerRoomAnimator != null)
            StartCoroutine(WaitForAnimationThenLightAndScene());
        else
            Debug.LogWarning("LockerRoomSequenceManager: LockerRoomAnimator is not assigned.");
    }

    private System.Collections.IEnumerator WaitForAnimationThenLightAndScene()
    {
        // Give the Animator one frame to enter its first state
        yield return null;

        // Safety timeout in case something goes weird
        float timeout = 30f; // seconds
        float timer = 0f;

        while (true)
        {
            AnimatorStateInfo stateInfo = lockerRoomAnimator.GetCurrentAnimatorStateInfo(0);

            bool correctState = true;

            if (!string.IsNullOrEmpty(animationStateName))
            {
                // Only proceed when we're actually in the target state
                correctState = stateInfo.IsName(animationStateName);
            }

            if (correctState && !lockerRoomAnimator.IsInTransition(0) && stateInfo.normalizedTime >= 1f)
            {
                // Animation has reached (or passed) the end
                break;
            }

            timer += Time.deltaTime;
            if (timer > timeout)
            {
                Debug.LogWarning("LockerRoomSequenceManager: Wait for animation timed out.");
                break;
            }

            yield return null;
        }

        // Turn on the light
        if (lockerRoomLight != null)
            lockerRoomLight.enabled = true;

        // Small pause so you can actually see the lit locker room
        if (delayAfterLight > 0f)
            yield return new WaitForSeconds(delayAfterLight);

        // Change scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("LockerRoomSequenceManager: Next scene name is not set.");
        }
    }
}