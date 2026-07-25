using UnityEngine;

public class ParallaxNew : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Parallax Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxSpeed = 0.3f;

    private float spriteWidth;
    private Vector3 lastPlayerPosition;

    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr != null)
            spriteWidth = sr.bounds.size.x;

        lastPlayerPosition = player.position;

        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void LateUpdate()
    {
        // Player movement since last frame
        float deltaX = player.position.x - lastPlayerPosition.x;

        // Move background according to player movement
        transform.position += new Vector3(-deltaX * parallaxSpeed, 0f, 0f);

        lastPlayerPosition = player.position;

        RepeatBackground();
    }

    private void RepeatBackground()
    {
        Camera cam = Camera.main;

        float cameraLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        float cameraRight = cam.transform.position.x + cam.orthographicSize * cam.aspect;

        // Background completely left of camera
        if (transform.position.x + spriteWidth / 2 < cameraLeft)
        {
            transform.position += Vector3.right * spriteWidth * 2f;
        }

        // Background completely right of camera
        else if (transform.position.x - spriteWidth / 2 > cameraRight)
        {
            transform.position -= Vector3.right * spriteWidth * 2f;
        }
    }
}