using UnityEngine;
using UnityEngine.SceneManagement;

public class Mimic : MonoBehaviour
{
    [Header("Mimic")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform player;
    [SerializeField] private PlayerController playerController;

    [Header("Ranges")]
    [SerializeField] private float normalRange = 4f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackDuration = 0.35f;
    [SerializeField] private float transformDuration = 0.2f;
    [SerializeField] private float transformBackDuration = 0.2f;

    private bool isAttacking;
    private bool playerKilled;
    private bool wasInNormalRange;
    private Coroutine transformRoutine;
    private Coroutine transformBackRoutine;
    private Coroutine attackRoutine;

    private static readonly int TransformBool = Animator.StringToHash("transform");
    private static readonly int TransformBackBool = Animator.StringToHash("transformback");
    private static readonly int IsAttackingBool = Animator.StringToHash("isAttacking");

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    void Update()
    {
        if (playerKilled)
        {
            return;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (animator == null || player == null)
        {
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool inNormalRange = distanceToPlayer <= normalRange;
        bool inAttackRange = inNormalRange && distanceToPlayer <= attackRange;

        if (inNormalRange && !wasInNormalRange)
        {
            PlayTransformAnimation();
        }

        if (!inNormalRange && wasInNormalRange)
        {
            PlayTransformBackAnimation();
        }

        if (!isAttacking && inAttackRange)
        {
            StartAttack();
        }

        animator.SetBool(IsAttackingBool, isAttacking);
        wasInNormalRange = inNormalRange;
    }

    void PlayTransformAnimation()
    {
        if (transformBackRoutine != null)
        {
            StopCoroutine(transformBackRoutine);
            transformBackRoutine = null;
        }

        if (transformRoutine != null)
        {
            StopCoroutine(transformRoutine);
        }

        transformRoutine = StartCoroutine(TransformAnimationRoutine());
    }

    void PlayTransformBackAnimation()
    {
        if (transformRoutine != null)
        {
            StopCoroutine(transformRoutine);
            transformRoutine = null;
        }

        if (transformBackRoutine != null)
        {
            StopCoroutine(transformBackRoutine);
        }

        transformBackRoutine = StartCoroutine(TransformBackAnimationRoutine());
    }

    System.Collections.IEnumerator TransformAnimationRoutine()
    {
        animator.SetBool(TransformBool, true);
        animator.SetBool(TransformBackBool, false);

        yield return new WaitForSeconds(transformDuration);

        animator.SetBool(TransformBool, false);
        transformRoutine = null;
    }

    System.Collections.IEnumerator TransformBackAnimationRoutine()
    {
        animator.SetBool(TransformBool, false);
        animator.SetBool(TransformBackBool, true);

        yield return new WaitForSeconds(transformBackDuration);

        animator.SetBool(TransformBackBool, false);
        transformBackRoutine = null;
    }

    void StartAttack()
    {
        if (attackRoutine != null)
        {
            return;
        }

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        isAttacking = true;
        animator.SetBool(IsAttackingBool, true);
        attackRoutine = StartCoroutine(AttackAndKillPlayer());
    }

    System.Collections.IEnumerator AttackAndKillPlayer()
    {
        yield return new WaitForSeconds(attackDuration);

        if (player == null)
        {
            isAttacking = false;
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            attackRoutine = null;
            yield break;
        }

        playerKilled = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
