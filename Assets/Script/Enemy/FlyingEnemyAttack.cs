using System.Collections;
using UnityEngine;

// Flying-enemy version of EnemyAttack: same randomized Attack1/Attack2 and
// dash gap-closer as the ground EnemyAttack, but the dash moves in full 2D
// (diagonals included, since it flies). Also supports an optional grab: when
// canLiftPlayer is true, an attack opportunity can become a lift-and-carry
// instead of a normal hit - the player is attached to liftPoint and carried
// for liftDuration seconds, then dropped. Set canLiftPlayer/canDashAttack to
// false to reuse this script on flying enemies that shouldn't grab/dash.
//
// Animator parameters used: Attack1, Attack2 (triggers)
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class FlyingEnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    [Tooltip("How long an attack locks movement for, in seconds. Roughly match your attack animation length.")]
    [SerializeField] private float attackMoveLockDuration = 0.4f;
    [SerializeField] private int damage = 10;
    [Tooltip("If true, the same attack animation is never played twice in a row.")]
    [SerializeField] private bool avoidRepeatAttack = true;

    [Header("Dash Attack")]
    [Tooltip("If false, this enemy can never dash attack, regardless of the settings below.")]
    [SerializeField] private bool canDashAttack = false;
    [SerializeField] private float dashAttackRange = 5f;
    [Range(0f, 1f)]
    [SerializeField] private float dashAttackChance = 0.4f;
    [SerializeField] private float dashAttackCooldown = 3f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashRecoveryDuration = 0.15f;

    [Header("Lift")]
    [Tooltip("If false, this enemy never lifts the player, regardless of the settings below. Set false to reuse this script on flying enemies that shouldn't grab.")]
    [SerializeField] private bool canLiftPlayer = false;
    [Range(0f, 1f)]
    [Tooltip("Chance (0-1), rolled instead of a normal attack when in range, that this attack becomes a lift-and-carry instead of a regular hit.")]
    [SerializeField] private float liftChance = 0.25f;
    [SerializeField] private float liftCooldown = 6f;
    [Tooltip("Damage dealt on the initial grab.")]
    [SerializeField] private int liftGrabDamage = 5;
    [Tooltip("Optional - the exact spot the grab reaches from (e.g. the claws). If set, the grab only actually connects when the player is within Grab Check Radius of this point, and the player is snapped here at the moment of the grab. Leave empty to just use Attack Range/chance like before, with no extra precision check.")]
    [SerializeField] private Transform grabPoint;
    [SerializeField] private float grabCheckRadius = 0.6f;
    [Tooltip("Where the player is carried while lifted. If left empty, Grab Point above is used for both the grab and the carry - handy if you just want one point for the whole thing.")]
    [SerializeField] private Transform liftPoint;
    [SerializeField] private float liftDuration = 2f;
    [Tooltip("How high above the grab point the enemy rises while carrying the player, for a bit of drama before the drop.")]
    [SerializeField] private float liftRiseHeight = 2f;
    [SerializeField] private float liftRiseSpeed = 3f;

    [Header("Hit Detection")]
    [Tooltip("Where the hit check is centered - place a child empty GameObject at the claw/beak tip.")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float hitCheckRadius = 0.6f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float hitCheckDelay = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] attackClips;
    [SerializeField] private AudioClip liftGrabClip;
    [SerializeField] private AudioClip liftDropClip;

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
    private Vector2 dashDirection;

    private float liftCooldownTimer;
    private float liftTimer;
    private Vector3 liftRiseTarget;
    private HeroKnightPlayerController liftedPlayerController;
    private bool liftPointMisconfigured;

    /// <summary>True while an attack's move-lock window is active.</summary>
    public bool IsAttacking => attackLockTimer > 0f;

    /// <summary>True while this enemy is mid-block (delegates to EnemyHealth).</summary>
    public bool IsBlocking => enemyHealth != null && enemyHealth.IsBlocking;

    /// <summary>True while dashing in (or in the brief recovery right after).</summary>
    public bool IsDashing => dashTimer > 0f || dashRecoveryTimer > 0f;

    /// <summary>True while actively carrying a lifted player.</summary>
    public bool IsLifting => liftTimer > 0f;

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        attackTriggers = new[] { Attack1Trigger, Attack2Trigger };
    }

    private void OnDisable()
    {
        // Safety net: if this gets disabled mid-lift (e.g. the enemy died
        // while carrying the player), don't leave the player stuck frozen.
        ReleaseLiftedPlayer();
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

        if (liftCooldownTimer > 0f)
        {
            liftCooldownTimer -= Time.deltaTime;
        }

        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            rb.linearVelocity = dashDirection * dashSpeed;

            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }
        else if (dashRecoveryTimer > 0f)
        {
            dashRecoveryTimer -= Time.deltaTime;
            rb.linearVelocity = Vector2.zero;
        }

        if (liftTimer > 0f)
        {
            UpdateLift();
        }

        // Safety net: clear a leftover trigger once our lock window ends, in
        // case the Animator Controller is missing a return-to-Idle transition.
        if (wasAttacking && !IsAttacking)
        {
            anim.ResetTrigger(Attack1Trigger);
            anim.ResetTrigger(Attack2Trigger);
        }

        wasAttacking = IsAttacking;
    }

    /// <summary>
    /// Called by FlyingEnemyAI every frame it's tracking the player. Returns
    /// whether the player is within attackRange, and - if off cooldown and
    /// able to act - either starts a lift (if canLiftPlayer rolls it) or a
    /// normal attack, as a side effect.
    /// </summary>
    public bool IsPlayerInAttackRange(Transform player)
    {
        if (player == null)
        {
            return false;
        }

        bool inRange = Vector2.Distance(transform.position, player.position) <= attackRange;
        bool canAct = !IsAttacking && !IsBlocking && !IsLifting
            && (enemyHealth == null || !enemyHealth.IsStaggered);

        if (inRange && attackCooldownTimer <= 0f && canAct)
        {
            if (!TryStartLift(player))
            {
                PlayRandomAttack();
            }
        }

        return inRange;
    }

    /// <summary>
    /// Called by FlyingEnemyAI when the player is beyond attackRange but
    /// within dashAttackRange. Rolls dashAttackChance and, if it hits, dashes
    /// straight at the player (any 2D direction, not just left/right) ending
    /// in a strike. Returns whether a dash was actually started.
    /// </summary>
    public bool TryStartDashAttack(Transform player)
    {
        if (!canDashAttack || player == null || IsDashing || IsAttacking || IsBlocking || IsLifting)
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
        dashDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
        dashTimer = dashDuration;
        dashAttackCooldownTimer = dashAttackCooldown;
    }

    private void EndDash()
    {
        rb.linearVelocity = Vector2.zero;
        dashRecoveryTimer = dashRecoveryDuration;

        int index = Random.Range(0, attackTriggers.Length);
        lastAttackIndex = index;
        attackLockTimer = attackMoveLockDuration;

        anim.SetTrigger(attackTriggers[index]);
        PlayRandomClip(attackClips);

        StartCoroutine(CheckAttackHitAfterDelay(hitCheckDelay));
    }

    private bool TryStartLift(Transform player)
    {
        if (!canLiftPlayer || liftCooldownTimer > 0f)
        {
            return false;
        }

        if (Random.value > liftChance)
        {
            return false;
        }

        if (!player.TryGetComponent(out HeroKnightPlayerController playerController))
        {
            return false;
        }

        // Optional extra precision: if Grab Point is set, the grab only
        // actually connects when the player is within reach of that exact
        // spot (e.g. the claws), not just anywhere within the enemy's
        // general Attack Range. Leave Grab Point empty to skip this check
        // and keep the old range+chance-only behavior.
        if (grabPoint != null)
        {
            Collider2D hit = Physics2D.OverlapCircle(grabPoint.position, grabCheckRadius, playerLayer);
            if (hit == null || hit.transform != player)
            {
                return false;
            }
        }

        player.TryGetComponent(out PlayerHealth playerHealth);
        StartLift(playerController, playerHealth);
        return true;
    }

    private void StartLift(HeroKnightPlayerController playerController, PlayerHealth playerHealth)
    {
        liftedPlayerController = playerController;
        liftTimer = liftDuration;
        liftCooldownTimer = liftCooldown;
        liftRiseTarget = transform.position + Vector3.up * liftRiseHeight;

        // Lift Point (or Grab Point, if that's what ends up being used for
        // the carry) MUST be a child of this enemy, not of the player. If
        // it's parented under the player instead, its world position would
        // be recalculated from the player's own (just-moved) position every
        // frame, compounding a small offset into a runaway explosion within
        // a few frames. Guard against that instead of silently launching
        // the player hundreds of units away.
        Transform carryPoint = liftPoint != null ? liftPoint : grabPoint;
        liftPointMisconfigured = carryPoint != null && carryPoint.IsChildOf(playerController.transform);
        if (liftPointMisconfigured)
        {
            Debug.LogWarning(
                $"{name}: the carry point (Lift Point or Grab Point) is parented under the player being grabbed, which causes runaway position drift each frame. " +
                "Parent it under this enemy instead (e.g. below its talons). Falling back to the enemy's own position for this grab.",
                this);
        }
        else if (grabPoint != null)
        {
            // Snap the player into the claws right at the moment of the
            // grab - if Lift Point differs from Grab Point, the carry then
            // transitions there over the following frames as the enemy rises.
            playerController.transform.position = grabPoint.position;
        }

        playerController.SetExternallyControlled(true);

        // Note: this always grabs even if the player was mid-parry - a
        // parry here only cancels via TakeDamage's own logic (which still
        // just negates the grab damage, not the lift itself). If you want a
        // perfectly-timed parry to cancel the grab entirely, check
        // playerAttack.IsParryWindowOpen here before committing to the lift.
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(liftGrabDamage, enemyHealth);
        }

        int index = Random.Range(0, attackTriggers.Length);
        anim.SetTrigger(attackTriggers[index]);
        PlaySound(liftGrabClip);
    }

    private void UpdateLift()
    {
        liftTimer -= Time.deltaTime;

        // Rise a bit while carrying, for drama, then hold at the peak.
        transform.position = Vector3.MoveTowards(transform.position, liftRiseTarget, liftRiseSpeed * Time.deltaTime);

        if (liftedPlayerController != null)
        {
            Transform carryPoint = liftPoint != null ? liftPoint : grabPoint;

            // Fall back to snapping the player to the enemy's own position
            // if there's no usable carry point, or it's misconfigured (see
            // StartLift), rather than reading a position that could
            // compound into a runaway launch.
            Vector3 carryPosition = (carryPoint != null && !liftPointMisconfigured)
                ? carryPoint.position
                : transform.position;

            liftedPlayerController.transform.position = carryPosition;
        }

        if (liftTimer <= 0f)
        {
            EndLift();
        }
    }

    private void EndLift()
    {
        PlaySound(liftDropClip);
        ReleaseLiftedPlayer();
    }

    private void ReleaseLiftedPlayer()
    {
        if (liftedPlayerController != null)
        {
            liftedPlayerController.SetExternallyControlled(false);
            liftedPlayerController = null;
        }

        liftPointMisconfigured = false;
        liftTimer = 0f;
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

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
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

        if (grabPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(grabPoint.position, grabCheckRadius);
        }

        if (liftPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(liftPoint.position, 0.2f);
        }
    }
}