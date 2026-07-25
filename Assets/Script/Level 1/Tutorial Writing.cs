using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TutorialWriting : MonoBehaviour
{
    [SerializeField] private GameObject typewriterCanvas;
    [SerializeField] private TMP_Text sentenceText;
    [SerializeField] private TMP_Text continueText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typingClip;
    [Range(0f, 1f)]
    [SerializeField] private float typingVolume = 0.7f;
    [TextArea(2, 4)]
    [SerializeField] private string[] sentences;
    [SerializeField] private float wordDelay = 0.12f;
    [SerializeField] private float sentencePause = 2f;
    [SerializeField] private string continuePrompt = "Press Space to Continue";

    private int currentSentenceIndex;
    private Coroutine typingRoutine;
    private Coroutine pauseRoutine;
    private bool isTyping;
    private bool isPausing;
    private float previousTimeScale = 1f;
    private readonly List<MonoBehaviour> disabledBackgroundMovement = new List<MonoBehaviour>();
    private readonly Dictionary<MonoBehaviour, bool> backgroundMovementStates = new Dictionary<MonoBehaviour, bool>();

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (typewriterCanvas == null)
        {
            typewriterCanvas = gameObject;
        }

        if (typewriterCanvas != null)
        {
            typewriterCanvas.SetActive(true);
        }
    }

    public void Display()
    {
        currentSentenceIndex = 0;
        ActivateCanvas();
        ShowCurrentSentence();
    }

    public void Display(string[] newSentences)
    {
        sentences = newSentences;
        Display();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        if (isTyping)
        {
            CompleteCurrentSentence();
            return;
        }

        if (isPausing)
        {
            AdvanceAfterSentence();
            return;
        }

        if (sentences == null || sentences.Length == 0)
        {
            HideCanvas();
            return;
        }

        if (currentSentenceIndex < sentences.Length - 1)
        {
            currentSentenceIndex++;
            ShowCurrentSentence();
            return;
        }

        HideCanvas();
    }

    private void ActivateCanvas()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (typewriterCanvas != null)
        {
            typewriterCanvas.SetActive(true);
        }

        DisableBackgroundMovement();
    }

    private void HideCanvas()
    {
        RestoreBackgroundMovement();
        RestoreTimeScale();

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        StopPauseRoutine();
        isTyping = false;
        isPausing = false;

        if (continueText != null)
        {
            continueText.gameObject.SetActive(false);
        }

        if (typewriterCanvas != null)
        {
            typewriterCanvas.SetActive(false);
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
            sentenceText.text = string.Empty;
            HideCanvas();
            return;
        }

        currentSentenceIndex = Mathf.Clamp(currentSentenceIndex, 0, sentences.Length - 1);

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        StopPauseRoutine();

        if (continueText != null)
        {
            continueText.gameObject.SetActive(false);
            continueText.text = continuePrompt;
        }

        typingRoutine = StartCoroutine(TypeSentence(sentences[currentSentenceIndex]));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        sentenceText.text = string.Empty;

        if (string.IsNullOrWhiteSpace(sentence))
        {
            yield return null;
            FinishSentence();
            yield break;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int charactersSinceSound = 0;

        for (int i = 0; i < sentence.Length; i++)
        {
            builder.Append(sentence[i]);
            sentenceText.text = builder.ToString();

            if (!char.IsWhiteSpace(sentence[i]))
            {
                charactersSinceSound++;

                if (charactersSinceSound >= 2)
                {
                    PlayTypingSound();
                    charactersSinceSound = 0;
                }
            }

            if (i < sentence.Length - 1)
            {
                yield return new WaitForSecondsRealtime(wordDelay);
            }
        }

        FinishSentence();
    }

    private void CompleteCurrentSentence()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        sentenceText.text = sentences != null && sentences.Length > 0
            ? sentences[currentSentenceIndex]
            : string.Empty;

        FinishSentence();
    }

    private void FinishSentence()
    {
        isTyping = false;
        typingRoutine = null;

        if (continueText != null)
        {
            continueText.gameObject.SetActive(true);
        }

        if (sentences != null && currentSentenceIndex < sentences.Length - 1)
        {
            StartPauseRoutine();
        }
    }

    private void StartPauseRoutine()
    {
        StopPauseRoutine();
        pauseRoutine = StartCoroutine(PauseBeforeNextSentence());
    }

    private IEnumerator PauseBeforeNextSentence()
    {
        isPausing = true;

        if (sentencePause > 0f)
        {
            yield return new WaitForSecondsRealtime(sentencePause);
        }

        pauseRoutine = null;
        AdvanceAfterSentence();
    }

    private void AdvanceAfterSentence()
    {
        StopPauseRoutine();
        isPausing = false;

        if (sentences == null || sentences.Length == 0)
        {
            HideCanvas();
            return;
        }

        if (currentSentenceIndex < sentences.Length - 1)
        {
            currentSentenceIndex++;
            ShowCurrentSentence();
            return;
        }

        HideCanvas();
    }

    private void StopPauseRoutine()
    {
        if (pauseRoutine == null)
        {
            return;
        }

        StopCoroutine(pauseRoutine);
        pauseRoutine = null;
    }

    private void DisableBackgroundMovement()
    {
        if (disabledBackgroundMovement.Count > 0)
        {
            return;
        }

        Parallax[] parallaxBackgrounds = FindObjectsByType<Parallax>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        ParallaxNew[] repeatingBackgrounds = FindObjectsByType<ParallaxNew>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Parallax background in parallaxBackgrounds)
        {
            CaptureAndDisable(background);
        }

        foreach (ParallaxNew background in repeatingBackgrounds)
        {
            CaptureAndDisable(background);
        }
    }

    private void CaptureAndDisable(MonoBehaviour background)
    {
        if (background == null || backgroundMovementStates.ContainsKey(background))
        {
            return;
        }

        backgroundMovementStates.Add(background, background.enabled);
        disabledBackgroundMovement.Add(background);
        background.enabled = false;
    }

    private void RestoreBackgroundMovement()
    {
        for (int i = 0; i < disabledBackgroundMovement.Count; i++)
        {
            MonoBehaviour background = disabledBackgroundMovement[i];

            if (background != null && backgroundMovementStates.TryGetValue(background, out bool wasEnabled))
            {
                background.enabled = wasEnabled;
            }
        }

        disabledBackgroundMovement.Clear();
        backgroundMovementStates.Clear();
    }

    private void OnDisable()
    {
        RestoreBackgroundMovement();
        RestoreTimeScale();
    }

    private void OnDestroy()
    {
        RestoreBackgroundMovement();
        RestoreTimeScale();
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = previousTimeScale;
    }

    private void PlayTypingSound()
    {
        if (audioSource == null || typingClip == null)
        {
            return;
        }

        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(typingClip, typingVolume);
    }
}
