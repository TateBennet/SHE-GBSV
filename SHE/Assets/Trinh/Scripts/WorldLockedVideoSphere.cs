using UnityEngine;

public class WorldLockedVideoSphere : MonoBehaviour
{
    [Header("Video Setup")]
    public Renderer sphereRenderer;

    void Start()
    {
        // CRITICAL: World-lock to anchor, NEVER follow camera
        SetupWorldLock();
        Setup360Material();
    }

    private void SetupWorldLock()
    {
        // Find or create world anchor
        Transform worldAnchor = GameObject.Find("WorldAnchor")?.transform;
        if (worldAnchor == null)
        {
            GameObject anchorObj = new GameObject("WorldAnchor");
            worldAnchor = anchorObj.transform;
            worldAnchor.position = Vector3.zero;
            worldAnchor.rotation = Quaternion.identity;
            Debug.Log("Created WorldAnchor at origin");
        }

        // Parent to world anchor (stays fixed in world space)
        transform.SetParent(worldAnchor);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(0, 180, 0); // Face inward
        transform.localScale = Vector3.one * 50f; // Large 360 sphere

        // Ensure sphere normals face inward
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null && mr.material != null)
        {
            mr.material.renderQueue = 2000; // Ensure proper rendering
        }

        Debug.Log("Video sphere world-locked to anchor");
    }

    private void Setup360Material()
    {
        if (sphereRenderer == null) sphereRenderer = GetComponent<Renderer>();

        var mat = sphereRenderer.material;

        // Fix equirectangular projection (no red line)
        mat.mainTextureOffset = new Vector2(0.5f, 0f);
        mat.mainTextureScale = new Vector2(2f, 1f);

        // Inside-out rendering
        if (mat.HasProperty("_Inside")) mat.SetFloat("_Inside", 1f);

        Debug.Log("360 material setup complete");
    }

    // NO Update() - sphere stays world-locked, camera moves freely
}
