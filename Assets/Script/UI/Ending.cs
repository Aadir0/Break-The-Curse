using UnityEngine;
using UnityEngine.SceneManagement;

public class Ending : MonoBehaviour
{
    [SerializeField] private GameObject credits;
    [SerializeField] private GameObject[] mainMenuButton;

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
