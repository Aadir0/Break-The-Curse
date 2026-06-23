using UnityEngine;
using UnityEngine.SceneManagement;

public class Orb : MonoBehaviour
{
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float spawnDistanceFromPlayer = 8f;
    
    private Rigidbody2D rb;
    private Transform player;
    private Camera mainCamera;
    private bool isInCameraView;
    private bool wasInPreviousWorld = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        mainCamera = Camera.main;
        
        // Ensure this enemy has the "Enemy" tag
        gameObject.tag = "Enemy";
    }

    void Update()
    {
        if (player == null || mainCamera == null)
            return;

        // Check if world has switched (you may need to adjust this based on your world-switching mechanism)
        bool isCurrentlyInInvertedWorld = GetIsInInvertedWorld();
        
        if (wasInPreviousWorld != isCurrentlyInInvertedWorld)
        {
            // World switched - reposition orb
            RespawnOrbNearPlayer();
            wasInPreviousWorld = isCurrentlyInInvertedWorld;
        }

        // Check if Orb is visible in camera view
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(transform.position);
        isInCameraView = viewportPoint.x >= 0 && viewportPoint.x <= 1 && 
                         viewportPoint.y >= 0 && viewportPoint.y <= 1 && 
                         viewportPoint.z > 0;

        // Only chase if in camera view and within detection range
        if (isInCameraView)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= detectionRange)
            {
                ChasePlayer();
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void ChasePlayer()
    {
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        
        // Move towards player
        rb.linearVelocity = directionToPlayer * chaseSpeed;
        
        // Turn to face player
        float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void RespawnOrbNearPlayer()
    {
        if (player == null)
            return;

        // Spawn orb at a fixed distance from player in a random direction
        float randomAngle = Random.Range(0f, 360f);
        Vector2 spawnOffset = new Vector2(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad),
            Mathf.Sin(randomAngle * Mathf.Deg2Rad)
        ) * spawnDistanceFromPlayer;

        transform.position = (Vector2)player.position + spawnOffset;
        rb.linearVelocity = Vector2.zero;
    }

    bool GetIsInInvertedWorld()
    {
        // TODO: Replace this with your actual world-switching detection logic
        // Examples:
        // - Check a static boolean from a game manager
        // - Check current scene name
        // - Check player's current layer/state
        // For now, this is a placeholder you need to implement based on your game logic
        
        // Example: if you have a GameManager with a static bool
        // return GameManager.isInvertedWorld;
        
        // Example: if you check by scene name
        // return SceneManager.GetActiveScene().name.Contains("Inverted");
        
        return false; // Change this to your actual logic
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Kill player on collision
        if (collision.gameObject.CompareTag("Player"))
        {
            Destroy(collision.gameObject);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Reload the current scene
        }
    }
}
