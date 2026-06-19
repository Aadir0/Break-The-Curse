using UnityEngine;
using UnityEngine.SceneManagement;

public class FallingBox : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private float delayBeforeLoad = 1f;

    private bool hasTriggered;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasTriggered) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            hasTriggered = true;

            Debug.Log("CRUSHED TO DEATH");
            Debug.Log("Loading Scene: " + sceneToLoad);

            Invoke(nameof(LoadScene), delayBeforeLoad);
        }
    }

        private void LoadScene()
    {
        Debug.Log("Scene Count: " + SceneManager.sceneCountInBuildSettings);

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            Debug.Log(SceneUtility.GetScenePathByBuildIndex(i));
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}