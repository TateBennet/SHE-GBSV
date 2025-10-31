using UnityEngine;
using System.Collections;

public class VBallCountChecker : MonoBehaviour
{
    public float functionDelay = 3f;
    public ProVidMngr ProVidMngr;
    private int failVid1 = 0;
    private int failVid2 = 0;
    private int failVid3 = 0;

    public void CheckCounter()
    {
        if(failVid1 > 3 || failVid2 > 3 || failVid3 > 3)
        {
            StartCoroutine(DelayedFunction(OnThirdHit));
        }
        else if (VballSFX.counter == 0)
        {
            StartCoroutine(DelayedFunction(OnMissedBall));
        }
        else if (VballSFX.counter == 1)
        {
            StartCoroutine(DelayedFunction(OnFirstHit));
        }
        else if (VballSFX.counter == 2)
        {
            StartCoroutine(DelayedFunction(OnSecondHit));
        }
        else if (VballSFX.counter == 3)
        {
            StartCoroutine(DelayedFunction(OnThirdHit));
        }
        else
        {
            Debug.Log("Counter is " + VballSFX.counter);
        }
    }

    IEnumerator DelayedFunction(System.Action callback)
    {
        yield return new WaitForSeconds(functionDelay);
        callback?.Invoke();
    }

    void OnMissedBall()
    {
        Debug.Log("Failed attempt, try again...");
        ProVidMngr.PlayVideoByIndex(2);
        failVid1++;
    }

    void OnFirstHit()
    {
        Debug.Log("Successful hit 1/3 - moving on...");
        ProVidMngr.PlayVideoByIndex(3);
        failVid2++;
    }

    void OnSecondHit()
    {
        Debug.Log("successful hit 2/3 - moving on...");
        ProVidMngr.PlayVideoByIndex(4);
        failVid3++;
    }

    void OnThirdHit()
    {
        Debug.Log("successful hit 3/3 - task complete!");
        ProVidMngr.PlayVideoByIndex(5);
    }

}
