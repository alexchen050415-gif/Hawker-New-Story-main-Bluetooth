using UnityEngine;

public class FloatingTextMotion : MonoBehaviour
{
    // Hover amplitude in meters.
    public float hoverAmplitude = 0.02f;
    // Hover speed in cycles per second.
    public float hoverSpeed = 1.0f;
    // Subtle scale breathing.
    public float scaleAmplitude = 0.02f;

    Vector3 _startPos;
    Vector3 _startScale;

    void Start()
    {
        _startPos = transform.localPosition;
        _startScale = transform.localScale;
    }

    void Update()
    {
        // Up-down hover
        float t = Time.time * hoverSpeed * Mathf.PI * 2f;
        transform.localPosition = _startPos + Vector3.up * (Mathf.Sin(t) * hoverAmplitude);

        // Tiny scale pulse
        float s = 1f + Mathf.Sin(t) * scaleAmplitude;
        transform.localScale = _startScale * s;
    }
}
