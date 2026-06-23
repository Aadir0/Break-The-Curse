using UnityEngine;

public class BackGroundSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip normalWorldMusic;
    [SerializeField] private AudioClip invertedWorldMusic;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Inversion inversion;
    private bool isInvertedWorld;
    private static BackGroundSound instance;

    void Awake()
    {
        // Singleton pattern - persist across scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        inversion = FindAnyObjectByType<Inversion>();
    }

    void Start()
    {
        inversion = FindAnyObjectByType<Inversion>();
        if (inversion != null)
        {
            isInvertedWorld = inversion.isInverted;
            if (isInvertedWorld)
            {
                PlayInvertedWorldMusic();
            }
            else
            {
                PlayNormalWorldMusic();
            }
        }
        else
        {
            PlayNormalWorldMusic();
        }
    }

    void Update()
    {
        inversion = FindAnyObjectByType<Inversion>();
        if (inversion == null)
            return;

        if (inversion.isInverted != isInvertedWorld)
        {
            ToggleWorldState();
        }

        // Optional: Handle E key directly
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleWorldState();
        }
    }
    public void SwitchToInvertedWorld()
    {
        if (isInvertedWorld)
            return;

        isInvertedWorld = true;
        SwitchMusic(invertedWorldMusic);
    }

    public void SwitchToNormalWorld()
    {
        if (!isInvertedWorld)
            return;

        isInvertedWorld = false;
        SwitchMusic(normalWorldMusic);
    }

    public void ToggleWorldState()
    {
        if (isInvertedWorld)
        {
            SwitchToNormalWorld();
        }
        else
        {
            SwitchToInvertedWorld();
        }
    }

    void PlayNormalWorldMusic()
    {
        if (normalWorldMusic != null)
        {
            audioSource.clip = normalWorldMusic;
            audioSource.Play();
            isInvertedWorld = false;
        }
    }

    void PlayInvertedWorldMusic()
    {
        if (invertedWorldMusic != null)
        {
            audioSource.clip = invertedWorldMusic;
            audioSource.Play();
            isInvertedWorld = true;
        }
    }

    void SwitchMusic(AudioClip newClip)
    {
        if (newClip == null)
            return;

        StartCoroutine(FadeOutAndSwitchMusic(newClip));
    }

    System.Collections.IEnumerator FadeOutAndSwitchMusic(AudioClip newClip)
    {
        float elapsedTime = 0f;
        float startVolume = audioSource.volume;

        // Fade out
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        // Switch clip
        audioSource.clip = newClip;
        audioSource.Play();

        // Fade in
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, startVolume, elapsedTime / fadeDuration);
            yield return null;
        }

        audioSource.volume = startVolume;
    }
}
