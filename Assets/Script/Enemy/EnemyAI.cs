using UnityEngine;

// Patrols between two points. If the player enters detection range, walks
// toward them until in attack range, then stops and lets EnemyAttack take
// over. Freezes movement while EnemyAttack reports an attack or block in
// progress, same pattern as the player's IsActionLocked.
//
// Animator parameters used (from the Animator Controller):
//   AnimState (int: 0 idle / 1 walk), isGrounded (bool)
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float moveSpeed = 2f;
    [Tooltip("How close to a patrol point counts as 'arrived'.")]
    [SerializeField] private float waypointTolerance = 0.1f;
    [Tooltip("How long the enemy waits at each patrol point before turning around.")]
    [SerializeField] private float waitAtPointDuration = 1f;

    [Header("Player Detection")]
    [Tooltip("If false, this enemy only ever patrols and never notices/chases the player.")]
    [SerializeField] private bool detectsPlayer = true;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Ground Check")]
    [Tooltip("Optional - a child transform at the enemy's feet. If left empty, isGrounded always reports true.")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;
    [SerializeField] private EnemyAttack enemyAttack;
    [SerializeField] private EnemyHealth enemyHealth;

    private static readonly int AnimStateParam = Animator.StringToHash("AnimState");
    private static readonly int IsGroundedParam = Animator.StringToHash("isGrounded");

    private Transform currentPatrolTarget;
    private Transform detectedPlayer;
    private float waitTimer;
    private bool facingRight = true;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (enemyAttack == null) enemyAttack = GetComponent<EnemyAttack>();
        if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();

        currentPatrolTarget = pointB != null ? pointB : pointA;
    }

    private void Update()
    {
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            Stop();
            return;
        }

        UpdateGroundedParam();

        if (enemyAttack != null && enemyAttack.IsDashing)
        {
            // EnemyAttack is fully driving the Rigidbody2D for the dash -
            // don't touch velocity here, just keep the run animation going.
            anim.SetInteger(AnimStateParam, 1);
            return;
        }

        bool locked = (enemyAttack != null && (enemyAttack.IsAttacking || enemyAttack.IsBlocking))
            || (enemyHealth != null && enemyHealth.IsStaggered);
        if (locked)
        {
            Stop();
            return;
        }

        DetectPlayer();

        if (detectedPlayer != null)
        {
            bool inAttackRange = enemyAttack != null && enemyAttack.IsPlayerInAttackRange(detectedPlayer);

            if (inAttackRange)
            {
                // Close enough - stand still, face the player, let
                // EnemyAttack drive the actual attack.
                Stop();
                FaceToward(detectedPlayer.position);
                return;
            }

            if (enemyAttack != null && enemyAttack.TryStartDashAttack(detectedPlayer))
            {
                // Dash just started this frame - EnemyAttack takes over
                // movement from here (see the IsDashing check above).
                FaceToward(detectedPlayer.position);
                return;
            }

            MoveToward(detectedPlayer.position);
            return;
        }

        Patrol();
    }

    private void Stop()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        anim.SetInteger(AnimStateParam, 0);
    }

    private void UpdateGroundedParam()
    {
        bool grounded = groundCheck == null
            || Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        anim.SetBool(IsGroundedParam, grounded);
    }

    private void DetectPlayer()
    {
        detectedPlayer = null;

        if (!detectsPlayer)
        {
            return;
        }

        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        if (hit != null)
        {
            detectedPlayer = hit.transform;
        }
    }

    private void Patrol()
    {
        if (pointA == null || pointB == null)
        {
            Stop();
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            Stop();
            return;
        }

        float distanceToTarget = Mathf.Abs(transform.position.x - currentPatrolTarget.position.x);

        if (distanceToTarget <= waypointTolerance)
        {
            waitTimer = waitAtPointDuration;
            currentPatrolTarget = currentPatrolTarget == pointA ? pointB : pointA;
            Stop();
            return;
        }

        MoveToward(currentPatrolTarget.position);
    }

    private void MoveToward(Vector3 targetPosition)
    {
        float direction = Mathf.Sign(targetPosition.x - transform.position.x);
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        anim.SetInteger(AnimStateParam, 1);
        FaceDirection(direction);
    }

    private void FaceToward(Vector3 targetPosition)
    {
        FaceDirection(Mathf.Sign(targetPosition.x - transform.position.x));
    }

    private void FaceDirection(float direction)
    {
        if (Mathf.Abs(direction) < 0.01f)
        {
            return;
        }

        bool shouldFaceRight = direction > 0f;
        if (shouldFaceRight == facingRight)
        {
            return;
        }

        facingRight = shouldFaceRight;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        if (detectsPlayer)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
    }
}