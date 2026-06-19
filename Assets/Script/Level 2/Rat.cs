using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Rat : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float chaseRange = 7f;

    [Header("Attack")]
    [SerializeField] private float attackDelay = 0.7f;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "Poison Death";

    private Transform player;
    private Animator animator;

    private bool isAttacking = false;

    private void Start()
    {
        animator = GetComponent<Animator>();

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (player == null || isAttacking)
            return;

        float distance =
            Mathf.Abs(player.position.x - transform.position.x);

        bool shouldChase = distance <= chaseRange;

        if (animator != null)
        {
            animator.SetBool("Run", shouldChase);
        }

        if (!shouldChase)
            return;

        float direction =
            Mathf.Sign(player.position.x - transform.position.x);

        transform.position +=
            Vector3.right * direction *
            moveSpeed * Time.deltaTime;

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isAttacking)
            return;

        if (collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(AttackPlayer());
        }
    }

    private IEnumerator AttackPlayer()
    {
        isAttacking = true;

        if (animator != null)
        {
            animator.SetBool("Run", false);
            animator.SetTrigger("Attack");
        }

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