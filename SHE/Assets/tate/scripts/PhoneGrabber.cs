using UnityEngine;

public class PhoneGrabber : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The phone prefab (can be this object if script is attached to phone).")]
    public GameObject phone;
    [Tooltip("The visual hand GameObject to hide (left or right).")]
    public GameObject handVisual;  // drag your hand mesh here
    [Tooltip("The wrist/palm transform to parent the phone to.")]
    public Transform handPalm;     // drag the wrist or palm bone here

    [Header("Grab Settings")]
    public Vector3 localPositionOffset;
    public Vector3 localRotationOffset;

    private void Start()
    {
        if (phone) phone.SetActive(false); // start hidden
    }

    public void ActivatePhone()
    {
        phone.SetActive(true);

        // Hide the hand visual
        if (handVisual) handVisual.SetActive(false);

        // Parent phone to palm
        if (handPalm)
        {
            transform.SetParent(handPalm);
            transform.localPosition = localPositionOffset;
            transform.localRotation = Quaternion.Euler(localRotationOffset);
        }
    }
}