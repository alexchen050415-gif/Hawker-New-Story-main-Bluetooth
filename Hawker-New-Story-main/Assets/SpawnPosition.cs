using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils; // for XROrigin

public class XRSpawnOrienter : MonoBehaviour
{
    [Header("Assign these")]
    public XROrigin xrOrigin;        // Drag your XR Origin (Action-based)
    public Transform spawnPoint;     // An empty at floor height, with its forward = desired facing
    public bool disableCCWhileMove = true;

    CharacterController cc;

    void Awake()
    {
        if (!xrOrigin) xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin) cc = xrOrigin.GetComponent<CharacterController>();

        // Do it on scene load (in case you reuse this across scenes)
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Wait one frame so XR has a valid camera pose
        StartCoroutine(AlignNextFrame());
    }

    IEnumerator AlignNextFrame()
    {
        yield return null; // let camera update this frame
        RecenterToSpawn();
    }

    public void RecenterToSpawn()
    {
        if (!xrOrigin || !spawnPoint) return;

        // Temporarily disable CC to avoid being pushed by overlaps
        bool ccWasEnabled = false;
        if (disableCCWhileMove && cc)
        {
            ccWasEnabled = cc.enabled;
            cc.enabled = false;
        }

        // 1) Move head to spawn position
        xrOrigin.MoveCameraToWorldLocation(spawnPoint.position);

        // 2) Rotate yaw so camera faces spawnPoint.forward (projected on horizontal)
        Vector3 up = Vector3.up;
        Vector3 fwd = Vector3.ProjectOnPlane(spawnPoint.forward, up).normalized;
        if (fwd.sqrMagnitude > 0.0001f)
            xrOrigin.MatchOriginUpCameraForward(up, fwd);

        if (disableCCWhileMove && cc && ccWasEnabled) cc.enabled = true;
    }
}
