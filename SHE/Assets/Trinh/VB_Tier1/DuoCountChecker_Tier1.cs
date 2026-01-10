using UnityEngine;
using System.Collections;

public class DuoCountChecker_Tier1 : MonoBehaviour
{
    [Header("References")]
    public DuoStreamPro DSP;

    [Header("Tuning")]
    [Tooltip("Max time to wait for the standby player to be prepared + have a valid texture before swapping.")]
    public float maxWaitForStandbyReady = 6.0f;

    [Tooltip("If standby isn't ready in time, fall back to preparing the target clip on the active player (prevents freeze).")]
    public bool fallbackToPrepareIfNotReady = true;

    public void CheckCounter()
    {
        if (!DSP)
        {
            Debug.LogWarning("DuoCountChecker_Tier1: Missing DSP reference.");
            return;
        }

        // Decide which video we want next
        if (VballSFX.iterations >= 5)
        {
            RequestBranch(5, OnThirdHit);
        }
        else if (VballSFX.counter == 0 || VballSFX.failed == true)
        {
            RequestBranch(2, OnMissedBall);
        }
        else if (VballSFX.counter == 1)
        {
            RequestBranch(3, OnFirstHit);
        }
        else if (VballSFX.counter == 2)
        {
            RequestBranch(4, OnSecondHit);
        }
        else if (VballSFX.counter == 3)
        {
            RequestBranch(5, OnThirdHit);
        }
        else
        {
            Debug.Log("Counter is " + VballSFX.counter);
        }
    }

    private void RequestBranch(int targetIndex, System.Action onReadySwap)
    {
        // Start preload for the branch target
        DSP.PreloadVideo(targetIndex);

        // Wait until standby is actually ready, then switch
        StartCoroutine(SwitchWhenStandbyReady(targetIndex, onReadySwap));
    }

    private IEnumerator SwitchWhenStandbyReady(int targetIndex, System.Action onReadySwap)
    {
        float timer = 0f;

        // Capture the current standby player reference
        var standby = DSP.GetStandbyPlayer();

        // Wait until standby is ready OR timeout
        while (timer < maxWaitForStandbyReady)
        {
            if (standby != null && standby.isPrepared && standby.texture != null)
                break;

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        bool standbyReady = (standby != null && standby.isPrepared && standby.texture != null);

        if (standbyReady)
        {
            // Instant swap path (standby is ready)
            onReadySwap?.Invoke();
            yield break;
        }

        Debug.LogWarning(
            $"DuoCountChecker_Tier1: Standby NOT ready after {maxWaitForStandbyReady:0.00}s " +
            $"(standbyNull={standby == null}, prepared={(standby != null && standby.isPrepared)}, tex={(standby != null && standby.texture != null)})."
        );

        if (!fallbackToPrepareIfNotReady)
        {
            // Still attempt swap (may freeze, but preserves the "swap only" behavior)
            onReadySwap?.Invoke();
            yield break;
        }

        // Fallback: force-load the requested clip (prevents freeze; may add a short delay)
        // This calls PrepareActivateAndPause internally and will still trigger your audio manager events.
        DSP.PlayVideoByIndex(targetIndex);
    }

    private void OnMissedBall()
    {
        Debug.Log("Failed attempt, try again...");
        DSP.PlayNextVideo(2);
    }

    private void OnFirstHit()
    {
        Debug.Log("Successful hit 1/3 - moving on...");
        DSP.PlayNextVideo(3);
    }

    private void OnSecondHit()
    {
        Debug.Log("successful hit 2/3 - moving on...");
        DSP.PlayNextVideo(4);
    }

    private void OnThirdHit()
    {
        Debug.Log("successful hit 3/3 - task complete!");
        DSP.PlayNextVideo(5);
    }
}
