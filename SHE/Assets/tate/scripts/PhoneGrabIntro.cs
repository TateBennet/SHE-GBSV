using UnityEngine;

public class PhoneGrabIntro : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The phone prefab (can be this object if script is attached to phone).")]
    public GameObject phone;
    public GameObject tooltip;
    [Tooltip("The visual hand GameObject to hide (left or right).")]
    public GameObject handVisual;  // drag your hand mesh here
    [Tooltip("The wrist/palm transform to parent the phone to.")]
    public Transform handPalm;     // drag the wrist or palm bone here

    [Header("Grab Settings")]
    public Vector3 localPositionOffset;
    public Vector3 localRotationOffset;

    [Header("Material Settings")]
    [Tooltip("Renderer that holds the phone materials.")]
    public Renderer phoneRenderer;
    [Tooltip("Index of material to change (0 = first, 1 = second, etc.).")]
    public int materialIndex = 1;
    [Tooltip("The new Base Map (Albedo) texture to apply when grabbed.")]
    public Texture2D newBaseMapTexture;

    private bool grabbed = false;

    public void ActivatePhone()
    {
        phone.SetActive(true);
    }

    public void ActivateToolTip()
    {
        tooltip.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (grabbed) return;

        if (other.CompareTag("Hand")) // add this tag to your hand colliders
        {
            Debug.Log("Phone grabbed by: " + other.name);

            // Disable any animation influence
            if (phone.TryGetComponent(out Animator anim))
                anim.enabled = false;

            // Disable physics influence
            if (phone.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Hide the hand visual
            if (handVisual) handVisual.SetActive(false);

            // Force snap directly to hand in world space
            if (handPalm)
            {
                transform.SetParent(handPalm, true);
                transform.position = handPalm.position + handPalm.TransformVector(localPositionOffset);
                transform.rotation = handPalm.rotation * Quaternion.Euler(localRotationOffset);
            }

            // 🔄 Change texture on specific material slot
            if (phoneRenderer != null && newBaseMapTexture != null)
            {
                Material[] mats = phoneRenderer.materials;

                if (materialIndex >= 0 && materialIndex < mats.Length)
                {
                    // For URP/Standard shader use "_BaseMap"
                    // For Built-in Standard shader use "_MainTex"
                    mats[materialIndex].SetTexture("_BaseMap", newBaseMapTexture);
                    Debug.Log($"✅ Changed Base Map texture on material index {materialIndex}");
                }
                else
                {
                    Debug.LogWarning($"Material index {materialIndex} is out of range! Renderer has {mats.Length} materials.");
                }

                // Reassign materials array back to renderer
                phoneRenderer.materials = mats;
            }

            grabbed = true;
        }
    }
}