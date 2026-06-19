using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MimicChestLVL2 : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRange = 2f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float chaseRange = 8f;

    [Header("Attack")]
    [SerializeField] private float attackDelay = 1f;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "Poison Death";

    private Animator animator;
    private Transform player;

    private bool playerInRange;
    private bool awakened;
    private bool attacking;

    private void Start()
    {
        animator = GetComponent<Animator>();

        GameObject playerObj =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (player == null)
            return;

        float distance =
            Vector2.Distance(transform.position, player.position);

        playerInRange = distance <= interactRange;

        // Wake mimic
        if (!awakened &&
            playerInRange &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartCoroutine(AwakenMimic());
        }

        if (!awakened || attacking)
            return;

        bool shouldChase = distance <= chaseRange;

        animator.SetBool("Walk", shouldChase);

        if (!shouldChase)
            return;

        float direction =
            Mathf.Sign(player.position.x - transform.position.x);

        transform.position +=
            Vector3.right *
            direction *
            moveSpeed *
            Time.deltaTime;

        Vector3 scale = transform.localScale;

        if (direction > 0)
        {
            scale.x = Mathf.Abs(scale.x);
        }
        else
        {
            scale.x = -Mathf.Abs(scale.x);
        }

        transform.localScale = scale;
    }

    private IEnumerator AwakenMimic()
    {
        awakened = true;

        animator.SetTrigger("Open");

        yield return new WaitForSeconds(1f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (attacking)
            return;

        if (!awakened)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(AttackPlayer());
        }
    }

    private IEnumerator AttackPlayer()
    {
        attacking = true;

        animator.SetBool("Walk", false);
        animator.SetTrigger("Attack");

        PlayerController playerController =
            player.GetComponent<PlayerController>();

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        yield return new WaitForSeconds(attackDelay);

        SceneManager.LoadScene(nextSceneName);
    }
}