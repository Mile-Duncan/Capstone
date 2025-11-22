using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LooseScreen : MonoBehaviour
{
    public Button RestartButton;
    public TextMeshProUGUI ScoreText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RestartButton.onClick.AddListener(ReloadCurrentScene);
    }

    public static void ReloadCurrentScene()
    {
        // Get the name of the active scene
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // Load the scene by its name
        SceneManager.UnloadSceneAsync(currentSceneName);
        SceneManager.LoadScene(currentSceneName);
    }
}
