using UnityEngine;
using UnityEngine.SceneManagement;

public class VBallSceneLoadIntro : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip hitSound;
    public AudioSource audioSource;

    [Header("Scene Transition")]
    [Tooltip("The name of the scene to load when the ball hits the floor.")]
    public string sceneToLoad;
    private int counter;
    private int handcounter = 0;

    public void ReleaseFromAnimation()
    {
        if (TryGetComponent(out Animator anim))
        {
            anim.enabled = false;
            Debug.Log("✅ Animation finished — Animator disabled, object released.");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 🖐️ Hand hit
        if (collision.collider.CompareTag("Hand"))
        {
            handcounter++;
            // Play hit sound
            if (audioSource && hitSound)
                audioSource.PlayOneShot(hitSound);
        }

        // 🧱 Floor hit
        if (collision.collider.CompareTag("Floor") && handcounter > 0)
        {
            counter++;
            Debug.Log("Volleyball hit the floor " + counter + " times, will load next scene after 3 bounces");

            if (!string.IsNullOrEmpty(sceneToLoad) && counter == 3)
            {
                SceneManager.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogWarning("No scene assigned to load on floor hit!");
            }
        }
    }
}