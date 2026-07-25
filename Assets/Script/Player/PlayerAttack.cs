using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// NOTE: Requires the "Input System" package (com.unity.inputsystem), same as
// NewPlayerController.cs.
//
// Handles the player's attack combo and block. Kept as its own component so
// combat can be tuned/replaced without touching movement code.
//
// Animator parameters used (match HeroKnight.cs):
//   Attack1 / Attack2 / Attack3 (triggers) - left mouse button, randomized
//   Block (trigger)                         - right mouse button
[RequireComponent(typeof(Animator))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    [Tooltip("Minimum time between attacks, in seconds.")]
    [SerializeField] private float attackCooldown = 0.25f;
    [Tooltip("How long an attack locks movement for, in seconds. Roughly match your attack animation length.")]
    [SerializeField] private float attackMoveLockDuration = 0.35f;
    [Tooltip("If true, the same attack animation is never played twice in a row.")]
    [SerializeField] private bool avoidRepeatAttack = true;
    [SerializeField] private int damage = 15;
    [Tooltip("Posture damage dealt to an enemy's parry bar on a normal landed hit (separate from - and smaller than - a perfect parry's posture damage).")]
    [SerializeField] private float attackPostureDamage = 8f;

    [Header("Attack Hit Detection")]
    [Tooltip("Where the hit check is centered - place a child empty GameObject roughly at the weapon/fist tip and drag it here.")]
    [SerializeField] private Transform attackPoint;
    [Tooltip("Radius of the hit check circle around attackPoint.")]
    [SerializeField] private float attackRange = 0.75f;
    [Tooltip("Only colliders on these layers count as a hit.")]
    [SerializeField] private LayerMask enemyLayers;
    [Tooltip("Delay after the attack trigger fires before checking for a hit - roughly match the frame your weapon actually swings. For frame-perfect timing instead, set this to 0 and call CheckAttackHit() from an Animation Event on the swing frame.")]
    [SerializeField] private float hitCheckDelay = 0.15f;

    [Header("Block Dust")]
    [SerializeField] private GameObject blockDustPrefab;
    [Tooltip("Optional - where the dust spawns. If left empty, it spawns at a fixed offset in front of the player.")]
    [SerializeField] private Transform blockDustSpawnPoint;
    [Tooltip("Horizontal offset from the player used when no spawn point is assigned.")]
    [SerializeField] private float blockDustOffset = 0.5f;

    [Header("Parry Timing")]
    [Tooltip("Window after pressing Block (right-click) during which a hit counts as a perfect parry instead of a normal block. Sekiro-style: press right as the hit lands, don't just hold it.")]
    [SerializeField] private float parryWindowDuration = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Played when an attack connects with something on Enemy Layers.")]
    [SerializeField] private AudioClip[] attackHitClips;
    [Tooltip("Played when an attack swings and hits nothing.")]
    [SerializeField] private AudioClip[] attackMissClips;
    [SerializeField] private AudioClip blockClip;

    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private HeroKnightPlayerController playerController;
    [Tooltip("Optional - auto-found via GetComponent. Used to refuse blocking while guard-broken.")]
    [SerializeField] private PlayerHealth playerHealth;

    private static readonly int Attack1Trigger = Animator.StringToHash("Attack1");
    private static readonly int Attack2Trigger = Animator.StringToHash("Attack2");
    private static readonly int Attack3Trigger = Animator.StringToHash("Attack3");
    private static readonly int BlockTrigger = Animator.StringToHash("Block");

    private int[] attackTriggers;
    private int lastAttackIndex = -1;
    private float attackCooldownTimer;
    private float attackLockTimer;
    private bool isBlocking;
    private float blockStartTime;

    private InputAction attackAction;
    private InputAction blockAction;

    /// <summary>True while an attack's move-lock window is active.</summary>
    public bool IsAttacking => attackLockTimer > 0f;

    /// <summary>True while the right mouse button is held down.</summary>
    public bool IsBlocking => isBlocking;

    /// <summary>
    /// True only during the brief window right after Block was pressed.
    /// A hit landing while this is true is a perfect parry; a hit landing
    /// while IsBlocking is true but this is false is just a normal block.
    /// </summary>
    public bool IsParryWindowOpen => isBlocking && (Time.time - blockStartTime) <= parryWindowDuration;

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (playerController == null) playerController = GetComponent<HeroKnightPlayerController>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        attackTriggers = new[] { Attack1Trigger, Attack2Trigger, Attack3Trigger };

        attackAction = new InputAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
        blockAction = new InputAction("Block", InputActionType.Button, "<Mouse>/rightButton");
    }

    private void OnEnable()
    {
        attackAction.Enable();
        blockAction.Enable();
    }

    private void OnDisable()
    {
        attackAction.Disable();
        blockAction.Disable();

        // Don't leave the player stuck in a blocked state if this component
        // gets disabled (e.g. on death) mid-block.
        isBlocking = false;
        attackLockTimer = 0f;
    }

    private void OnDestroy()
    {
        attackAction.Dispose();
        blockAction.Dispose();
    }

    private void Update()
    {
        if (IsDead() || IsExternallyControlled())
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

        if (blockAction.WasPressedThisFrame() && !IsGuardBroken())
        {
            StartBlock();
        }
        else if (blockAction.WasReleasedThisFrame())
        {
            EndBlock();
        }

        // Can't attack while blocking (matches HeroKnight's rule).
        if (attackAction.WasPressedThisFrame() && attackCooldownTimer <= 0f && !isBlocking)
        {
            EnemyHealth staggeredEnemy = FindStaggeredEnemyInRange();

            if (staggeredEnemy != null)
            {
                PerformExecute(staggeredEnemy);
            }
            else
            {
                PlayRandomAttack();
            }
        }
    }

    /// <summary>
    /// Looks for a staggered (posture-broken) enemy within attack range so a
    /// normal attack press turns into an instant execute instead.
    /// </summary>
    private EnemyHealth FindStaggeredEnemyInRange()
    {
        Vector3 checkOrigin = attackPoint != null ? attackPoint.position : transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkOrigin, attackRange, enemyLayers);

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out EnemyHealth candidate) && candidate.IsStaggered)
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Instantly kills a staggered enemy. Reuses the first attack trigger for
    /// the swing visual - swap this for a dedicated "Execute" trigger/state
    /// once you've added one to the Animator Controller.
    /// </summary>
    private void PerformExecute(EnemyHealth target)
    {
        attackCooldownTimer = attackCooldown;
        attackLockTimer = attackMoveLockDuration;
        lastAttackIndex = -1;

        anim.SetTrigger(attackTriggers[0]);
        PlayRandomClip(attackHitClips);

        target.Execute();
    }

    private bool IsDead()
    {
        return playerController != null && playerController.IsDead;
    }

    private bool IsExternallyControlled()
    {
        return playerController != null && playerController.IsExternallyControlled;
    }

    private bool IsGuardBroken()
    {
        return playerHealth != null && playerHealth.IsGuardBroken;
    }

    /// <summary>
    /// Called by PlayerHealth when the player gets guard-broken, so an
    /// in-progress block gets cut short immediately.
    /// </summary>
    public void ForceStopBlocking()
    {
        if (!isBlocking)
        {
            return;
        }

        isBlocking = false;
    }

    private void PlayRandomAttack()
    {
        int index = Random.Range(0, attackTriggers.Length);

        // Reroll onto a different index if we happened to repeat the last
        // attack, so the combo feels varied rather than truly random (which
        // can produce the same animation two or three times in a row).
        if (avoidRepeatAttack && attackTriggers.Length > 1 && index == lastAttackIndex)
        {
            index = (index + 1) % attackTriggers.Length;
        }

        lastAttackIndex = index;
        attackCooldownTimer = attackCooldown;
        attackLockTimer = attackMoveLockDuration;

        anim.SetTrigger(attackTriggers[index]);

        if (hitCheckDelay <= 0f)
        {
            CheckAttackHit();
        }
        else
        {
            StartCoroutine(CheckAttackHitAfterDelay(hitCheckDelay));
        }
    }

    private IEnumerator CheckAttackHitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CheckAttackHit();
    }

    /// <summary>
    /// Checks whether the attack connected with anything on Enemy Layers,
    /// deals damage to each EnemyHealth found, and plays the matching
    /// hit/miss sound. Public so it can also be called directly from an
    /// Animation Event (e.g. name it "CheckAttackHit" on the swing frame of
    /// your attack clips) instead of relying on hitCheckDelay for timing.
    /// </summary>
    public void CheckAttackHit()
    {
        if (attackPoint == null)
        {
            PlayRandomClip(attackMissClips);
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        bool connected = false;

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out EnemyHealth enemyHealth))
            {
                enemyHealth.TakeDamage(damage);
                enemyHealth.AddPosture(attackPostureDamage, false);
                connected = true;
            }
        }

        PlayRandomClip(connected ? attackHitClips : attackMissClips);
    }

    private void StartBlock()
    {
        isBlocking = true;
        blockStartTime = Time.time;
        anim.SetTrigger(BlockTrigger);
        SpawnBlockDust();
        PlaySound(blockClip);
    }

    private void EndBlock()
    {
        isBlocking = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlayRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || audioSource == null)
        {
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        PlaySound(clip);
    }

    private void SpawnBlockDust()
    {
        if (blockDustPrefab == null)
        {
            return;
        }

        int facing = playerController != null ? playerController.FacingDirection : 1;

        Vector3 spawnPosition = blockDustSpawnPoint != null
            ? blockDustSpawnPoint.position
            : transform.position + new Vector3(blockDustOffset * facing, 0f, 0f);

        GameObject dust = Instantiate(blockDustPrefab, spawnPosition, transform.localRotation);

        // Flip the dust to face the same way the player does, same trick as
        // HeroKnight's AE_SlideDust.
        dust.transform.localScale = new Vector3(facing, 1f, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}