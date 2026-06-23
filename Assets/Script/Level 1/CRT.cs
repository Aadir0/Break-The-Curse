using UnityEngine;

public class CRT : MonoBehaviour
{
    public CanvasGroup cg;
    [SerializeField] private float minAlpha = 0.08f;
    [SerializeField] private float maxAlpha = 0.45f;
    [SerializeField] private float flickerSpeed = 8f;
    private float targetAlpha;

    void Start()
    {
        targetAlpha = cg != null ? cg.alpha : minAlpha;
    }

    void Update()
    {
        if (cg == null)
        {
            return;
        }

        if (Random.value < Time.deltaTime * flickerSpeed)
        {
            targetAlpha = Random.Range(minAlpha, maxAlpha);
        }

        cg.alpha = Mathf.Lerp(cg.alpha, targetAlpha, Time.deltaTime * flickerSpeed);
    }
}
