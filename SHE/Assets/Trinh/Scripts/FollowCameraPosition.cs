using UnityEngine;

public class FollowCameraPosition : MonoBehaviour
{
    Transform _cam;

    void Start()
    {
        var main = Camera.main;
        _cam = main ? main.transform : null;
    }

    void LateUpdate()
    {
        if (!_cam) { var main = Camera.main; _cam = main ? main.transform : null; }
        if (_cam) transform.position = _cam.position;
        // Do NOT match rotation here for 360 spheres — head rotation should change the view.
    }
}
