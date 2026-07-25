using UnityEngine;
using UnityEngine.Events;

public class PlayerPotionInventory : MonoBehaviour
{
    [SerializeField] private int startingPotions;
    [SerializeField] private int maxPotions = 9;
    [SerializeField] private int healAmount = 35;
    [SerializeField] private KeyCode consumeKey = KeyCode.H;

    [Header("Events")]
    public PotionCountChangedEvent onPotionCountChanged = new PotionCountChangedEvent();
    public UnityEvent onPotionUsed = new UnityEvent();
    public UnityEvent onPotionAdded = new UnityEvent();

    [System.Serializable]
    public class PotionCountChangedEvent : UnityEvent<int, int> { }

    private PlayerHealth playerHealth;
    private int currentPotions;
    private bool hasInitialized;

    public int CurrentPotions => currentPotions;
    public int MaxPotions => maxPotions;

    private void Awake()
    {
        Initialize();
    }

    private void OnValidate()
    {
        maxPotions = Mathf.Max(1, maxPotions);
        startingPotions = Mathf.Clamp(startingPotions, 0, maxPotions);
        healAmount = Mathf.Max(1, healAmount);
    }

    private void Start()
    {
        Initialize();
        onPotionCountChanged?.Invoke(currentPotions, maxPotions);
    }

    private void Update()
    {
        Initialize();

        if (Input.GetKeyDown(consumeKey))
        {
            UsePotion();
        }
    }

    public bool AddPotion(int amount = 1)
    {
        Initialize();

        if (amount <= 0 || currentPotions >= maxPotions)
        {
            return false;
        }

        currentPotions = Mathf.Min(maxPotions, currentPotions + amount);
        onPotionAdded?.Invoke();
        onPotionCountChanged?.Invoke(currentPotions, maxPotions);
        return true;
    }

    public bool UsePotion()
    {
        Initialize();

        if (currentPotions <= 0 || playerHealth == null || playerHealth.IsDead)
        {
            return false;
        }

        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
        {
            return false;
        }

        currentPotions--;
        playerHealth.Heal(healAmount);
        onPotionUsed?.Invoke();
        onPotionCountChanged?.Invoke(currentPotions, maxPotions);
        return true;
    }

    private void Initialize()
    {
        if (hasInitialized)
        {
            return;
        }

        playerHealth = GetComponent<PlayerHealth>();
        maxPotions = Mathf.Max(1, maxPotions);
        currentPotions = Mathf.Clamp(startingPotions, 0, maxPotions);
        hasInitialized = true;
    }
}
