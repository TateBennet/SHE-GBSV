using UnityEngine;
using System.Collections;

public class DelayedObjectEnable : MonoBehaviour
{
    [Tooltip("Assign the object you want to enable after a short delay.")]
    public GameObject targetObject;

    [Tooltip("Time to wait before enabling (in seconds).")]
    public float delay = 3f;

    void Start()
    {
        if (targetObject != null)
            StartCoroutine(EnableAfterDelay());
        else
            Debug.LogWarning("No target object assigned on " + gameObject.name);
    }

    IEnumerator EnableAfterDelay()
    {
        yield return new WaitForSeconds(delay);
        targetObject.SetActive(true);
    }
}