using UnityEngine;
using System.Collections;

public class TimedObjectEnabler : MonoBehaviour
{
    [Tooltip("Objects that will be enabled at specific times (seconds after Start).")]
    public GameObject[] objects;

    [Tooltip("Time in seconds from Play when each object should enable.")]
    public float[] delays;

    void Start()
    {
        StartCoroutine(EnableObjectsAtTimes());
    }

    IEnumerator EnableObjectsAtTimes()
    {
        if (objects == null || delays == null || objects.Length != delays.Length)
        {
            Debug.LogError("Objects and delays arrays must have the same length!", this);
            yield break;
        }

        // Record when the experience starts
        float startTime = Time.realtimeSinceStartup;

        for (int i = 0; i < objects.Length; i++)
        {
            float targetTime = startTime + delays[i];

            // Wait until the target time is reached
            while (Time.realtimeSinceStartup < targetTime)
                yield return null;

            if (objects[i] != null)
            {
                objects[i].SetActive(true);
                Debug.Log($"Enabled {objects[i].name} at {delays[i]} seconds (absolute)");
            }
        }
    }
}