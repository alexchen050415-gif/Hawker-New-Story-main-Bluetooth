using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderWithDelay : MonoBehaviour
{
    [Tooltip("Name of the scene to load.")]
    public string sceneName;

    [Tooltip("Seconds to wait before loading the next scene.")]
    public float delaySeconds = 2f;

    // Call this from your button OnClick / XR button OnPress
    public void LoadNextSceneWithDelay()
    {
        StartCoroutine(LoadAfterDelay());
    }

    private System.Collections.IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSeconds(delaySeconds);
        SceneManager.LoadScene(sceneName);
    }
}
