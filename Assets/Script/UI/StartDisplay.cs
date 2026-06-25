using UnityEngine;
using System.Collections;

public class StartDisplay : MonoBehaviour
{
    [SerializeField] private GameObject displayCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayDuration = 2.5f;

    private void Awake()
    {
        if (displayCanvas == null)
        {
            displayCanvas = gameObject;
        }

        if (canvasGroup == null)
        {
            canvasGroup = displayCanvas.GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = displayCanvas.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        StartCoroutine(ShowStartDisplay());
    }

    private IEnumerator ShowStartDisplay()
    {
        displayCanvas.SetActive(true);
        canvasGroup.alpha = 0f;

        yield return FadeCanvas(0f, 1f);
        yield return new WaitForSeconds(displayDuration);
        yield return FadeCanvas(1f, 0f);

        displayCanvas.SetActive(false);
    }

    private IEnumerator FadeCanvas(float startAlpha, float endAlpha)
    {
        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = endAlpha;
            yield break;
        }

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }
}
