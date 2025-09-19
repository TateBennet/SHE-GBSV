using UnityEngine;

public class PhoneGrabber : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The visual hand GameObject to hide (left or right).")]
    public GameObject handVisual;  // drag your hand mesh here
    [Tooltip("The wrist/palm transform to parent the phone to.")]
    public Transform handPalm;     // drag the wrist or palm bone here

    [Header("Grab Settings")]
    public Vector3 localPositionOffset;
    public Vector3 localRotationOffset;

    private bool grabbed = false;

    void OnTriggerEnter(Collider other)
    {
        if (grabbed) return;

        // Make sure it's the hand collider
        if (other.CompareTag("PlayerHand")) // add this tag to your hand colliders
        {
            Debug.Log("Phone grabbed by: " + other.name);

            // Hide the hand visual
            if (handVisual) handVisual.SetActive(false);

            // Parent phone to palm
            if (handPalm)
            {
                transform.SetParent(handPalm);
                transform.localPosition = localPositionOffset;
                transform.localRotation = Quaternion.Euler(localRotationOffset);
            }

            grabbed = true;
        }
    }
}