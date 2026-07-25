using System.Collections.Generic;
using UnityEngine;

// Pairs a specific player character with the lift point it should be
// carried at - lets one flying enemy correctly grab whichever of two
// swappable player characters is currently active in the scene.
[System.Serializable]
public class PlayerLiftPoint
{
    public NewPlayerController player;
    public Transform liftPoint;
}

// Flying enemy with no attack of any kind - the only thing it does is grab
// the player, lift them, carry them for a few seconds, and release. No
// windup animation, no attack trigger, no separate strike: closing in
// (optionally via a dash) and overlapping grabPoint immediately starts the
// carry.
//
// Uses no Animator attack parameters at all - only whatever Idle/Fly
// animation FlyingEnemyAI already drives via AnimState.
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class FlyingEnemyAttack : MonoBehaviour
{
    [Header("Grab")]
    [Tooltip("Range at which the enemy can attempt a grab (close range, e.g. right after a dash or walk-up).")]
    [SerializeField] private float grabRange = 1.5f;
    [SerializeField] private float grabCooldown = 5f;
    [Tooltip("Damage dealt on a successful grab - kept small on purpose, since the real threat is being carried off, not the hit itself.")]
    [SerializeField] private int grabDamage = 3;

    [Header("Grab Detection Point")]
    [Tooltip("Where the grab connects - place a child empty GameObject at the claws/talons. The player must be overlapping this for the grab to succeed.")]
    [SerializeField] private Transform grabPoint;
    [SerializeField] private float grabCheckRadius = 0.6f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Carry")]
    [Tooltip("Maps each specific player character to its own lift point (e.g. different characters need the carry position offset differently for their sprite/size). Only the entry matching whichever player actually gets grabbed is used - handles switching between two playable characters automatically.")]
    [SerializeField] private List<PlayerLiftPoint> liftPointsByPlayer = new List<PlayerLiftPoint>();
    [Tooltip("Used if the grabbed player isn't found in Lift Points By Player above - a safe fallback so this still works even with only one character set up.")]
    [SerializeField] private Transform defaultLiftPoint;
    [SerializeField] private float liftDuration = 2f;
    [Tooltip("How high above the grab point the enemy rises while carrying the player, for a bit of drama before the drop.")]
    [SerializeField] private float liftRiseHeight = 2f;
    [SerializeField] private float liftRiseSpeed = 3f;

    [Header("Dash (closing the distance to grab)")]
    [Tooltip("If false, the enemy only grabs once already within grabRange (relies on FlyingEnemyAI to walk it in) and never dashes.")]
    [SerializeField] private bool canDash = true;
    [Tooltip("Dash triggers when the player is farther than grabRange but within this range.")]
    [SerializeField] private float dashRange = 5f;
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashRecoveryDuration = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip grabClip;
    [SerializeField] private AudioClip liftDropClip;

    [Header("References")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Rigidbody2D rb;

    private float grabCooldownTimer;

    private float dashCooldownTimer;
    private float dashTimer;
    private float dashRecoveryTimer;
    private Vector2 dashDirection;

    private float liftTimer;
    private Vector3 liftRiseTarget;
    private HeroKnightPlayerController liftedPlayerController;
    private Transform currentLiftPoint;

    /// <summary>True while this enemy is mid-block (delegates to EnemyHealth) - kept for API parity with FlyingEnemyAI's lock checks.</summary>
    public bool IsBlocking => enemyHealth != null && enemyHealth.IsBlocking;

    /// <summary>True while dashing in (or in the brief recovery right after).</summary>
    public bool IsDashing => dashTimer > 0f || dashRecoveryTimer > 0f;

    /// <summary>True while actively carrying a grabbed player.</summary>
    public bool IsLifting => liftTimer > 0f;

    private void Awake()
    {
        if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
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

        if (grabCooldownTimer > 0f)
        {
            grabCooldownTimer -= Time.deltaTime;
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
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
    }

    /// <summary>
    /// Called by FlyingEnemyAI every frame it's tracking the player. Returns
    /// whether the player is within grabRange, and - if off cooldown and
    /// able to act - attempts a grab immediately as a side effect.
    /// </summary>
    public bool IsPlayerInAttackRange(Transform player)
    {
        if (player == null)
        {
            return false;
        }

        bool inRange = Vector2.Distance(transform.position, player.position) <= grabRange;
        bool canAct = !IsBlocking && !IsLifting && !IsDashing
            && (enemyHealth == null || !enemyHealth.IsStaggered);

        if (inRange && grabCooldownTimer <= 0f && canAct)
        {
            AttemptGrab();
        }

        return inRange;
    }

    /// <summary>
    /// Called by FlyingEnemyAI when the player is beyond grabRange but
    /// within dashRange. Dashes straight at the player (any 2D direction)
    /// and attempts a grab as soon as it arrives. Returns whether a dash was
    /// actually started.
    /// </summary>
    public bool TryStartDashAttack(Transform player)
    {
        if (!canDash || player == null || IsDashing || IsBlocking || IsLifting)
        {
            return false;
        }

        if (enemyHealth != null && (enemyHealth.IsDead || enemyHealth.IsStaggered))
        {
            return false;
        }

        if (dashCooldownTimer > 0f || grabCooldownTimer > 0f)
        {
            return false;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > dashRange || distance <= grabRange)
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
        dashCooldownTimer = dashCooldown;
    }

    private void EndDash()
    {
        rb.linearVelocity = Vector2.zero;
        dashRecoveryTimer = dashRecoveryDuration;
        AttemptGrab();
    }

    private void AttemptGrab()
    {
        grabCooldownTimer = grabCooldown;
        PlaySound(grabClip);
        CheckGrab();
    }

    /// <summary>
    /// Checks whether the player is overlapping grabPoint and, if so, starts
    /// carrying them immediately - no windup, no delay.
    /// </summary>
    private void CheckGrab()
    {
        if (grabPoint == null)
        {
            return;
        }

        Collider2D hit = Physics2D.OverlapCircle(grabPoint.position, grabCheckRadius, playerLayer);

        if (hit != null && hit.TryGetComponent(out HeroKnightPlayerController playerController))
        {
            hit.TryGetComponent(out PlayerHealth playerHealth);
            StartLift(playerController, playerHealth);
        }
    }

    private void StartLift(HeroKnightPlayerController playerController, PlayerHealth playerHealth)
    {
        liftedPlayerController = playerController;
        currentLiftPoint = GetLiftPointFor(playerController);
        liftTimer = liftDuration;
        liftRiseTarget = transform.position + Vector3.up * liftRiseHeight;

        playerController.SetExternallyControlled(true);

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(grabDamage, enemyHealth);
        }
    }

    /// <summary>
    /// Looks up the lift point that matches the specific player character
    /// that got grabbed, so switching between two playable characters still
    /// carries whichever one is currently active at the right spot. Falls
    /// back to defaultLiftPoint if no matching entry is set up.
    /// </summary>
    private Transform GetLiftPointFor(HeroKnightPlayerController playerController)
    {
        foreach (PlayerLiftPoint mapping in liftPointsByPlayer)
        {
            if (mapping.player == playerController && mapping.liftPoint != null)
            {
                return mapping.liftPoint;
            }
        }

        return defaultLiftPoint;
    }

    private void UpdateLift()
    {
        liftTimer -= Time.deltaTime;

        // Rise a bit while carrying, for drama, then hold at the peak.
        transform.position = Vector3.MoveTowards(transform.position, liftRiseTarget, liftRiseSpeed * Time.deltaTime);

        if (currentLiftPoint != null && liftedPlayerController != null)
        {
            liftedPlayerController.transform.position = currentLiftPoint.position;
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

        currentLiftPoint = null;
        liftTimer = 0f;
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
        Gizmos.DrawWireSphere(transform.position, grabRange);

        if (canDash)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, dashRange);
        }

        if (grabPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(grabPoint.position, grabCheckRadius);
        }

        if (defaultLiftPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(defaultLiftPoint.position, 0.2f);
        }

        Gizmos.color = Color.cyan;
        foreach (PlayerLiftPoint mapping in liftPointsByPlayer)
        {
            if (mapping.liftPoint != null)
            {
                Gizmos.DrawWireSphere(mapping.liftPoint.position, 0.2f);
            }
        }
    }
}