using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PoisonMushroom : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Level3";
    [SerializeField] private float poisonDuration = 3f;

    private bool playerInRange;
    private bool poisoned;

    private SpriteRenderer playerSprite;
    private PlayerController playerController;

    private void Update()
    {
        if (poisoned)
            return;

        if (playerInRange &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartCoroutine(PoisonSequence());
        }
    }

    private IEnumerator PoisonSequence()
    {
        poisoned = true;

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerSprite = player.GetComponent<SpriteRenderer>();
            playerController = player.GetComponent<PlayerController>();
        }

        // Turn player green
        if (playerSprite != null)
        {
            playerSprite.color = Color.green;
        }

        // Start effects
        if (PoisonOverlay.Instance != null)
        {
            PoisonOverlay.Instance.StartPoison();
            CameraDizzy.Instance.StartDizzy();
        }

        if (CameraDizzy.Instance != null)
        {
            CameraDizzy.Instance.StartDizzy();
        }

        // Disable movement
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        yield return new WaitForSeconds(poisonDuration);

        SceneManager.LoadScene(nextSceneName);
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