using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject TutorialPanel;
    [SerializeField] private Slider backgroundMusicSlider;
    [SerializeField] private BackGroundSound backGroundSound;
    [SerializeField] private GameObject OptionsPanel;
    [SerializeField] private GameObject CreditsPanel;
    [SerializeField] private GameObject[] buttonsToDisableOnPause;
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject playHoverObject;
    [SerializeField] private Color playHoverColor = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField] private string playHoverTag = "Play";
    [SerializeField] private bool usePlayHoverTag = true;
    [SerializeField] private LayerMask playHoverLayer;
    [SerializeField] private bool usePlayHoverLayer;
    [SerializeField] private GameObject StoryPanel;

    private Image playHoverImage;
    private SpriteRenderer playHoverSpriteRenderer;
    private Color originalPlayHoverColor;
    private bool playHoverActive;

    private void Awake()
    {
        if (backGroundSound == null)
        {
            backGroundSound = FindAnyObjectByType<BackGroundSound>();
        }

        if (backgroundMusicSlider != null && backGroundSound != null)
        {
            backgroundMusicSlider.SetValueWithoutNotify(GetCurrentMusicVolume());
            backgroundMusicSlider.onValueChanged.AddListener(SetBackgroundMusicVolume);
        }

        CachePlayHoverRenderer();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Time.timeScale = Time.timeScale == 0f ? 1f : 0f; // Toggle pause state
            pauseMenuUI.SetActive(!pauseMenuUI.activeSelf);
        }
    }

    private float GetCurrentMusicVolume()
    {
        AudioSource musicSource = backGroundSound != null ? backGroundSound.GetComponent<AudioSource>() : null;
        return musicSource != null ? musicSource.volume : 1f;
    }

    public void StartGame()
    {
        if (TutorialPanel != null)
        {
            TutorialPanel.SetActive(true);
        }
    }

    public void Story()
    {
        if (StoryPanel != null)
        {
            Time.timeScale = 0f; // Pause the game
            for (int i = 0; i < buttonsToDisableOnPause.Length; i++)
            {
                if (buttonsToDisableOnPause[i] != null)
                {
                    buttonsToDisableOnPause[i].SetActive(false);
                }
            }
            SetPlayHoverColor(playHoverColor);
            StoryPanel.SetActive(true);
        }
    }

    public void CloseStory()
    {
        if (StoryPanel != null)
        {
            Time.timeScale = 1f; // Resume the game
            for (int i = 0; i < buttonsToDisableOnPause.Length; i++)
            {
                if (buttonsToDisableOnPause[i] != null)
                {
                    buttonsToDisableOnPause[i].SetActive(true);
                }
            }
            SetPlayHoverColor(originalPlayHoverColor);
            StoryPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void Options()
    {
        if (OptionsPanel != null)
        {
            for (int i = 0; i < buttonsToDisableOnPause.Length; i++)
            {
                if (buttonsToDisableOnPause[i] != null)
                {
                    buttonsToDisableOnPause[i].SetActive(false);
                }
            }
            OptionsPanel.SetActive(true);
        }
    }

    public void CloseOptions()
    {
        if (OptionsPanel != null)
        {
            OptionsPanel.SetActive(false);
        }
        if (CreditsPanel != null)
        {
            CreditsPanel.SetActive(false);
        }
        for (int i = 0; i < buttonsToDisableOnPause.Length; i++)
        {
            if (buttonsToDisableOnPause[i] != null)
            {
                buttonsToDisableOnPause[i].SetActive(true);
            }
        }
    }

    public void Credits()
    {
        if (CreditsPanel != null)
        {
            CreditsPanel.SetActive(true);
            for (int i = 0; i < buttonsToDisableOnPause.Length; i++)
            {
                if (buttonsToDisableOnPause[i] != null)
                {
                    buttonsToDisableOnPause[i].SetActive(false);
                }
            }
        }
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void Resume()
    {
        Time.timeScale = 1f; // Resume the game
        pauseMenuUI.SetActive(false);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SetBackgroundMusicVolume(float volume)
    {
        if (backGroundSound == null)
        {
            backGroundSound = FindAnyObjectByType<BackGroundSound>();
        }

        if (backGroundSound != null)
        {
            backGroundSound.SetMusicVolume(volume);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsPlayHoverTarget(eventData.pointerEnter))
        {
            return;
        }

        playHoverActive = true;
        SetPlayHoverColor(playHoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!playHoverActive)
        {
            return;
        }

        playHoverActive = false;
        SetPlayHoverColor(originalPlayHoverColor);
    }

    private void CachePlayHoverRenderer()
    {
        if (playHoverObject == null)
        {
            return;
        }

        playHoverImage = playHoverObject.GetComponent<Image>();
        playHoverSpriteRenderer = playHoverObject.GetComponent<SpriteRenderer>();

        if (playHoverImage != null)
        {
            originalPlayHoverColor = playHoverImage.color;
        }
        else if (playHoverSpriteRenderer != null)
        {
            originalPlayHoverColor = playHoverSpriteRenderer.color;
        }
    }

    private void SetPlayHoverColor(Color color)
    {
        if (playHoverImage != null)
        {
            playHoverImage.color = color;
        }

        if (playHoverSpriteRenderer != null)
        {
            playHoverSpriteRenderer.color = color;
        }
    }

    private bool IsPlayHoverTarget(GameObject pointerTarget)
    {
        if (pointerTarget == null)
        {
            return false;
        }

        Transform current = pointerTarget.transform;

        while (current != null)
        {
            GameObject currentObject = current.gameObject;

            if (MatchesPlayHoverTag(currentObject) && MatchesPlayHoverLayer(currentObject))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool MatchesPlayHoverTag(GameObject target)
    {
        return !usePlayHoverTag
            || string.IsNullOrEmpty(playHoverTag)
            || target.tag == playHoverTag;
    }

    private bool MatchesPlayHoverLayer(GameObject target)
    {
        return !usePlayHoverLayer
            || (playHoverLayer.value & (1 << target.layer)) != 0;
    }
}
