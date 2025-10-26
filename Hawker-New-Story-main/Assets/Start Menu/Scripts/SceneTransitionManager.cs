using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager singleton { get; private set; }

    private FadeScreen faderScreen;

    private void Awake()
    {
        if (singleton != null && singleton != this) { Destroy(this.gameObject); }
        else { singleton = this; DontDestroyOnLoad(this.gameObject); }
    }

    public void LoadScene(string sceneName)
    {
        faderScreen = FindObjectOfType<FadeScreen>();
        if (faderScreen != null)
        {
            StartCoroutine(LoadSceneRoutine(sceneName));
        }
        else
        {
            Debug.LogError("Could not find a FadeScreen object in the scene!");
        }
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // This assumes your fader script has a public function called "FadeOut()".
        faderScreen.FadeOut();

        // Wait for a fixed 3 seconds to match your FadeScreen's setting.
        yield return new WaitForSeconds(3f); // <-- THIS IS THE CORRECTED LINE

        // Load the new scene
        SceneManager.LoadScene(sceneName);
    }
}