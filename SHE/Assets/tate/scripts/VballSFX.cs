using UnityEngine;
using System.Collections; // 👈 Needed for coroutines


public class VballSFX : MonoBehaviour
{
    public AudioClip hitSound;
    public AudioSource audioSource;
    public float cooldown = 0.2f; // seconds between allowed hits
    public float functionDelay = 2f; // delay before OnFirstHit/OnSecondHit runs

    public static int counter = 0;
    public int bounceCount = 0;
    public static bool failed = false;
    public static int iterations = 0;
    private float lastHitTime = -999f;

    void OnCollisionEnter(Collision collision)
    {

        // hand hit
        if (collision.collider.CompareTag("Hand") && Time.time - lastHitTime > cooldown)
        {

            lastHitTime = Time.time;
            audioSource.PlayOneShot(hitSound);
            counter++;
            iterations++;
            failed = false;
            Debug.Log("volleyball hit " + counter + " times!");
        }

        // Floor hit
        if (collision.collider.CompareTag("Floor"))
        {
            bounceCount++;
            Debug.Log("volleyball hit the floor");
            if(bounceCount >= 2)
            {
                gameObject.SetActive(false);
                bounceCount = 0;
            }
                
        }

        // Player hit (miss)
        if (collision.collider.CompareTag("Player"))
        {
            BallMissed();
        }
    }

    public void BallMissed()
    {
        iterations++;
        failed = true;
        Debug.Log("volleyball hit the player and counts as miss, loading fail scene...");

    }

    public void ResetStats()
    {
        iterations = 0;
        counter = 0;
        Debug.Log("vball counter was reset!" + " counter is: " + counter + " iterations is: " + iterations);
    }

}
