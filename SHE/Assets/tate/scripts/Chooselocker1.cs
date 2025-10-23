using UnityEngine;

public class Chooselocker1 : MonoBehaviour
{
    [Header("Objects to Disable")]
    public GameObject[] objectsToDisable;

    [Header("Animation Settings")]
    public Animator animator;        // Assign in Inspector
    public string animationName;     // The name of the animation to play

    // Disable all the target GameObjects
    public void DisableObjects()
    {
        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    // Play a specific animation on the assigned Animator
    public void PlayAnimation()
    {
        if (animator != null && !string.IsNullOrEmpty(animationName))
        {
            animator.Play(animationName);
        }
        else
        {
            Debug.LogWarning("Animator or animation name not set!");
        }
    }
}