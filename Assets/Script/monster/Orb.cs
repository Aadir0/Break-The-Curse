using UnityEngine;
using UnityEngine.SceneManagement;

public class Orb : MonoBehaviour
{
    private Inversion inversion;
    [SerializeField] private float baseSpeed = 4f;
    [SerializeField] private float speedIncreasePerSecond = 0.25f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float rotationOffset = 0f;
    private Transform player;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }
    private void FixedUpdate()
    {
        if (player == null)
            return;

        float speed = baseSpeed;

        if (inversion != null)
        {
            speed += inversion.GetInvertedTime() * speedIncreasePerSecond;
        }

        Vector2 direction =
            (player.position - transform.position).normalized;

        float targetAngle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;

        rb.MoveRotation(
            Mathf.MoveTowardsAngle(
                rb.rotation,
                targetAngle,
                rotationSpeed * Time.fixedDeltaTime));

        rb.linearVelocity = direction * speed;
    }
    public void SetInversion(Inversion inversionScript)
    {
        inversion = inversionScript;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}
