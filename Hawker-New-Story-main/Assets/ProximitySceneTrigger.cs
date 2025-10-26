using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProximitySceneTriggerMulti : MonoBehaviour
{
    public enum FireMode { AnyTarget, AllTargets }   // fire when any one qualifies, or after all do

    [Header("What to measure")]
    public Transform head;                            // XR camera; if null, uses Camera.main
    public List<Transform> targetObjects = new List<Transform>(); // (optional) drag scene instances here
    [Tooltip("Auto-find active objects with this tag (e.g., \"Food\").")]
    public string findTargetsByTag = "";

    [Header("Trigger conditions")]
    [Tooltip("Meters from the head at which the trigger fires.")]
    public float triggerDistance = 0.18f;
    [Tooltip("How long a target must stay within distance to count (anti-flicker).")]
    public float dwellSeconds = 0.25f;
    public FireMode fireMode = FireMode.AnyTarget;

    [Header("Next scene")]
    public string sceneName;                // must match Build Settings
    public float sceneDelaySeconds = 2f;    // wait before load

    [System.Serializable]
    public class Entry
    {
        [Tooltip("Any GameObject to show/hide (UI panel, cube, particles, etc).")]
        public GameObject boxOrText;
        [Tooltip("Optional voice clip to play when shown.")]
        public AudioClip voiceClip;
        [Tooltip("Seconds to stay visible; set < 0 to never auto-hide.")]
        public float visibleSeconds = 6f;
        public bool fadeOut = false;
        public float fadeDuration = 0.35f;
    }

    [Header("Show/Hide payload")]
    public List<Entry> entries = new List<Entry>();

    [Header("Target discovery")]
    [Tooltip("How often to rescan the scene for tag-matched targets (sec).")]
    public float rescanInterval = 0.5f;

    private AudioSource _audio;
    private bool _fired;
    private float _nextScan;
    // Per-target dwell timers
    private readonly Dictionary<Transform, float> _dwell = new Dictionary<Transform, float>();

    void Awake()
    {
        if (!head && Camera.main) head = Camera.main.transform;

        // Initial tag scan (in case targets are already active at start)
        ScanByTagOnce();

        // Ensure payload starts hidden (optional)
        foreach (var e in entries) if (e.boxOrText) e.boxOrText.SetActive(false);

        _audio = GetComponent<AudioSource>();
        if (!_audio) _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
    }

    void Update()
    {
        TryDiscoverTargets(); // keep registering tag-matched targets that appear later

        if (_fired || head == null || CountValidTargets() == 0) return;

        int qualified = 0;
        foreach (var t in targetObjects)
        {
            if (!t) continue;

            float d = Vector3.Distance(head.position, t.position);
            if (d <= triggerDistance)
            {
                // accumulate dwell time up to dwellSeconds
                _dwell[t] = Mathf.Min(dwellSeconds, _dwell[t] + Time.deltaTime);
            }
            else
            {
                _dwell[t] = 0f;
            }

            if (_dwell[t] >= dwellSeconds) qualified++;
        }

        bool shouldFire =
            (fireMode == FireMode.AnyTarget && qualified >= 1) ||
            (fireMode == FireMode.AllTargets && qualified == CountValidTargets());

        if (shouldFire)
        {
            _fired = true;
            StartCoroutine(FireSequence());
        }
    }

    // ===== Helpers =====
    void ScanByTagOnce()
    {
        if (string.IsNullOrEmpty(findTargetsByTag)) return;
        var found = GameObject.FindGameObjectsWithTag(findTargetsByTag);
        foreach (var go in found) RegisterTarget(go.transform);
    }

    void TryDiscoverTargets()
    {
        if (string.IsNullOrEmpty(findTargetsByTag)) return;
        if (Time.time < _nextScan) return;
        _nextScan = Time.time + rescanInterval;

        var found = GameObject.FindGameObjectsWithTag(findTargetsByTag);
        foreach (var go in found) RegisterTarget(go.transform);
    }

    void RegisterTarget(Transform t)
    {
        if (!t) return;

        // Prefer a child named "ProximityPoint" if present (useful for long objects)
        var pp = t.Find("ProximityPoint");
        if (pp) t = pp;

        if (!targetObjects.Contains(t))
        {
            targetObjects.Add(t);
            _dwell[t] = 0f;
            // Debug.Log($"[Proximity] Registered target: {t.name}");
        }
    }

    int CountValidTargets()
    {
        int n = 0;
        foreach (var t in targetObjects) if (t) n++;
        return n;
    }

    IEnumerator FireSequence()
    {
        // Show payload & start per-entry hide timers
        foreach (var e in entries)
        {
            if (!e.boxOrText) continue;
            e.boxOrText.SetActive(true);
            if (e.voiceClip) _audio.PlayOneShot(e.voiceClip);
            if (e.visibleSeconds >= 0f) StartCoroutine(HideAfter(e));
        }

        // Delay, then load next scene
        if (!string.IsNullOrEmpty(sceneName))
        {
            if (sceneDelaySeconds > 0f) yield return new WaitForSeconds(sceneDelaySeconds);
            SceneManager.LoadScene(sceneName);
        }
    }

    IEnumerator HideAfter(Entry e)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, e.visibleSeconds));

        if (e.fadeOut)
        {
            var cg = e.boxOrText.GetComponent<CanvasGroup>();
            if (!cg) cg = e.boxOrText.AddComponent<CanvasGroup>();
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

        if (e.boxOrText) e.boxOrText.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        if (head == null) return;
        Gizmos.color = Color.cyan;
        foreach (var t in targetObjects)
        {
            if (!t) continue;
            Gizmos.DrawWireSphere(t.position, triggerDistance);
        }
    }
}
