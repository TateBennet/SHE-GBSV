using UnityEngine;
using System.Collections;

public class DuoCountChecker : MonoBehaviour
{
    public float functionDelay = 3f;
    public DuoStreamPro DSP;

    public void CheckCounter()
    {
        // decide which video we want
        if (VballSFX.iterations >= 5)
        {
            // preload the one we are going to play
            DSP.PreloadVideo(5);   // we call PlayVideoByIndex(5,6) later
            StartCoroutine(DelayedFunction(OnThirdHit));
        }
        else if (VballSFX.counter == 0 || VballSFX.failed == true)
        {
            DSP.PreloadVideo(2);   // we call PlayVideoByIndex(2,3)
            StartCoroutine(DelayedFunction(OnMissedBall));
        }
        else if (VballSFX.counter == 1)
        {
            DSP.PreloadVideo(3);   // we call PlayVideoByIndex(3,4)
            StartCoroutine(DelayedFunction(OnFirstHit));
        }
        else if (VballSFX.counter == 2)
        {
            DSP.PreloadVideo(4);   // we call PlayVideoByIndex(4,5)
            StartCoroutine(DelayedFunction(OnSecondHit));
        }
        else if (VballSFX.counter == 3)
        {
            DSP.PreloadVideo(5);   // we call PlayVideoByIndex(5,6)
            StartCoroutine(DelayedFunction(OnThirdHit));
        }
        else
        {
            Debug.Log("Counter is " + VballSFX.counter);
        }
    }

    IEnumerator DelayedFunction(System.Action callback)
    {
        float timer = 0f;
        float maxWait = 6f;
        float minWait = 3f;  // ✅ enforce at least 2 seconds
        float readyTime = 0f;

        var standby = DSP.GetStandbyPlayer();

        // Wait until standby is ready or timeout
        while ((!standby.isPrepared || standby.texture == null) && timer < maxWait)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        readyTime = timer; // how long it took to prepare

        // If it finished quickly, wait out the rest of the minimum delay
        if (readyTime < minWait)
        {
            yield return new WaitForSeconds(minWait - readyTime);
        }

        if (!standby.isPrepared)
            Debug.LogWarning("Standby video not ready after timeout.");

        callback?.Invoke();
    }



    void OnMissedBall()
    {
        Debug.Log("Failed attempt, try again...");
        DSP.PlayVideoByIndex(2, 3);
    }

    void OnFirstHit()
    {
        Debug.Log("Successful hit 1/3 - moving on...");
        DSP.PlayVideoByIndex(3, 4);
    }

    void OnSecondHit()
    {
        Debug.Log("successful hit 2/3 - moving on...");
        DSP.PlayVideoByIndex(4, 5);
    }

    void OnThirdHit()
    {
        Debug.Log("successful hit 3/3 - task complete!");
        DSP.PlayVideoByIndex(5, 6);
    }
}
