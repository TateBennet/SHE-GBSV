using UnityEngine;

public class AudioMover : MonoBehaviour
{
    public AudioSource coach;
    public AudioSource coachCD;
    public AudioSource headCoach;
    public AudioSource guys;
    public Vector3 newPosition; // set in Inspector
    public Vector3 newPosition2;
    public Vector3 newPosition3;
    public Vector3 newPosition4;
    public Vector3 newPosition5;

    public void MoveTo()
    {
        if (coach == null)
        {
            Debug.LogWarning("MoveAudioSource: No AudioSource assigned!");
            return;
        }

        coach.transform.position = newPosition;
    }

    public void MoveTo2()
    {
        if (headCoach == null)
        {
            Debug.LogWarning("MoveAudioSource: No AudioSource assigned!");
            return;
        }

        headCoach.transform.position = newPosition2;
    }

    public void MoveTo3()
    {
        if (coachCD == null)
        {
            Debug.LogWarning("MoveAudioSource: No AudioSource assigned!");
            return;
        }

        coachCD.transform.position = newPosition3;
    }

    public void MoveTo4()
    {
        if (guys == null)
        {
            Debug.LogWarning("MoveAudioSource: No AudioSource assigned!");
            return;
        }

        guys.transform.position = newPosition4;
    }

    public void MoveTo5()
    {
        if (headCoach == null)
        {
            Debug.LogWarning("MoveAudioSource: No AudioSource assigned!");
            return;
        }

        headCoach.transform.position = newPosition5;
    }

    // Optional utility for scripting
    public void MoveTo(Vector3 pos) => coach.transform.position = pos;
}
