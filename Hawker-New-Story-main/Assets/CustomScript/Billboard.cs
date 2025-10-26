using UnityEngine;

public class Billboard : MonoBehaviour
{
    // Drag your VR camera here in the Inspector, or it will auto-find Camera.main.
    public Transform target;

    void Start()
    {
        // If user didn't assign a camera, use the Main Camera (XR Origin usually tags it as MainCamera).
        if (target == null && Camera.main != null)
            target = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Point the forward of this object towards the camera so it faces the player.
        // Keep upright to avoid tilting with head roll.
        Vector3 lookPos = transform.position + (transform.position - target.position);
        lookPos.y = transform.position.y; // lock vertical tilt; remove this line if you want full facing
        transform.LookAt(lookPos);

        // Optionally: keep text from mirroring when looked from behind
        // transform.forward = - (target.position - transform.position).normalized;
    }
}
