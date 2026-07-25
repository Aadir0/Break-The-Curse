using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HealthPotionPickup : MonoBehaviour
{
    [SerializeField] private int potionAmount = 1;
    [SerializeField] private bool destroyWhenInventoryIsFull;
    [SerializeField] private Sprite potionSprite;
    [SerializeField] private Color potionColor = new Color(0.9f, 0.1f, 0.25f, 1f);
    [SerializeField] private float visualScale = 0.75f;

    private static Sprite fallbackSprite;

    private void Reset()
    {
        Collider2D pickupCollider = GetComponent<Collider2D>();
        pickupCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider2D pickupCollider = GetComponent<Collider2D>();

        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }

        EnsureVisual();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerPotionInventory inventory = other.GetComponentInParent<PlayerPotionInventory>();

        if (inventory == null)
        {
            return;
        }

        bool added = inventory.AddPotion(potionAmount);

        if (added || destroyWhenInventoryIsFull)
        {
            Destroy(gameObject);
        }
    }

    private void EnsureVisual()
    {
        if (TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = potionSprite != null ? potionSprite : GetFallbackSprite();
            }

            spriteRenderer.color = potionColor;
            transform.localScale = Vector3.one * visualScale;
            return;
        }

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = potionSprite != null ? potionSprite : GetFallbackSprite();
        spriteRenderer.color = potionColor;
        transform.localScale = Vector3.one * visualScale;
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
        {
            return fallbackSprite;
        }

        const int size = 16;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color outline = new Color(0.35f, 0f, 0.08f, 1f);
        Color fill = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool neck = x >= 6 && x <= 9 && y >= 10 && y <= 13;
                bool body = (x - 7.5f) * (x - 7.5f) / 25f + (y - 5.5f) * (y - 5.5f) / 20f <= 1f;
                bool cap = x >= 5 && x <= 10 && y == 14;
                bool edge = x == 5 || x == 10 || y == 3 || y == 14;

                if (cap || neck || body)
                {
                    texture.SetPixel(x, y, edge ? outline : fill);
                }
                else
                {
                    texture.SetPixel(x, y, clear);
                }
            }
        }

        texture.Apply();
        fallbackSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return fallbackSprite;
    }
}
