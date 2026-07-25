using UnityEngine;
using UnityEngine.UI;

// Two-image health bar: a static background Image sits behind a fill-type
// Image that shrinks/grows to reflect the player's current health. Wire
// HealthBarUI.SetHealth up to PlayerHealth's onHealthChanged event.
public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The front image. Must have Image Type = Filled (see setup notes).")]
    [SerializeField] private Image fillImage;

    [Header("Smoothing")]
    [Tooltip("How fast the bar animates toward the new value per second (in fill-amount units, 0-1). Set high for an instant snap.")]
    [SerializeField] private float depletionSpeed = 2f;

    private float targetFillAmount = 1f;

    private void Awake()
    {
        if (fillImage != null)
        {
            targetFillAmount = fillImage.fillAmount;
        }
    }

    private void Update()
    {
        if (fillImage == null)
        {
            return;
        }

        fillImage.fillAmount = Mathf.MoveTowards(
            fillImage.fillAmount,
            targetFillAmount,
            depletionSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Call from PlayerHealth.onHealthChanged (int currentHealth, int maxHealth).
    /// </summary>
    public void SetHealth(int currentHealth, int maxHealth)
    {
        targetFillAmount = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }
}