using System.Collections;
using UnityEngine;

// Attacks the player when in range, randomizing between Attack1/Attack2 (add
// more triggers to attackTriggers + the Animator if you add Attack3, etc).
// Optionally dashes in as a gap-closer when the player is at mid-range,
// mixed in with the normal attacks at a random chance - set canDashAttack to
// false to reuse this script on enemies that shouldn't dash.
//
// Animator parameters used: Attack1, Attack2 (triggers)
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [Tooltip("How long an attack locks movement for, in seconds. Roughly match your attack animation length.")]
    [SerializeField] private float attackMoveLockDuration = 0.4f;
    [SerializeField] private int damage = 10;
    [Tooltip("If true, the same attack animation is never played twice in a row.")]
    [SerializeField] private bool avoidRepeatAttack = true;

    [Header("Dash Attack")]
    [Tooltip("If false, this enemy can never dash attack, regardless of the settings below. Set false to reuse this script on enemies that shouldn't dash.")]
    [SerializeField] private bool canDashAttack = false;
    [Tooltip("Dash attack can trigger when the player is farther than Attack Range but within this range - i.e. a mid-range gap closer.")]
    [SerializeField] private float dashAttackRange = 4f;
    [Range(0f, 1f)]
    [Tooltip("Chance (0-1), rolled once per approach, that the enemy dashes in instead of just walking - mixes it randomly with normal approach/attacks rather than dashing every time it's in range.")]
    [SerializeField] private float dashAttackChance = 0.4f;
    [SerializeField] private float dashAttackCooldown = 3f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.25f;
    [Tooltip("Extra move-lock time right after the dash finishes, before the strike/hit-check, so it reads as one lunge-and-hit action.")]
    [SerializeField] private float dashRecoveryDuration = 0.15f;

    [Header("Hit Detection")]
    [Tooltip("Where the hit check is centered - place a child empty GameObject at the weapon/claw tip.")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float hitCheckRadius = 0.6f;
    [SerializeField] private LayerMask playerLayer;
    [Tooltip("Delay after the attack trigger fires before checking for a hit - match the frame the swing connects.")]
    [SerializeField] private float hitCheckDelay = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] attackClips;

    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Rigidbody2D rb;

    private static readonly int Attack1Trigger = Animator.StringToHash("Attack1");
    private static readonly int Attack2Trigger = Animator.StringToHash("Attack2");

    private int[] attackTriggers;
    private int lastAttackIndex = -1;
    private float attackCooldownTimer;
    private float attackLockTimer;
    private bool wasAttacking;

    private float dashAttackCooldownTimer;
    private float dashTimer;
    private float dashRecoveryTimer;
    private int dashDirection;

    /// <summary>True while an attack's move-lock window is active.</summary>
    public bool IsAttacking => attackLockTimer > 0f;

    /// <summary>True while this enemy is mid-block (delegates to EnemyHealth).</summary>
    public bool IsBlocking => enemyHealth != null && enemyHealth.IsBlocking;

    /// <summary>True while dashing in (or in the brief recovery right after) - EnemyAttack owns movement during this, EnemyAI should stand down.</summary>
    public bool IsDashing => dashTimer > 0f || dashRecoveryTimer > 0f;

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        attackTriggers = new[] { Attack1Trigger, Attack2Trigger };
    }

    private void Update()
    {
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            return;
        }

        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (attackLockTimer > 0f)
        {
            attackLockTimer -= Time.deltaTime;
        }

        if (dashAttackCooldownTimer > 0f)
        {
            dashAttackCooldownTimer -= Time.deltaTime;
        }

        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y);

            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }
        else if (dashRecoveryTimer > 0f)
        {
            dashRecoveryTimer -= Time.deltaTime;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // Safety net: if a trigger somehow never got consumed by a
        // transition (e.g. Animator Controller missing a return-to-Idle
        // transition), clear it once our own lock window has elapsed so it
        // can't cause a stuck state on a later attack.
        if (wasAttacking && !IsAttacking)
        {
            anim.ResetTrigger(Attack1Trigger);
            anim.ResetTrigger(Attack2Trigger);
        }

        wasAttacking = IsAttacking;
    }

    /// <summary>
    /// Called by EnemyAI every frame it's tracking the player. Returns
    /// whether the player is within attackRange, and fires an attack (if off
    /// cooldown and not already attacking/blocking) as a side effect.
    /// </summary>
    public bool IsPlayerInAttackRange(Transform player)
    {
        if (player == null)
        {
            return false;
        }

        bool inRange = Vector2.Distance(transform.position, player.position) <= attackRange;

        bool canAct = !IsAttacking && !IsBlocking && (enemyHealth == null || !enemyHealth.IsStaggered);

        if (inRange && attackCooldownTimer <= 0f && canAct)
        {
            PlayRandomAttack();
        }

        return inRange;
    }

    /// <summary>
    /// Called by EnemyAI when the player is beyond attackRange but it's
    /// looking for something to do instead of just walking closer. Rolls
    /// dashAttackChance and, if it hits (and everything else checks out),
    /// starts a dash gap-closer that ends in a strike. Returns whether a
    /// dash was actually started - if false, EnemyAI should fall back to a
    /// normal walk approach.
    /// </summary>
    public bool TryStartDashAttack(Transform player)
    {
        if (!canDashAttack || player == null || IsDashing || IsAttacking || IsBlocking)
        {
            return false;
        }

        if (enemyHealth != null && (enemyHealth.IsDead || enemyHealth.IsStaggered))
        {
            return false;
        }

        if (dashAttackCooldownTimer > 0f)
        {
            return false;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > dashAttackRange || distance <= attackRange)
        {
            return false;
        }

        if (Random.value > dashAttackChance)
        {
            return false;
        }

        StartDash(player);
        return true;
    }

    private void StartDash(Transform player)
    {
        dashDirection = player.position.x >= transform.position.x ? 1 : -1;
        dashTimer = dashDuration;
        dashAttackCooldownTimer = dashAttackCooldown;
    }

    private void EndDash()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        dashRecoveryTimer = dashRecoveryDuration;

        // The dash ends in a strike - reuses the same Attack1/2 triggers and
        // hit-check as a normal attack, no dedicated "Dash" trigger needed.
        int index = Random.Range(0, attackTriggers.Length);
        lastAttackIndex = index;
        attackLockTimer = attackMoveLockDuration;

        anim.SetTrigger("DashAttack");
        PlayRandomClip(attackClips);

        StartCoroutine(CheckAttackHitAfterDelay(hitCheckDelay));
    }

    private void PlayRandomAttack()
    {
        int index = Random.Range(0, attackTriggers.Length);

        // Reroll onto a different index if we happened to repeat the last
        // attack, so it doesn't feel like it's always the same animation.
        if (avoidRepeatAttack && attackTriggers.Length > 1 && index == lastAttackIndex)
        {
            index = (index + 1) % attackTriggers.Length;
        }

        lastAttackIndex = index;
        attackCooldownTimer = attackCooldown;
        attackLockTimer = attackMoveLockDuration;

        anim.SetTrigger(attackTriggers[index]);
        PlayRandomClip(attackClips);

        StartCoroutine(CheckAttackHitAfterDelay(hitCheckDelay));
    }

    private IEnumerator CheckAttackHitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CheckAttackHit();
    }

    /// <summary>
    /// Checks whether the swing connected with the player and deals damage
    /// if so. Public so it can also be called from an Animation Event on the
    /// swing frame for precise timing instead of relying on hitCheckDelay.
    /// </summary>
    public void CheckAttackHit()
    {
        if (attackPoint == null)
        {
            return;
        }

        Collider2D hit = Physics2D.OverlapCircle(attackPoint.position, hitCheckRadius, playerLayer);

        if (hit != null && hit.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(damage, enemyHealth);
        }
    }

    private void PlayRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (canDashAttack)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, dashAttackRange);
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, hitCheckRadius);
        }
    }
}