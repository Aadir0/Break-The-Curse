using UnityEngine;
using UnityEngine.UI;

// World-space enemy health bar that stays hidden until the player actually
// engages this enemy in combat (first hit, block, or parry), then fades back
// out if there's no combat for a while. Wire up on the EnemyHealth Events:
//   onHealthChanged -> SetHealth
//   onCombatEngaged -> NotifyCombatEngaged
public class EnemyHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [Tooltip("The object to show/hide - usually the parent holding both background and fill images.")]
    [SerializeField] private GameObject visualsRoot;

    [Header("Behavior")]
    [Tooltip("How long after the last combat interaction before the bar hides again. Set to 0 or less to never hide once shown.")]
    [SerializeField] private float hideDelayAfterCombat = 5f;
    [SerializeField] private float fillSpeed = 4f;

    private float targetFillAmount = 1f;
    private float lastCombatTime = float.NegativeInfinity;
    private bool isVisible;

    private void Awake()
    {
        if (fillImage != null)
        {
            targetFillAmount = fillImage.fillAmount;
        }

        SetVisible(false);
    }

    private void Update()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.MoveTowards(fillImage.fillAmount, targetFillAmount, fillSpeed * Time.deltaTime);
        }

        if (isVisible && hideDelayAfterCombat > 0f && Time.time - lastCombatTime > hideDelayAfterCombat)
        {
            SetVisible(false);
        }
    }

    /// <summary>Call from EnemyHealth.onHealthChanged (currentHealth, maxHealth).</summary>
    public void SetHealth(int current, int max)
    {
        targetFillAmount = max > 0 ? (float)current / max : 0f;
    }

    /// <summary>Call from EnemyHealth.onCombatEngaged - shows the bar and resets the auto-hide timer.</summary>
    public void NotifyCombatEngaged()
    {
        lastCombatTime = Time.time;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        if (visualsRoot != null)
        {
            visualsRoot.SetActive(visible);
        }
    }
}
