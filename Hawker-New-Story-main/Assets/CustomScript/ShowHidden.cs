using UnityEngine;

public class ShowMultipleObjects : MonoBehaviour
{
    [Tooltip("Assign all the hidden objects you want to unhide when this button is pressed.")]
    public GameObject[] hiddenObjects;

    public void ShowObjects()
    {
        foreach (GameObject obj in hiddenObjects)
        {
            if (obj != null)
                obj.SetActive(true); // Make each object visible
        }
    }
}
