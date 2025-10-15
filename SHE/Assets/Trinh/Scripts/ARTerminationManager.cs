using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ARTerminationManager : MonoBehaviour
{
    [Header("Target VR Scene")]
    public string targetVRSceneName;

    private bool isTerminating = false;

    public void InitiateCleanTransition(string vrSceneName)
    {
        if (isTerminating) return;
        targetVRSceneName = vrSceneName;
        StartCoroutine(CleanARTermination());
    }

    private IEnumerator CleanARTermination()
    {
        isTerminating = true;
        Debug.Log("🔄 Initiating clean AR → VR transition...");

        // Phase 1: Find and disable all AR components by name/type
        yield return DisableAllARComponents();

        // Phase 2: Handle camera cleanup
        yield return CleanupARCamera();

        // Phase 3: Reset XR tracking
        yield return ResetTracking();

        // Phase 4: Restore rendering
        RestoreRendering();

        // Phase 5: Wait for cleanup
        yield return new WaitForSeconds(0.2f);

        // Phase 6: Load VR scene
        yield return LoadVRScene();

        FinalCleanup();
    }

    private IEnumerator DisableAllARComponents()
    {
        Debug.Log("🛑 Disabling AR components...");

        // Use new FindObjectsByType API with FindObjectsSortMode.None for performance
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (var obj in allObjects)
        {
            if (!obj.scene.IsValid()) continue; // Skip prefab assets

            // Check for AR components by type name
            var components = obj.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                string typeName = comp.GetType().Name;
                if (typeName.StartsWith("AR") &&
                    typeName != "ARCameraBackground" && // Handle separately
                    comp.enabled)
                {
                    comp.enabled = false;
                    Debug.Log($"Disabled {typeName} on {obj.name}");
                }
            }

            // Specific AR Session handling
            var arSession = obj.GetComponent<ARSession>();
            if (arSession != null)
            {
                arSession.enabled = false;
                Debug.Log("Disabled ARSession");
            }
        }

        yield return null; // One frame delay
    }

    private IEnumerator CleanupARCamera()
    {
        var cameras = Camera.allCameras;
        foreach (var camera in cameras)
        {
            // Remove AR Camera Background
            var arBackground = camera.GetComponent<ARCameraBackground>();
            if (arBackground != null)
            {
                arBackground.enabled = false;
                Debug.Log("Disabled ARCameraBackground");
            }

            // Restore VR camera settings
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = Color.black;
        }

        yield return null;
    }

    private IEnumerator ResetTracking()
    {
        yield return null; // Wait for AR to release

        // Simple camera reset - find main camera and reset rotation
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.localRotation = Quaternion.identity;
            Debug.Log("Reset main camera rotation");
        }

        // Try to reset any XROrigin cameras using new API
        var xrOrigins = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t.name.Contains("XROrigin") || t.name.Contains("XR Origin"));

        foreach (var origin in xrOrigins)
        {
            var cameraChild = origin.GetComponentInChildren<Camera>();
            if (cameraChild != null)
            {
                cameraChild.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void RestoreRendering()
    {
        Application.targetFrameRate = 72;
        QualitySettings.vSyncCount = 0;
        Time.captureFramerate = 0;
    }

    private IEnumerator LoadVRScene()
    {
        Debug.Log($"Loading VR scene: {targetVRSceneName}");

        // Direct scene load (simplest approach)
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetVRSceneName);
        loadOp.allowSceneActivation = true;

        while (!loadOp.isDone)
        {
            yield return null;
        }

        Debug.Log("✅ VR scene loaded");
    }

    private void FinalCleanup()
    {
        Destroy(gameObject);
    }

    // Nuclear option for testing
    [ContextMenu("Emergency AR Kill")]
    public void EmergencyARKill()
    {
        Debug.Log("💥 EMERGENCY AR KILL ACTIVATED");

        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            if (!obj.scene.IsValid()) continue;

            var arSession = obj.GetComponent<ARSession>();
            if (arSession != null)
            {
                DestroyImmediate(obj);
                continue;
            }

            var components = obj.GetComponents<MonoBehaviour>();
            bool hasAR = components.Any(c => c.GetType().Name.StartsWith("AR"));
            if (hasAR)
            {
                DestroyImmediate(obj);
                Debug.Log($"Destroyed AR object: {obj.name}");
            }
        }

        SceneManager.LoadScene(targetVRSceneName);
    }
}
