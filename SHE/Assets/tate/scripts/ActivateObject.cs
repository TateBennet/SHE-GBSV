using UnityEngine;

public class ActivateObject : MonoBehaviour
{

    public GameObject item;

    public void ActivatePhone()
    {
        item.SetActive(true);
    }
}