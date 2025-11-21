using System.Collections;
using UnityEngine;

public class FloorReset : MonoBehaviour
{
    public Transform ballRespawnPoint;   // Assign in Inspector
    public float resetDelay = 0.5f;
    public VballSFX bounceSound;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Ball"))
        {
            ResetBall(collision.collider.gameObject);
        }
    }

    private void ResetBall(GameObject ball)
    {
        bounceSound.audioSource.PlayOneShot(bounceSound.hitSound);
        StartCoroutine(Respawn(ball));
    }

    private IEnumerator Respawn(GameObject ball)
    {
        yield return new WaitForSeconds(resetDelay);

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Move ball to respawn
        ball.transform.position = ballRespawnPoint.position;
        ball.transform.rotation = ballRespawnPoint.rotation;
    }
}
