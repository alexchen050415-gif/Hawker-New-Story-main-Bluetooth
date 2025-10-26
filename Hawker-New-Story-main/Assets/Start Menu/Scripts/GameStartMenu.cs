using UnityEngine;
using UnityEngine.UI;

public class GameStartMenu : MonoBehaviour
{
    [Header("Scene To Load")]
    [Tooltip("The exact name of the scene you want to load.")]
    public string gameSceneName;

    [Header("Buttons")]
    public Button startButton;

    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
    }

    public void StartGame()
    {
        startButton.interactable = false;
        SceneTransitionManager.singleton.LoadScene(gameSceneName);
    }
}