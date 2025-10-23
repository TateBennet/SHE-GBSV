using UnityEngine;
using System.Collections; // 👈 Needed for coroutines


public class VballSFX : MonoBehaviour
{
    public AudioClip hitSound;
    public AudioSource audioSource;
    public float cooldown = 0.2f; // seconds between allowed hits
    public float functionDelay = 2f; // delay before OnFirstHit/OnSecondHit runs

    public static int counter = 0;
    private float lastHitTime = -999f;

    void OnCollisionEnter(Collision collision)
    {

        // hand hit
        if (collision.collider.CompareTag("Hand") && Time.time - lastHitTime > cooldown)
        {

            lastHitTime = Time.time;
            audioSource.PlayOneShot(hitSound);
            counter++;
            Debug.Log("volleyball hit " + counter + " times!");
        }

        // Floor hit
        if (collision.collider.CompareTag("Floor"))
        {
            Debug.Log("volleyball hit the floor — deactivating");
            gameObject.SetActive(false);
        }
    }

}
