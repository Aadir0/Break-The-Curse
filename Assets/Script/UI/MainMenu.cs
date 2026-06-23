using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void Options()
    {
        // Implement options menu logic here
    }

    public void Credits()
    {
        // Implement credits menu logic here
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void Resume()
    {
        Time.timeScale = 1f; // Resume the game
        // Implement logic to hide the pause menu UI
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        // Implement logic to show the pause menu UI
    }
}
