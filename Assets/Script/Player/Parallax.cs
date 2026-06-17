using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] private float speed = 1f;   // Movement speed
    [SerializeField] private SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer component
    [SerializeField] private PlayerController player;
    public float resetX = -15f;
    public float startX = 15f;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }
    }

    void Update()
    {
        if (player != null && player.MoveDirection != 0f)
        {
            transform.Translate(Vector3.right * player.MoveDirection * speed * Time.deltaTime);
        }

        if (spriteRenderer != null && transform.position.x <= spriteRenderer.bounds.size.x * -1)
        {
            transform.position += Vector3.right * spriteRenderer.bounds.size.x * 2f;
        }
    }
}