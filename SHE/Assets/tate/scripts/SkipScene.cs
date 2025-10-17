using UnityEngine;

public class SkipScene : MonoBehaviour
{
    public ProVidMngr stream;
    //private int videoIndex = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("pointer"))
        {
            //streamVideos.StartCoroutine("PlayNext");
            //stream.PlayNext();
            stream.PlayNextVideo();
        }
    }
}
