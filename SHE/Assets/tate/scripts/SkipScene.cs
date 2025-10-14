using UnityEngine;

public class SkipScene : MonoBehaviour
{
    public StreamVideos stream;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("pointer"))
        {
            //streamVideos.StartCoroutine("PlayNext");
            stream.PlayNext();
        }
    }
}
