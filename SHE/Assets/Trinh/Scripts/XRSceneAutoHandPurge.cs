using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class XRSceneAutoHandPurge : MonoBehaviour
{
    // how many frames to keep purging
    [SerializeField] private int purgeFrames = 60;

    private readonly string[] autoHandTypePrefixes =
    {
        "Autohand.AutoInputModule",
        "Autohand.HandCanvasPointer",
        "Autohand.HandBase",
        "Autohand.Hand",
        "Autohand.OpenXRAutoHandTracking",
        "Autohand.OpenXRAutoHandTrackingGrabber"
    };

    private void Awake()
    {
        // start purging immediately
        StartCoroutine(PurgeRoutine());
    }

    private IEnumerator PurgeRoutine()
    {
        for (int i = 0; i < purgeFrames; i++)
        {
            PurgeAutoHandStuffOnce();
            yield return null; // wait a frame, in case Auto Hand re-spawns something
        }
    }

    private void PurgeAutoHandStuffOnce()
    {
        // 1) kill Auto Hand behaviours that came across via DontDestroyOnLoad
        var allMBs = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in allMBs)
        {
            if (mb == null) continue;
            string fullName = mb.GetType().FullName;

            if (string.IsNullOrEmpty(fullName))
                continue;

            for (int p = 0; p < autoHandTypePrefixes.Length; p++)
            {
                if (fullName.StartsWith(autoHandTypePrefixes[p]))
                {
                    Debug.Log($"[XRSceneAutoHandPurge] Destroying leftover AutoHand object: {fullName} on {mb.gameObject.name}");
                    Destroy(mb.gameObject);
                    break;
                }
            }
        }

        // 2) make sure there is only ONE EventSystem, because Auto Hand tries to remove “extra” ones
        var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        bool keepFirst = true;
        foreach (var es in eventSystems)
        {
            if (keepFirst)
            {
                keepFirst = false;
            }
            else
            {
                Debug.Log("[XRSceneAutoHandPurge] Destroying extra EventSystem");
                Destroy(es.gameObject);
            }
        }

        // 3) make sure we only have one AudioListener (you had 2)
        var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        bool keepFirstListener = true;
        foreach (var al in listeners)
        {
            if (keepFirstListener)
            {
                keepFirstListener = false;
            }
            else
            {
                Debug.Log("[XRSceneAutoHandPurge] Destroying extra AudioListener");
                Destroy(al);
            }
        }

        // 4) optional: if LockerRoom camera came across, kill any camera that is not on your XR rig
        // if you want to be stricter, you can tag your XR camera and delete others
    }
}
