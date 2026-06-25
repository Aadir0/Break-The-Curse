using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [SerializeField] private GameObject winCanvas;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !SceneManager.GetActiveScene().name.Equals("Level 3"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        
        if (other.CompareTag("Player") && SceneManager.GetActiveScene().buildIndex == 3)
        {
            winCanvas.SetActive(true);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }
}
