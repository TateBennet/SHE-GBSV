using UnityEngine;

public class NotificationSFX : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip notificationClip;

    // 🔔 Call this to play your notification sound once
    public void PlayNotification()
    {
        if (audioSource && notificationClip)
        {
            audioSource.PlayOneShot(notificationClip);
        }
        else
        {
            Debug.LogWarning("NotificationSFX: Missing AudioSource or AudioClip reference!");
        }
    }
}
