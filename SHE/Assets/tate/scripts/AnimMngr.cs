using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TimedAnimation
{
    public Animator animator;        // The Animator component on the object
    public string animationName;     // The animation trigger or state name
    public float delay;              // Delay before playing
}

public class AnimMngr : MonoBehaviour
{
    [Header("Animations to Play")]
    public List<TimedAnimation> animations = new List<TimedAnimation>();

    public void StartAnimations()
    {
        foreach (var anim in animations)
        {
            if (anim.animator != null)
                StartCoroutine(PlayAfterDelay(anim));
        }
    }

    private IEnumerator PlayAfterDelay(TimedAnimation anim)
    {
        yield return new WaitForSeconds(anim.delay);
        anim.animator.Play(anim.animationName);
    }
}
