using UnityEngine;
using System.Collections;

public class DuoCountChecker : MonoBehaviour
{
    public float functionDelay = 3f;
    public DuoStreamPro DSP;

    public void CheckCounter()
    {
        if ( VballSFX.iterations >= 5)
        {
            StartCoroutine(DelayedFunction(OnThirdHit));
        }
        else if (VballSFX.counter == 0 || VballSFX.failed == true)
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
        DSP.PlayVideoByIndex(2,3);
        
    }

    void OnFirstHit()
    {
        Debug.Log("Successful hit 1/3 - moving on...");
        DSP.PlayVideoByIndex(3,4);
        
    }

    void OnSecondHit()
    {
        Debug.Log("successful hit 2/3 - moving on...");
        DSP.PlayVideoByIndex(4,5);
        
    }

    void OnThirdHit()
    {
        Debug.Log("successful hit 3/3 - task complete!");
        DSP.PlayVideoByIndex(5,6);
    }

}

