using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    private const int FinalLevelBuildIndex = 3;

    [SerializeField] private GameObject winCanvas;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        int activeBuildIndex = SceneManager.GetActiveScene().buildIndex;

        if (activeBuildIndex == FinalLevelBuildIndex)
        {
            if (winCanvas != null)
            {
                winCanvas.SetActive(true);
            }

            SceneManager.LoadScene(activeBuildIndex + 1);
            return;
        }

        if (activeBuildIndex < FinalLevelBuildIndex)
        {
            SceneManager.LoadScene(activeBuildIndex + 1);
        }
    }
}
