using UnityEngine;

public class PhoneBtnMgr : MonoBehaviour
{
    [Header("Assign all phone button objects here (each has a collider)")]
    public GameObject[] phoneButtons;

    // 🔹 Disables all buttons
    public void DisableAll()
    {
        if (phoneButtons == null) return;

        foreach (GameObject button in phoneButtons)
        {
            if (button != null)
                button.SetActive(false);
        }
    }

    // 🔹 Activates only the button at a specific index
    public void ActivateButton(int index)
    {
        if (phoneButtons == null || index < 0 || index >= phoneButtons.Length)
        {
            Debug.LogWarning($"PhoneButtonManager: Invalid index {index}");
            return;
        }

        // Disable all first
        DisableAll();

        // Enable the one we want
        if (phoneButtons[index] != null)
            phoneButtons[index].SetActive(true);
    }
}
