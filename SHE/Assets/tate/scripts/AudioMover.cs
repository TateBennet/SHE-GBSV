using UnityEngine;

public class AudioMover : MonoBehaviour
{
    public AudioSource targetSource;
    public AudioSource targetSource2;
    public Vector3 newPosition; // set in Inspector
    public Vector3 newPosition2;

    public void MoveTo()
    {
        if (targetSource == null)
        {
            Debug.LogWarning("MoveAudioSource: No AudioSource assigned!");
            return;
        }

        targetSource.transform.position = newPosition;
    }

    public void MoveTo2()
    {
        if (targetSource == null)
        {
            Debug.LogWarning("MoveAudioSource: No AudioSource assigned!");
            return;
        }

        targetSource2.transform.position = newPosition2;
    }

    // Optional utility for scripting
    public void MoveTo(Vector3 pos) => targetSource.transform.position = pos;
}
