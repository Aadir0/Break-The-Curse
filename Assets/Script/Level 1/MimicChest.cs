using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MimicChest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Settings")]
    [SerializeField] private float deathDelay = 1.2f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    private bool playerInRange;
    private bool triggered;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        if (triggered)
            return;

        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            StartCoroutine(MimicAttack());
        }
    }

    private IEnumerator MimicAttack()
    {
        triggered = true;

        // Freeze player movement
        PlayerController player =
            FindFirstObjectByType<PlayerController>();

        if (player != null)
        {
            player.enabled = false;
        }

        // Play mimic animation
        if (animator != null)
        {
            animator.Play("Open");
        }

        yield return new WaitForSeconds(deathDelay);

        Debug.Log("UNEXPECTED DEATH");

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}