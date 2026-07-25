using UnityEngine;
using UnityEngine.Events;

// Tracks the player's health. Plays the Hurt animation whenever the player
// takes damage and survives, and hands off to NewPlayerController.Die() (which
// already owns the Death trigger, death sound, blood effect, death canvas and
// scene reload) once health reaches zero, so that logic only lives in one
// place.
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(HeroKnightPlayerController))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [Tooltip("Brief window after taking damage during which further damage is ignored.")]
    [SerializeField] private float invulnerabilityDuration = 0.5f;

    [Header("Blocking")]
    [Range(0f, 1f)]
    [Tooltip("Fraction of incoming damage still taken while blocking outside the parry window. Kept above 0 on purpose - a mistimed block should still be worse than a perfect parry, or there's no reason to ever time it.")]
    [SerializeField] private float blockDamageMultiplier = 0.2f;
    [Tooltip("How much posture damage a successful parry deals to the attacking enemy - see EnemyHealth's Posture bar.")]
    [SerializeField] private float parryPostureDamage = 40f;

    [Header("Guard (Player's Own Posture)")]
    [Tooltip("Blocking outside the parry window costs the player's own guard posture. If it fills, the player is guard-broken: briefly can't block, and takes bonus damage. This is what makes timing a parry actually matter instead of just holding block forever.")]
    [SerializeField] private float maxGuardPosture = 100f;
    [SerializeField] private float blockGuardCost = 30f;
    [SerializeField] private float guardRegenPerSecond = 12f;
    [Tooltip("How long after the last blocked hit before guard posture starts draining back down.")]
    [SerializeField] private float guardRegenDelay = 2.5f;
    [SerializeField] private float guardBreakDuration = 1.5f;
    [Tooltip("Damage multiplier applied to hits landed while guard-broken.")]
    [SerializeField] private float guardBreakDamageMultiplier = 1.5f;

    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private HeroKnightPlayerController playerController;
    [Tooltip("Optional - auto-found via GetComponent. Used to check IsBlocking so incoming damage can be reduced/negated.")]
    [SerializeField] private PlayerAttack playerAttack;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip healClip;
    [Tooltip("Played whenever an attack is blocked (whether fully or partially negated).")]
    [SerializeField] private AudioClip blockedHitClip;
    [Tooltip("Played on a perfect parry - landing a block within the parry window.")]
    [SerializeField] private AudioClip parrySuccessClip;
    [SerializeField] private AudioClip guardBreakClip;

    [Header("Events")]
    [Tooltip("Invoked whenever health changes: (currentHealth, maxHealth). Hook a health bar UI up to this.")]
    public HealthChangedEvent onHealthChanged;
    public UnityEvent onDamaged;
    public UnityEvent onBlocked;
    public UnityEvent onParried;
    public UnityEvent onDied;
    [Tooltip("Invoked whenever the player's own guard posture changes: (currentGuardPosture, maxGuardPosture).")]
    public HealthChangedFloatEvent onGuardPostureChanged;
    public UnityEvent onGuardBroken;

    // UnityEvent<int,int> can't be serialized/shown in the Inspector directly -
    // it needs a concrete, non-generic subclass like this one.
    [System.Serializable]
    public class HealthChangedEvent : UnityEvent<int, int> { }

    // Same deal for the float-based guard posture event.
    [System.Serializable]
    public class HealthChangedFloatEvent : UnityEvent<float, float> { }

    private int currentHealth;
    private float invulnerabilityTimer;
    private float currentGuardPosture;
    private float lastGuardHitTime = float.NegativeInfinity;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    public float CurrentGuardPosture => currentGuardPosture;
    public float MaxGuardPosture => maxGuardPosture;

    /// <summary>True while guard-broken: can't block, takes bonus damage.</summary>
    public bool IsGuardBroken { get; private set; }

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (playerController == null) playerController = GetComponent<HeroKnightPlayerController>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (playerAttack == null) playerAttack = GetComponent<PlayerAttack>();

        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
        }

        if (!IsDead && !IsGuardBroken && currentGuardPosture > 0f && Time.time - lastGuardHitTime > guardRegenDelay)
        {
            float previous = currentGuardPosture;
            currentGuardPosture = Mathf.Max(0f, currentGuardPosture - guardRegenPerSecond * Time.deltaTime);

            if (!Mathf.Approximately(previous, currentGuardPosture))
            {
                onGuardPostureChanged?.Invoke(currentGuardPosture, maxGuardPosture);
            }
        }
    }

    /// <summary>
    /// Call this from whatever deals damage to the player (an enemy hitbox,
    /// a trap, a projectile, etc). Pass the attacking EnemyHealth when
    /// available so a successful parry can damage its posture bar.
    ///
    /// - Landing while IsParryWindowOpen (just pressed Block) is a perfect
    ///   parry: fully negates damage, no cost to the player, and slams the
    ///   attacker's posture.
    /// - Landing while IsBlocking but outside the parry window is a normal
    ///   block: damage scaled by blockDamageMultiplier (not fully free
    ///   anymore), and it costs the player's own guard posture. Enough
    ///   mistimed blocks in a row and the player gets guard-broken.
    /// - Landing while guard-broken deals bonus damage and can't be blocked
    ///   at all (PlayerAttack refuses to start a block while this is true).
    /// - Otherwise the hit lands fully and plays Hurt if the player survives,
    ///   or hands off to NewPlayerController.Die() if health reaches zero.
    /// </summary>
    public void TakeDamage(int amount, EnemyHealth attacker = null)
    {
        if (IsDead || amount <= 0 || invulnerabilityTimer > 0f)
        {
            return;
        }

        bool isBlocking = playerAttack != null && playerAttack.IsBlocking;
        bool isParry = playerAttack != null && playerAttack.IsParryWindowOpen;

        if (isParry)
        {
            invulnerabilityTimer = invulnerabilityDuration;
            onParried?.Invoke();
            PlaySound(parrySuccessClip);

            if (attacker != null)
            {
                attacker.AddPosture(parryPostureDamage, true);
            }

            return;
        }

        if (IsGuardBroken)
        {
            amount = Mathf.RoundToInt(amount * guardBreakDamageMultiplier);
        }
        else if (isBlocking)
        {
            amount = Mathf.RoundToInt(amount * blockDamageMultiplier);
            onBlocked?.Invoke();
            PlaySound(blockedHitClip);
            AddGuardPosture(blockGuardCost);
        }

        if (amount <= 0)
        {
            // Fully blocked - still start the invulnerability window so a
            // flurry of blocked hits doesn't spam onBlocked every frame.
            invulnerabilityTimer = invulnerabilityDuration;
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        invulnerabilityTimer = invulnerabilityDuration;
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (!isBlocking)
        {
            // Don't play the Hurt flinch on top of the Block pose - only
            // flinch on hits that weren't blocked at all.
            onDamaged?.Invoke();
            PlaySound(hurtClip);

            if (playerController != null)
            {
                playerController.PlayHurt();
            }
            else
            {
                anim.SetTrigger("Hurt");
            }
        }
    }

    private void AddGuardPosture(float amount)
    {
        if (IsGuardBroken || amount <= 0f)
        {
            return;
        }

        currentGuardPosture = Mathf.Min(maxGuardPosture, currentGuardPosture + amount);
        lastGuardHitTime = Time.time;
        onGuardPostureChanged?.Invoke(currentGuardPosture, maxGuardPosture);

        if (currentGuardPosture >= maxGuardPosture)
        {
            GuardBreak();
        }
    }

    private void GuardBreak()
    {
        IsGuardBroken = true;
        onGuardBroken?.Invoke();
        PlaySound(guardBreakClip);

        if (playerAttack != null)
        {
            playerAttack.ForceStopBlocking();
        }

        CancelInvoke(nameof(ClearGuardBreak));
        Invoke(nameof(ClearGuardBreak), guardBreakDuration);
    }

    private void ClearGuardBreak()
    {
        IsGuardBroken = false;
        currentGuardPosture = 0f;
        onGuardPostureChanged?.Invoke(currentGuardPosture, maxGuardPosture);
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        PlaySound(healClip);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void ResetHealth()
    {
        IsDead = false;
        currentHealth = maxHealth;
        invulnerabilityTimer = 0f;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        onDied?.Invoke();

        if (playerController != null)
        {
            playerController.Die();
        }
        else
        {
            anim.SetTrigger("Death");
        }
    }
}