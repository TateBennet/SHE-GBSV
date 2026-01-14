using UnityEngine;
using System.Collections;

public class DuoCountChecker : MonoBehaviour
{
    public float functionDelay = 3f;
    public TriStreamPro TP;
    public bool alreadyInFail = false;

    public void CheckCounter()
    {
        // decide which video we want
        if (VballSFX.iterations >= 5)
        {
            TP.CommitBranch(5);
        }
        else if (VballSFX.counter == 0 || VballSFX.failed == true)
        {
            if(alreadyInFail == true)
            {
                TP.ReplayActive();
                alreadyInFail = false;
            }
            else TP.CommitBranch(2);
        }
        else if (VballSFX.counter == 1)
        {
            TP.CommitBranch(3);
        }
        else if (VballSFX.counter == 2)
        {
            TP.CommitBranch(4);
        }
        else if (VballSFX.counter == 3)
        {
            TP.CommitBranch(5);
        }
        else
        {
            Debug.Log("Counter is " + VballSFX.counter);
        }
    }

    public void FailVideoCheck()
    {
        alreadyInFail = true;
    }

}
