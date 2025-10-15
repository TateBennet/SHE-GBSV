using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.ARFoundation;

public class VRSafetyInitializer : MonoBehaviour
{
    void Start()
    {
        // Kill any remaining AR components using new API
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            if (!obj.scene.IsValid()) continue;

            var arSession = obj.GetComponent<ARSession>();
            if (arSession != null)
            {
                DestroyImmediate(obj);
                Debug.LogWarning("Destroyed rogue ARSession");
                continue;
            }

            var components = obj.GetComponents<MonoBehaviour>();
            bool hasAR = components.Any(c => c.GetType().Name.StartsWith("AR") && c.enabled);
            if (hasAR)
            {
                foreach (var comp in components)
                {
                    if (comp.GetType().Name.StartsWith("AR"))
                    {
                        comp.enabled = false;
                    }
                }
            }
        }

        // Reset camera using new API
        var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var camera in cameras)
        {
            camera.transform.localRotation = Quaternion.identity;
            camera.clearFlags = CameraClearFlags.Skybox;
        }

        // Ensure video players work
        var videoPlayers = Object.FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None);
        foreach (var vp in videoPlayers)
        {
            vp.enabled = true;
        }

        Application.targetFrameRate = 72;
    }
}
