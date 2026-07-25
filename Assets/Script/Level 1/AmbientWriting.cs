using UnityEngine;
using TMPro;
using System.Collections;

public class AmbientWriting : MonoBehaviour
{
    [SerializeField] private GameObject textCanvas;
    [SerializeField] private TMP_Text sentenceText;
    [TextArea(2, 4)]
    [SerializeField] private string[] sentences;
    [SerializeField] private float characterDelay = 0.06f;
    [SerializeField] private float sentenceInterval = 1.5f;
    [SerializeField] private bool hideAfterLastSentence = true;
    [SerializeField] private bool spaceSkipsTypingAndInterval = true;

    private int currentSentenceIndex;
    private Coroutine displayRoutine;
    private bool isTyping;
    private bool isWaitingBetweenSentences;

    private void Start()
    {
        if (textCanvas == null)
        {
            textCanvas = gameObject;
        }
    }

    private void Update()
    {
        if (!spaceSkipsTypingAndInterval || !Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        if (isTyping)
        {
            CompleteCurrentSentence();
            return;
        }

        if (isWaitingBetweenSentences)
        {
            ShowNextSentence();
        }
    }

    public void Display()
    {
        currentSentenceIndex = 0;
        ShowCanvas();
        ShowCurrentSentence();
    }

    public void Display(string[] newSentences)
    {
        sentences = newSentences;
        Display();
    }

    public void Hide()
    {
        StopDisplayRoutine();
        isTyping = false;
        isWaitingBetweenSentences = false;

        if (sentenceText != null)
        {
            sentenceText.text = string.Empty;
        }

        if (textCanvas != null)
        {
            textCanvas.SetActive(false);
        }
    }

    private void ShowCanvas()
    {
        if (textCanvas != null)
        {
            textCanvas.SetActive(true);
        }
    }

    private void ShowCurrentSentence()
    {
        if (sentenceText == null)
        {
            return;
        }

        if (sentences == null || sentences.Length == 0)
        {
            Hide();
            return;
        }

        currentSentenceIndex = Mathf.Clamp(currentSentenceIndex, 0, sentences.Length - 1);
        StopDisplayRoutine();
        displayRoutine = StartCoroutine(TypeSentence(sentences[currentSentenceIndex]));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        isWaitingBetweenSentences = false;
        sentenceText.text = string.Empty;

        if (!string.IsNullOrWhiteSpace(sentence))
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            for (int i = 0; i < sentence.Length; i++)
            {
                builder.Append(sentence[i]);
                sentenceText.text = builder.ToString();

                if (i < sentence.Length - 1 && characterDelay > 0f)
                {
                    yield return new WaitForSecondsRealtime(characterDelay);
                }
            }
        }

        isTyping = false;
        displayRoutine = StartCoroutine(WaitBeforeNextSentence());
    }

    private IEnumerator WaitBeforeNextSentence()
    {
        isWaitingBetweenSentences = true;

        if (sentenceInterval > 0f)
        {
            yield return new WaitForSecondsRealtime(sentenceInterval);
        }

        displayRoutine = null;
        ShowNextSentence();
    }

    private void CompleteCurrentSentence()
    {
        StopDisplayRoutine();

        sentenceText.text = sentences != null && sentences.Length > 0
            ? sentences[currentSentenceIndex]
            : string.Empty;

        isTyping = false;
        displayRoutine = StartCoroutine(WaitBeforeNextSentence());
    }

    private void ShowNextSentence()
    {
        StopDisplayRoutine();
        isWaitingBetweenSentences = false;

        if (sentences == null || sentences.Length == 0)
        {
            Hide();
            return;
        }

        if (currentSentenceIndex < sentences.Length - 1)
        {
            currentSentenceIndex++;
            ShowCurrentSentence();
            return;
        }

        if (hideAfterLastSentence)
        {
            Hide();
        }
    }

    private void StopDisplayRoutine()
    {
        if (displayRoutine == null)
        {
            return;
        }

        StopCoroutine(displayRoutine);
        displayRoutine = null;
    }

    private void OnDisable()
    {
        StopDisplayRoutine();
        isTyping = false;
        isWaitingBetweenSentences = false;
    }
}
