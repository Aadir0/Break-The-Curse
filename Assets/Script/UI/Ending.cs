using UnityEngine;
using UnityEngine.SceneManagement;

public class Ending : MonoBehaviour
{
    [SerializeField] private GameObject credits;
    [SerializeField] private GameObject[] mainMenuButton;
    [SerializeField] private GameObject pauseMenuUI;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            for (int i = 0; i < mainMenuButton.Length; i++)
            {
                mainMenuButton[i].SetActive(false);
            }
            credits.SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Time.timeScale = Time.timeScale == 0f ? 1f : 0f;
            pauseMenuUI.SetActive(!pauseMenuUI.activeSelf);
        }
    }

    public void mainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
