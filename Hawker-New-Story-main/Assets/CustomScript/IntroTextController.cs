using UnityEngine;

public class IntroTextMultiController : MonoBehaviour
{
    [Header("Targets")]
    public GameObject[] textBoxes;   // assign all the text panels you want to appear

    [Header("Audio (optional)")]
    public AudioClip voiceClip;      // one clip to play at spawn (e.g., 3s)
    AudioSource _audio;

    [Header("Visibility Control")]
    public bool manualHide = true;   // if true, you must call HideAll()
    public float autoHideAfter = 0f; // if > 0 and manualHide=false, will auto-hide after this many seconds

    void Start()
    {
        // Show all boxes on scene start
        SetAll(true);

        // Play voice (optional)
        if (voiceClip != null)
        {
            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.clip = voiceClip;
            _audio.Play();
        }

        // Optional auto-hide timer
        if (!manualHide && autoHideAfter > 0f)
            Invoke(nameof(HideAll), autoHideAfter);
    }

    public void HideAll()
    {
        CancelInvoke(nameof(HideAll));
        if (_audio != null && _audio.isPlaying) _audio.Stop();
        SetAll(false);
    }

    public void ShowAll()
    {
        SetAll(true);
    }

    void SetAll(bool visible)
    {
        if (textBoxes == null) return;
        foreach (var go in textBoxes)
            if (go) go.SetActive(visible);
    }
}
