using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class VisibilityManager : MonoBehaviour
{
    // ... (keep your existing variables) ...
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private XRGrabInteractable grabInteractable;

    // --- ADD THIS NEW VARIABLE ---
    private FoodPicker foodPicker;

    void Awake()
    {
        // ... (keep your existing Awake code) ...
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(HandleSelection);

        // --- ADD THIS NEW LINE ---
        // Find the FoodPicker script in this object's children.
        foodPicker = GetComponentInChildren<FoodPicker>();
    }

    // ... (keep the OnDestroy function) ...
    private void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(HandleSelection);
    }

    private void HandleSelection(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor || args.interactorObject is XRSocketTagInteractor)
        {
            // --- ADD THIS NEW LOGIC ---
            // If we are being placed in a socket, destroy any particle we are holding.
            if (foodPicker != null)
            {
                foodPicker.DestroyCurrentParticle();
            }

            // ... (keep your existing logic for hiding the utensil) ...
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
            if (meshCollider != null)
            {
                meshCollider.isTrigger = true;
            }
        }
        else // It's a hand
        {
            // ... (keep all your existing logic for the hand) ...
            if (meshRenderer != null)
            {
                meshRenderer.enabled = true;
            }
            if (meshCollider != null)
            {
                meshCollider.isTrigger = false;
            }
        }
    }
}