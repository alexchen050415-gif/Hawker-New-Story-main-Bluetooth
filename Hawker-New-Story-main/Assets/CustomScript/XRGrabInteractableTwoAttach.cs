using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HandSpecificAttach : MonoBehaviour
{
    // Drag your Left Hand Controller here in the Inspector
    public XRBaseInteractor leftHandInteractor;

    // Drag your Right Hand Controller here
    public XRBaseInteractor rightHandInteractor;

    // Drag your left attach point here
    public Transform leftAttachTransform;

    // Drag your right attach point here
    public Transform rightAttachTransform;

    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        // Get the XR Grab Interactable component on this object
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    // This function will now be called when a hand HOVERS over the object
    public void SetAttachPoint(HoverEnterEventArgs args)
    {
        // Check if the interactor that is hovering is the left hand
        if (args.interactorObject == leftHandInteractor)
        {
            grabInteractable.attachTransform = leftAttachTransform;
        }
        // Check if the interactor is the right hand
        else if (args.interactorObject == rightHandInteractor)
        {
            grabInteractable.attachTransform = rightAttachTransform;
        }
    }
}