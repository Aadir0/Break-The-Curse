using UnityEngine;
using UnityEngine.UI;

public class PoisonOverlay : MonoBehaviour
{
    public static PoisonOverlay Instance;

    [SerializeField] private Image overlay;
    [SerializeField] private float fadeSpeed = 0.5f;

    private bool poisoning;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!poisoning)
            return;

        Color c = overlay.color;

        c.a = Mathf.MoveTowards(
            c.a,
            0.4f,
            fadeSpeed * Time.deltaTime);

        overlay.color = c;
    }

    public void StartPoison()
    {
        poisoning = true;
    }
}