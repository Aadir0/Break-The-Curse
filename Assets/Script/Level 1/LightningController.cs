using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.Rendering.Universal;

public class LightningController : MonoBehaviour
{
    [Header("Lightning Timing")]
    public float minLightningTime = 5f;
    public float maxLightningTime = 15f;

    [Header("Light")]
    public Light2D globalLight;
    public float normalIntensity = 1f;
    public float lightningIntensity = 2.5f;

    [Header("Thunder")]
    public AudioSource audioSource;
    public AudioClip[] thunderClips;

    [Header("Camera Shake")]
    public CinemachineImpulseSource impulseSource;

    [Header("Rain")]
    [SerializeField] private ParticleSystem rainParticles;
    [SerializeField] private float windStrength = 3f;
    [SerializeField] private float windSmoothSpeed = 3f;
    [SerializeField] private PlayerController player;
    private ParticleSystem.VelocityOverLifetimeModule rainVelocity;
    private float currentWindX;
    private float targetWindX;

    [Header("Rain Audio")]
    [SerializeField] private AudioSource rainAudioSource;
    [SerializeField] private AudioClip rainLoop;
    private float targetRainVolume;
    [SerializeField] private float minRainVolume = 0.3f;
    [SerializeField] private float maxRainVolume = 0.7f;

    private void Start()
    {
        globalLight.intensity = normalIntensity;
        StartCoroutine(LightningRoutine());
        rainVelocity = rainParticles.velocityOverLifetime;
        rainVelocity.enabled = true;
        rainAudioSource.clip = rainLoop;
        rainAudioSource.loop = true;
        targetRainVolume = minRainVolume;
        rainAudioSource.Play();
    }

    private void Update()
    {
        UpdateRainWind();
        rainAudioSource.volume = Mathf.Lerp(rainAudioSource.volume, targetRainVolume, Time.deltaTime * 2f);
    }

    IEnumerator LightningRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                Random.Range(minLightningTime, maxLightningTime));

            yield return StartCoroutine(DoLightning());

            float thunderDelay =
                Random.Range(0.5f, 2f);

            yield return new WaitForSeconds(thunderDelay);

            PlayThunder();
        }
    }

    IEnumerator DoLightning()
    {
        yield return StartCoroutine(FlashLightning(
            lightningIntensity,
            0.08f));

        impulseSource.GenerateImpulse();

        if (Random.value > 0.5f)
        {
            yield return new WaitForSeconds(0.08f);

            yield return StartCoroutine(FlashLightning(
                lightningIntensity * 1.2f,
                0.05f));

            impulseSource.GenerateImpulse();
        }
    }

   IEnumerator FlashLightning(float intensity, float duration)
    {
        globalLight.intensity = intensity;
        targetRainVolume = maxRainVolume;
        yield return new WaitForSeconds(duration);
        globalLight.intensity = normalIntensity;
        targetRainVolume = minRainVolume;
    }
    void UpdateRainWind()
    {
        if (player == null)
            return;

        float moveInput = player.currentMoveInput;

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            // Rain blows opposite to player movement
            targetWindX = Mathf.Sin(Time.time * 0.25f) * windStrength - moveInput * (windStrength * 0.5f);
        }

        currentWindX = Mathf.Lerp(
            currentWindX,
            targetWindX,
            windSmoothSpeed * Time.deltaTime);

        rainVelocity.x = new ParticleSystem.MinMaxCurve(currentWindX);
    }

    void PlayThunder()
    {
        if (thunderClips.Length == 0)
            return;

        AudioClip clip =
            thunderClips[
                Random.Range(0, thunderClips.Length)];

        audioSource.PlayOneShot(clip);
    }
}
