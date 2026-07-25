using UnityEngine;
using UnityEngine.UI;

// Posture ("parry"/"guard") bar that grows outward from the CENTER instead
// of from one edge. Unity's built-in Image "Filled" type has no middle
// origin, so this uses two mirrored half-bar Images instead: one growing
// left from center, one growing right from center, both driven by the same
// value so they meet and grow outward together.
//
// Reusable for both:
//   - EnemyHealth.onPostureChanged / onCombatEngaged (the enemy's bar)
//   - PlayerHealth.onGuardPostureChanged / onCombatEngaged (the player's bar)
// Stays hidden until combat starts, then fades back out after a delay of
// no combat - same behavior as EnemyHealthBarUI.
public class PostureBarUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Left half of the bar. Image Type = Filled, Fill Method = Horizontal, Fill Origin = Right (so it grows leftward, away from center).")]
    [SerializeField] private Image leftFillImage;
    [Tooltip("Right half of the bar. Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left (so it grows rightward, away from center).")]
    [SerializeField] private Image rightFillImage;
    [Tooltip("The object to show/hide - usually the parent holding background + both fill halves. Must NOT be the same object this script is on.")]
    [SerializeField] private GameObject visualsRoot;

    [Header("Behavior")]
    [Tooltip("How long after the last combat interaction before the bar hides again. Set to 0 or less to never hide once shown.")]
    [SerializeField] private float hideDelayAfterCombat = 5f;
    [SerializeField] private float fillSpeed = 4f;

    [Header("Near-Full Warning")]
    [Tooltip("Above this fraction (0-1), the bar tints toward warningColor to telegraph an incoming stagger/guard-break.")]
    [SerializeField] private float warningThreshold = 0.8f;
    [SerializeField] private Color normalColor = Color.yellow;
    [SerializeField] private Color warningColor = Color.red;

    private float targetFillAmount;
    private float currentFillAmount;
    private float lastCombatTime = float.NegativeInfinity;
    private bool isVisible;

    private void Awake()
    {
        if (leftFillImage != null)
        {
            currentFillAmount = leftFillImage.fillAmount;
            targetFillAmount = currentFillAmount;
        }

        SetVisible(false);
    }

    private void Update()
    {
        currentFillAmount = Mathf.MoveTowards(currentFillAmount, targetFillAmount, fillSpeed * Time.deltaTime);
        Color color = currentFillAmount >= warningThreshold ? warningColor : normalColor;

        if (leftFillImage != null)
        {
            leftFillImage.fillAmount = currentFillAmount;
            leftFillImage.color = color;
        }

        if (rightFillImage != null)
        {
            rightFillImage.fillAmount = currentFillAmount;
            rightFillImage.color = color;
        }

        if (isVisible && hideDelayAfterCombat > 0f && Time.time - lastCombatTime > hideDelayAfterCombat)
        {
            SetVisible(false);
        }
    }

    /// <summary>Call from EnemyHealth.onPostureChanged or PlayerHealth.onGuardPostureChanged (current, max).</summary>
    public void SetPosture(float current, float max)
    {
        targetFillAmount = max > 0f ? current / max : 0f;
    }

    /// <summary>Call from EnemyHealth.onCombatEngaged or PlayerHealth.onCombatEngaged - shows the bar and resets the auto-hide timer.</summary>
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