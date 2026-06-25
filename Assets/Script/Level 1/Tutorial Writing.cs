using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TutorialWriting : MonoBehaviour
{
    [SerializeField] private Text sentenceText;
    [TextArea(2, 4)]
    [SerializeField] private string[] sentences;
    [SerializeField] private bool pauseGameWhileShowing = true;

    private int currentSentenceIndex;

    private void Start()
    {
        currentSentenceIndex = 0;
        ShowCurrentSentence();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        currentSentenceIndex++;

        if (currentSentenceIndex >= sentences.Length)
        {
            FinishTutorial();
            return;
        }

        ShowCurrentSentence();
    }

    private void ShowCurrentSentence()
    {
        if (sentenceText == null)
        {
            return;
        }

        if (sentences == null || sentences.Length == 0)
        {
            sentenceText.text = string.Empty;
            FinishTutorial();
            return;
        }

        currentSentenceIndex = Mathf.Clamp(currentSentenceIndex, 0, sentences.Length - 1);
        sentenceText.text = sentences[currentSentenceIndex];
    }

    private void FinishTutorial()
    {
        if (pauseGameWhileShowing)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }
}
