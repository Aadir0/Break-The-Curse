using UnityEngine;
using TMPro;

// Generic floating combat-text popup: rises, fades, then destroys itself.
// Used for posture-break/parry numbers, but reusable for anything else
// (damage numbers, "GUARD BROKEN!", etc) if you want later.
[RequireComponent(typeof(TextMeshPro))]
public class DamagePopup : MonoBehaviour
{
    [SerializeField] private float riseSpeed = 1.5f;
    [SerializeField] private float lifetime = 0.8f;
    [Tooltip("Small random horizontal drift so stacked popups don't overlap exactly.")]
    [SerializeField] private float horizontalJitter = 0.3f;

    private TextMeshPro label;
    private float timer;
    private Color startColor;

    private void Awake()
    {
        label = GetComponent<TextMeshPro>();

        Vector3 pos = transform.position;
        pos.x += Random.Range(-horizontalJitter, horizontalJitter);
        transform.position = pos;
    }

    public void Show(string text, Color color)
    {
        label.text = text;
        label.color = color;
        startColor = color;
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        float t = Mathf.Clamp01(timer / lifetime);
        Color c = startColor;
        c.a = Mathf.Lerp(1f, 0f, t);
        label.color = c;

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
