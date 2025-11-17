using UnityEngine;

[System.Serializable]
public class AnimationEntry
{
    public Animator animator;       // The animator to use
    public string animationName;    // The animation to play on that animator
}

public class Chooselocker1 : MonoBehaviour
{
    [Header("Objects to Disable")]
    public GameObject[] objectsToDisable;

    [Header("Animations To Play")]
    public AnimationEntry[] animations;   // <-- multiple animators + animations

    // Disable all target objects
    public void DisableObjects()
    {
        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    // Play all animations
    public void PlayAnimations()
    {
        if (animations == null || animations.Length == 0)
        {
            Debug.LogWarning("No animations assigned!");
            return;
        }

        foreach (AnimationEntry entry in animations)
        {
            if (entry.animator != null && !string.IsNullOrEmpty(entry.animationName))
            {
                entry.animator.Play(entry.animationName);
            }
        }
    }
}
