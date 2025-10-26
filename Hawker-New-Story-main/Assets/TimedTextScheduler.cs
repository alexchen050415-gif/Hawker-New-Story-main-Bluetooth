using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedTextScheduler : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("The panel GameObject to show/hide (use the parent panel, not just the TMP text).")]
        public GameObject textBox;

        [Tooltip("Play this when the text appears (optional).")]
        public AudioClip voiceClip;

        [Tooltip("Seconds after start to SHOW this text.")]
        public float showAt = 0f;

        [Tooltip("Seconds after start to HIDE this text (must be >= showAt).")]
        public float hideAt = 5f;

        [Header("Fade (optional)")]
        public bool fadeOut = false;
        public float fadeDuration = 0.35f;
    }

    [Header("Entries (each with its own times)")]
    public List<Entry> entries = new List<Entry>();

    [Header("Playback")]
    public bool playOnStart = true;         // start automatically on scene load
    public float globalOffsetSeconds = 0f;  // shifts entire schedule (e.g., start 2s later)

    [Header("Audio")]
    public AudioSource audioSource;         // optional; will be auto-added if null

    Coroutine _timelineCo;

    void Awake()
    {
        if (!audioSource)
        {
            audioSource = GetComponent<AudioSource>();
            if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        // (Optional) ensure all texts start hidden in-editor
        foreach (var e in entries)
            if (e.textBox) e.textBox.SetActive(false);
    }

    void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        Stop(); // in case we replay
        _timelineCo = StartCoroutine(RunTimeline());
    }

    public void Stop()
    {
        if (_timelineCo != null) StopCoroutine(_timelineCo);
        _timelineCo = null;
    }

    public void SkipAllAndHide()
    {
        Stop();
        foreach (var e in entries)
            if (e.textBox) e.textBox.SetActive(false);
    }

    IEnumerator RunTimeline()
    {
        // Launch one coroutine per entry so times are independent
        foreach (var e in entries)
            StartCoroutine(HandleEntry(e));
        yield break;
    }

    IEnumerator HandleEntry(Entry e)
    {
        if (e.textBox == null) yield break;

        float showTime = Mathf.Max(0f, e.showAt + globalOffsetSeconds);
        float hideTime = Mathf.Max(showTime, e.hideAt + globalOffsetSeconds); // clamp to >= show

        // Wait until show time
        if (showTime > 0f) yield return new WaitForSeconds(showTime);

        // Show
        e.textBox.SetActive(true);
        if (e.voiceClip) audioSource.PlayOneShot(e.voiceClip);

        // Wait until hide time
        float visibleFor = hideTime - showTime;
        if (visibleFor > 0f) yield return new WaitForSeconds(visibleFor);

        // Hide (with optional fade)
        if (e.fadeOut)
        {
            var cg = e.textBox.GetComponent<CanvasGroup>();
            if (!cg) cg = e.textBox.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            float t = 0f, dur = Mathf.Max(0.01f, e.fadeDuration);
            while (t < dur)
            {
                t += Time.deltaTime;
                cg.alpha = 1f - Mathf.Clamp01(t / dur);
                yield return null;
            }
            cg.alpha = 1f; // reset for next time
        }

        e.textBox.SetActive(false);
    }
}
