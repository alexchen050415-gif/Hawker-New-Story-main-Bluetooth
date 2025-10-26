using UnityEngine;

public class FoodPicker : MonoBehaviour
{
    // ... (keep all your existing code at the top) ...
    [Tooltip("The small food particle prefab to spawn.")]
    public GameObject foodParticlePrefab;

    [Tooltip("The point on the utensil where the particle will attach.")]
    public Transform particleAttachPoint;

    private GameObject currentParticle = null;

    private void OnTriggerEnter(Collider other)
    {
        if (currentParticle != null)
        {
            return;
        }

        if (other.CompareTag("Food"))
        {
            currentParticle = Instantiate(foodParticlePrefab, particleAttachPoint.position, particleAttachPoint.rotation);
            currentParticle.transform.SetParent(particleAttachPoint);
        }
    }

    // --- ADD THIS NEW FUNCTION ---
    public void DestroyCurrentParticle()
    {
        if (currentParticle != null)
        {
            Destroy(currentParticle);
            currentParticle = null;
        }
    }
}