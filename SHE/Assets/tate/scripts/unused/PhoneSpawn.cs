using UnityEngine;

public class PhoneSpawn : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The phone prefab (can be this object if script is attached to phone).")]
    public GameObject phone;

    [Tooltip("The player's head transform (usually Main Camera under XR Origin).")]
    public Transform playerHead;

    [Header("Placement Settings")]
    [Tooltip("Distance from the player’s head (meters).")]
    public float distance = 0.5f;

    [Tooltip("Vertical offset (meters) above/below head height).")]
    public float verticalOffset = -0.1f;

    [Tooltip("Whether the phone should face the player directly.")]
    public bool facePlayer = true;

    private void Start()
    {
        if (phone) phone.SetActive(false); // start hidden
    }

    /// <summary>
    /// Call this method to activate and place the phone in front of the player.
    /// </summary>
    public void ActivatePhone()
    {
        if (!phone || !playerHead)
        {
            Debug.LogWarning("PhoneActivator: missing reference(s).");
            return;
        }

        // Place in front of player head
        Vector3 spawnPos = playerHead.position
                         + playerHead.forward * distance
                         + Vector3.up * verticalOffset;

        phone.transform.position = spawnPos;

        if (facePlayer)
        {
            // Rotate to face the player
            Vector3 lookDir = (playerHead.position - phone.transform.position).normalized;
            phone.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up) * Quaternion.Euler(0, 0, 180f);
        }

        phone.SetActive(true);
    }

    /// <summary>
    /// Call this to hide the phone again if needed.
    /// </summary>
    public void DeactivatePhone()
    {
        if (phone) phone.SetActive(false);
    }
}
