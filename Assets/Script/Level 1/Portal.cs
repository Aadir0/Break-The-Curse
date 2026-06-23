using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [SerializeField] private string fallbackSceneName = "MainMenu";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
                return;
            }

            if (!string.IsNullOrWhiteSpace(fallbackSceneName))
            {
                SceneManager.LoadScene(fallbackSceneName);
            }
        }
    }
}
