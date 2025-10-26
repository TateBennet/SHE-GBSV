using UnityEngine;
using System.Collections;

public class CollisionNotify : MonoBehaviour
{
    [Tooltip("The object to mirror active state from.")]
    public GameObject targetObject;

    [Tooltip("The hand tag to detect.")]
    public string handTag = "Hand";

    public BallServer ballLogic;

    private bool isCooldown = false;

    void Update()
    {
        if (targetObject != null)
        {
            // Mirror the active state of the target object
            if (gameObject.activeSelf != targetObject.activeSelf)
                gameObject.SetActive(targetObject.activeSelf);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(handTag) && !isCooldown)
        {
            ballLogic.Serve();
            StartCoroutine(TemporarilyDisableBoth());
        }
    }

    private IEnumerator TemporarilyDisableBoth()
    {
        isCooldown = true;

        // Temporarily disable both
        if (targetObject != null)
            targetObject.SetActive(false);

        gameObject.SetActive(false);

        // Wait for 3 seconds
        yield return new WaitForSeconds(3f);

        // Reactivate both
        if (targetObject != null)
            targetObject.SetActive(true);

        gameObject.SetActive(true);

        isCooldown = false;
    }
}
