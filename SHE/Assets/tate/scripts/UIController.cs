using UnityEngine;

public class UIController : MonoBehaviour
{
    [Header("References")]
    public StreamVideos streamVideos;   // Drag your StreamVideos component here
    public GameObject uiRoot;           // World-space UI panel (in scene)
    public Transform uiAnchor;          // Usually the VR camera / player head

    [Header("Placement Options")]
    public bool autoPlaceUI = true;
    public float uiDistance = 1.2f;
    public Vector3 uiOffset = new Vector3(0, -0.05f, 0);
    public bool faceAnchor = true;      // rotate to face the player

    private void Start()
    {
        if (uiRoot) uiRoot.SetActive(false); // start hidden
    }

    public void ShowUI()
    {
        Debug.Log("showing ui");
        if (!uiRoot) return;

        if (autoPlaceUI && uiAnchor)
        {
            // Place in front of player
            Vector3 fwd = uiAnchor.forward;
            Vector3 pos = uiAnchor.position + fwd * uiDistance + uiOffset;
            uiRoot.transform.position = pos;

            if (faceAnchor)
            {
                // Make UI face the player
                Vector3 lookDir = (uiAnchor.position - uiRoot.transform.position);
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 1e-6f)
                {
                    uiRoot.transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                    uiRoot.transform.rotation *= Quaternion.Euler(0f, 180f, 0f); // flip to face
                }
            }
        }

        uiRoot.SetActive(true);
    }

    public void HideUI()
    {
        if (uiRoot) uiRoot.SetActive(false);
    }

    // --- Button Hooks ---
    public void OnNextPressed()
    {
        if (streamVideos) streamVideos.PlayNext();
        HideUI();
    }

    public void OnPrevPressed()
    {
        if (streamVideos) streamVideos.PlayPrevious();
        HideUI();
    }

    public void OnPlayPausePressed()
    {
        if (streamVideos) streamVideos.TogglePlayPause();
    }
}
