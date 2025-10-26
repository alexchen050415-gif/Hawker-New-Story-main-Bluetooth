using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressToShowTexts : MonoBehaviour
{
    [System.Serializable]
    public class TextEntry
    {
        [Tooltip("The UI panel or GameObject to show/hide (parent panel recommended).")]
        public GameObject textBox;

        [Tooltip("Voice clip to play when this text appears (optional).")]
        public AudioClip voiceClip;

        [Tooltip("How long this text stays visible after showing (seconds).")]
        public float visibleSeconds = 6f;

        [Header("Optional Fade")]
        public bool fadeOut = false;
        public float fadeDuration = 0.35f;
    }

    [Header("All the texts you want to control")]
    public List<TextEntry> entries = new List<TextEntry>();

    [Header("Audio")]
    [Tooltip("If left empty, an AudioSource will be added automatically.")]
    public AudioSource audioSource;

    void Awake()
    {
        if (!audioSource)
        {
            audioSource = GetComponent<AudioSource>();
            if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Call this from your XR button (OnPress/OnClick) to show all texts now.
    /// </summary>
    public void ShowAllNow()
    {
        StopAllCoroutines();
        foreach (var e in entries)
        {
            if (e.textBox == null) continue;

            // Ensure visible
            e.textBox.SetActive(true);

            // Play voice (non-blocking)
            if (e.voiceClip) audioSource.PlayOneShot(e.voiceClip);

            // Start a timer to hide this entry
            StartCoroutine(HideEntryAfterDelay(e));
        }
    }

    IEnumerator HideEntryAfterDelay(TextEntry e)
    {
        if (e.visibleSeconds > 0f)
            yield return new WaitForSeconds(e.visibleSeconds);

        if (e.fadeOut)
        {
            // Ensure CanvasGroup exists
            var cg = e.textBox.GetComponent<CanvasGroup>();
            if (!cg) cg = e.textBox.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            float t = 0f;
            while (t < e.fadeDuration)
            {
                t += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, e.fadeDuration));
                cg.alpha = a;
                yield return null;
            }
            cg.alpha = 1f; // reset alpha for next time (we disable object below)
        }

        if (e.textBox) e.textBox.SetActive(false);
    }

    /// <summary>
    /// Optional: call this if you want to hide everything immediately (e.g., a Skip button).
    /// </summary>
    public void HideAllNow()
    {
        StopAllCoroutines();
        foreach (var e in entries)
            if (e.textBox) e.textBox.SetActive(false);
    }
}
