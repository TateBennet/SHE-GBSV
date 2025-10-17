using UnityEngine;
using System.Collections;

public class VBallCountChecker : MonoBehaviour
{
    public float functionDelay = 3f;
    public ProVidMngr ProVidMngr;

    public void CheckCounter()
    {
        if (VballSFX.counter == 0)
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
    }

    void OnFirstHit()
    {
        Debug.Log("Successful hit 1/3 - moving on...");
        ProVidMngr.PlayVideoByIndex(3);
    }

    void OnSecondHit()
    {
        Debug.Log("successful hit 2/3 - moving on...");
        ProVidMngr.PlayVideoByIndex(4);
    }

    void OnThirdHit()
    {
        Debug.Log("successful hit 3/3 - task complete!");
        ProVidMngr.PlayVideoByIndex(5);
    }

}
