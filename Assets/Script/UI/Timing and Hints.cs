using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TimingandHints : MonoBehaviour
{
    [SerializeField] private GameObject hintCanvas;
    [SerializeField] private Text timerText;
    [SerializeField] private Text hintText;
    [SerializeField] private string[] hints;
    [SerializeField] private float timeBeforeHint = 60f;
    [SerializeField] private float hintDisplayDuration = 10f;
    [SerializeField] private string timerFormat = "Hint in: {0}s";
    [SerializeField] private string hintReadyText = "Press H for hint";
    [SerializeField] private int maxHintsToShow = 2;
    [SerializeField] private KeyCode hintKey = KeyCode.H;

    private Coroutine hintTimerCoroutine;
    private Coroutine hideHintCoroutine;
    private int hintIndex;
    private int completedHints;
    private bool hintAvailable;

    private void Awake()
    {
        if (hintCanvas == null && hintText != null)
        {
            hintCanvas = hintText.gameObject;
        }
    }

    private void Start()
    {
        HideHint();
        SetTimerTextActive(true);
        UpdateTimerText(timeBeforeHint);

        StartHintTimer();
    }

    private void Update()
    {
        if (Input.GetKeyDown(hintKey))
        {
            ShowHintFromButton();
        }
    }

    public void ShowHintFromButton()
    {
        if (!hintAvailable)
        {
            return;
        }

        ShowHint();
    }

    private void StartHintTimer()
    {
        if (completedHints >= maxHintsToShow)
        {
            SetTimerTextActive(false);
            return;
        }

        if (hintTimerCoroutine != null)
        {
            StopCoroutine(hintTimerCoroutine);
        }

        UpdateTimerText(timeBeforeHint);
        hintAvailable = false;

        hintTimerCoroutine = StartCoroutine(HintTimerRoutine());
    }

    private IEnumerator HintTimerRoutine()
    {
        float timer = timeBeforeHint;

        while (timer > 0f)
        {
            UpdateTimerText(timer);
            timer -= Time.deltaTime;
            yield return null;
        }

        UpdateTimerText(0f);
        hintTimerCoroutine = null;
        hintAvailable = true;

        if (timerText != null)
        {
            timerText.text = hintReadyText;
        }
    }

    private void ShowHint()
    {
        if (hintCanvas == null)
        {
            return;
        }

        if (hintTimerCoroutine != null)
        {
            StopCoroutine(hintTimerCoroutine);
            hintTimerCoroutine = null;
        }

        hintAvailable = false;

        if (hintText != null && hints != null && hints.Length > 0)
        {
            hintText.text = hints[hintIndex];
        }

        hintCanvas.SetActive(true);
        SetHintTextActive(true);
        UpdateTimerText(0f);

        if (hideHintCoroutine != null)
        {
            StopCoroutine(hideHintCoroutine);
        }

        hideHintCoroutine = StartCoroutine(HideHintAfterDelay());
    }

    private IEnumerator HideHintAfterDelay()
    {
        yield return new WaitForSeconds(hintDisplayDuration);

        HideHint();

        if (hints != null && hints.Length > 0)
        {
            hintIndex = (hintIndex + 1) % hints.Length;
        }

        completedHints++;
        StartHintTimer();
    }

    private void HideHint()
    {
        if (hintCanvas != null)
        {
            hintCanvas.SetActive(true);
        }

        SetHintTextActive(false);
    }

    private void SetHintTextActive(bool isActive)
    {
        if (hintText != null)
        {
            hintText.gameObject.SetActive(isActive);
        }
    }

    private void UpdateTimerText(float timeRemaining)
    {
        if (timerText == null)
        {
            return;
        }

        int seconds = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
        timerText.text = string.Format(timerFormat, seconds);
    }

    private void SetTimerTextActive(bool isActive)
    {
        if (timerText != null)
        {
            timerText.gameObject.SetActive(isActive);
        }
    }
}
