using UnityEngine;

public class CameraDizzy : MonoBehaviour
{
    public static CameraDizzy Instance;

    [SerializeField] private float maxRotation = 12f;
    [SerializeField] private float wobbleSpeed = 6f;

    private bool dizzy;
    private float effectTimer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!dizzy)
            return;

        effectTimer += Time.deltaTime;

        float currentRotation =
            Mathf.Lerp(0f,
                       maxRotation,
                       effectTimer / 3f);

        float angle =
            Mathf.Sin(Time.time * wobbleSpeed)
            * currentRotation;

        transform.rotation =
            Quaternion.Euler(0f, 0f, angle);
    }

    public void StartDizzy()
    {
        dizzy = true;
    }
}