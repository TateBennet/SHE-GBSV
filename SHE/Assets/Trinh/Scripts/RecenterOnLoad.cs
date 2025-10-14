using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
public class RecenterOnLoad : MonoBehaviour
{
    void Start()
    {
        // Ask the XR runtime to recenter if supported.
        var inputs = new List<XRInputSubsystem>();
#if UNITY_2022_3_OR_NEWER || UNITY_6000_0_OR_NEWER
        SubsystemManager.GetSubsystems(inputs);
#else
        SubsystemManager.GetInstances(inputs);
#endif
        foreach (var s in inputs)
        {
            if (s != null) { s.TryRecenter(); }
        }
    }
}
