using UnityEngine;
using UnityEngine.Events;

// Enemy health with Hurt/Die animations, plus an optional "sometimes blocks
// incoming attacks" mechanic (drives the Block trigger -> Shield state).
// Set canBlock to false to reuse this exact script on enemies that should
// never block.
//
// Animator parameters used: Hurt, Die, Block (triggers)
[RequireComponent(typeof(Animator))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 50;

    [Header("Blocking")]
    [Tooltip("If false, this enemy can never block, regardless of the settings below. Set false to reuse this script on non-blocking enemies.")]
    [SerializeField] private bool canBlock = false;
    [Range(0f, 1f)]
    [Tooltip("Chance (0-1) that an incoming hit gets blocked, rolled per hit.")]
    [SerializeField] private float blockChance = 0.3f;
    [Range(0f, 1f)]
    [Tooltip("Fraction of incoming damage still taken while blocking. 0 = fully blocks, 1 = block has no effect.")]
    [SerializeField] private float blockDamageMultiplier = 0f;
    [Tooltip("Minimum time between blocks, so the enemy can't block every single hit in a row.")]
    [SerializeField] private float blockCooldown = 2f;
    [Tooltip("How long IsBlocking stays true after a block - roughly match your Shield animation length.")]
    [SerializeField] private float blockReactionDuration = 0.4f;

    [Header("Posture (Parry Bar)")]
    [Tooltip("Posture damage from parries fills this. Full bar = staggered and executable, Sekiro-style.")]
    [SerializeField] private float maxPosture = 100f;
    [Tooltip("How much posture drains back down per second once postureRegenDelay has passed since the last hit.")]
    [SerializeField] private float postureRegenPerSecond = 10f;
    [Tooltip("How long after the last posture hit before it starts draining back down.")]
    [SerializeField] private float postureRegenDelay = 3f;
    [Tooltip("How long the enemy stays staggered (frozen, executable) before recovering if not executed first.")]
    [SerializeField] private float staggerDuration = 4f;

    [Header("References")]
    [SerializeField] private Animator anim;
    [Tooltip("Optional - auto-found via GetComponent. Shows floating text when posture is damaged.")]
    [SerializeField] private DamagePopupSpawner popupSpawner;

    [Header("Posture Damage Popup")]
    [SerializeField] private Color parryPopupColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color hitPostureColor = Color.white;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip blockClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip staggerClip;
    [SerializeField] private float destroyDelayAfterDeath = 1.5f;

    [Header("Drops")]
    [SerializeField] private GameObject healthPotionPrefab;
    [Range(0f, 1f)]
    [SerializeField] private float healthPotionDropChance = 1f;
    [SerializeField] private Vector3 healthPotionDropOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Events")]
    public HealthChangedEvent onHealthChanged;
    public UnityEvent onDamaged;
    public UnityEvent onBlocked;
    public UnityEvent onDied;
    [Tooltip("Invoked whenever posture changes: (currentPosture, maxPosture). Hook a parry-bar UI up to this.")]
    public PostureChangedEvent onPostureChanged;
    public UnityEvent onStaggered;
    public UnityEvent onExecuted;
    [Tooltip("Invoked the moment the player first interacts with this enemy in combat (hit, blocked, or parried). Hook an enemy health bar's show/reset-timer here.")]
    public UnityEvent onCombatEngaged;

    // UnityEvent<float,float> needs a concrete subclass to show up in the Inspector.
    [System.Serializable]
    public class PostureChangedEvent : UnityEvent<float, float> { }

    // UnityEvent<int,int> needs a concrete subclass to show up in the Inspector.
    [System.Serializable]
    public class HealthChangedEvent : UnityEvent<int, int> { }

    private static readonly int HurtTrigger = Animator.StringToHash("Hurt");
    private static readonly int DieTrigger = Animator.StringToHash("Die");
    private static readonly int BlockTrigger = Animator.StringToHash("Block");

    private int currentHealth;
    private float blockCooldownTimer;
    private float currentPosture;
    private float lastPostureHitTime = float.NegativeInfinity;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    public float CurrentPosture => currentPosture;
    public float MaxPosture => maxPosture;

    /// <summary>True once the posture bar fills - frozen and vulnerable to Execute().</summary>
    public bool IsStaggered { get; private set; }

    /// <summary>True for a brief window right after a successful block - lets EnemyAttack/EnemyAI know to hold still.</summary>
    public bool IsBlocking { get; private set; }

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (popupSpawner == null) popupSpawner = GetComponent<DamagePopupSpawner>();

        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (blockCooldownTimer > 0f)
        {
            blockCooldownTimer -= Time.deltaTime;
        }

        if (!IsDead && !IsStaggered && currentPosture > 0f && Time.time - lastPostureHitTime > postureRegenDelay)
        {
            float previousPosture = currentPosture;
            currentPosture = Mathf.Max(0f, currentPosture - postureRegenPerSecond * Time.deltaTime);

            if (!Mathf.Approximately(previousPosture, currentPosture))
            {
                onPostureChanged?.Invoke(currentPosture, maxPosture);
            }
        }
    }

    /// <summary>
    /// Call this from whatever deals damage to the enemy (e.g. PlayerAttack's
    /// CheckAttackHit). May be reduced or fully negated if the enemy rolls a
    /// successful block.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        onCombatEngaged?.Invoke();

        bool blocked = canBlock && blockCooldownTimer <= 0f && Random.value <= blockChance;

        if (blocked)
        {
            amount = Mathf.RoundToInt(amount * blockDamageMultiplier);
            blockCooldownTimer = blockCooldown;
            PlayBlock();
        }

        if (amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (!blocked)
        {
            // Don't also play Hurt on top of the Block reaction this hit.
            onDamaged?.Invoke();
            PlayHurt();
        }
    }

    private void PlayBlock()
    {
        IsBlocking = true;
        anim.SetTrigger(BlockTrigger);
        onBlocked?.Invoke();
        PlaySound(blockClip);
        CancelInvoke(nameof(ClearBlockingFlag));
        Invoke(nameof(ClearBlockingFlag), blockReactionDuration);
    }

    private void ClearBlockingFlag()
    {
        IsBlocking = false;
    }

    /// <summary>
    /// Call this whenever the player damages this enemy's posture - either a
    /// perfect parry (isParry: true, see PlayerHealth.TakeDamage) or regular
    /// chip posture damage from a normal landed attack (isParry: false, see
    /// PlayerAttack.CheckAttackHit). Fills the posture bar and shows a
    /// floating popup; once it hits maxPosture the enemy becomes staggered
    /// and can be executed.
    /// </summary>
    public void AddPosture(float amount, bool isParry = false)
    {
        if (IsDead || IsStaggered || amount <= 0f)
        {
            return;
        }

        onCombatEngaged?.Invoke();

        float before = currentPosture;
        currentPosture = Mathf.Min(maxPosture, currentPosture + amount);
        float appliedAmount = currentPosture - before;
        lastPostureHitTime = Time.time;
        onPostureChanged?.Invoke(currentPosture, maxPosture);

        if (appliedAmount > 0f && popupSpawner != null)
        {
            string text = isParry ? $"PARRY -{appliedAmount:0}" : $"-{appliedAmount:0}";
            popupSpawner.Spawn(text, isParry ? parryPopupColor : hitPostureColor);
        }

        if (currentPosture >= maxPosture)
        {
            Stagger();
        }
    }

    private void Stagger()
    {
        IsStaggered = true;
        onStaggered?.Invoke();
        PlaySound(staggerClip);

        // No dedicated "Staggered" trigger in the Animator yet - Hurt is the
        // closest visual stand-in for now. Add a proper Staggered state/
        // trigger later and swap this out for something that holds a
        // vulnerable pose instead of a quick flinch.
        anim.SetTrigger(HurtTrigger);

        CancelInvoke(nameof(ClearStagger));
        Invoke(nameof(ClearStagger), staggerDuration);
    }

    private void ClearStagger()
    {
        if (IsDead)
        {
            return;
        }

        IsStaggered = false;
        currentPosture = 0f;
        onPostureChanged?.Invoke(currentPosture, maxPosture);
    }

    /// <summary>
    /// Instantly kills this enemy, bypassing normal health/blocking. Only
    /// call this while IsStaggered is true (PlayerAttack checks this before
    /// calling Execute). Invokes onExecuted in addition to onDied so you can
    /// hook up a distinct execution effect/camera cut if you want one.
    /// </summary>
    public void Execute()
    {
        if (IsDead || !IsStaggered)
        {
            return;
        }

        CancelInvoke(nameof(ClearStagger));
        onExecuted?.Invoke();
        Die();
    }

    private void PlayHurt()
    {
        anim.SetTrigger(HurtTrigger);
        PlaySound(hurtClip);
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        anim.SetTrigger(DieTrigger);
        onDied?.Invoke();
        PlaySound(deathClip);
        DropHealthPotion();

        if (TryGetComponent(out EnemyAI ai))
        {
            ai.enabled = false;
        }

        if (TryGetComponent(out EnemyAttack attack))
        {
            attack.enabled = false;
        }

        if (TryGetComponent(out FlyingEnemyAI flyingAi))
        {
            flyingAi.enabled = false;
        }

        if (TryGetComponent(out FlyingEnemyAttack flyingAttack))
        {
            // FlyingEnemyAttack.OnDisable() releases a lifted player back to
            // normal control if one is currently being carried.
            flyingAttack.enabled = false;
        }

        Destroy(gameObject, destroyDelayAfterDeath);
    }

    private void DropHealthPotion()
    {
        if (Random.value > healthPotionDropChance)
        {
            return;
        }

        Vector3 dropPosition = transform.position + healthPotionDropOffset;

        if (healthPotionPrefab != null)
        {
            Instantiate(healthPotionPrefab, dropPosition, Quaternion.identity);
            return;
        }

        GameObject potion = new GameObject("Health Potion");
        potion.transform.position = dropPosition;
        CircleCollider2D pickupCollider = potion.AddComponent<CircleCollider2D>();
        pickupCollider.isTrigger = true;
        pickupCollider.radius = 0.35f;
        Rigidbody2D pickupBody = potion.AddComponent<Rigidbody2D>();
        pickupBody.bodyType = RigidbodyType2D.Kinematic;
        pickupBody.gravityScale = 0f;
        potion.AddComponent<HealthPotionPickup>();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
