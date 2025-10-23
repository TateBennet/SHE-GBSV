using UnityEngine;

public class ActivateObject : MonoBehaviour
{

    public GameObject itemToActivate;
    public GameObject itemToDeactivate;

    public void ActivateItem()
    {
        itemToActivate.SetActive(true);
    }

    public void DisableItem()
    {
        itemToDeactivate.SetActive(false);
    }
}