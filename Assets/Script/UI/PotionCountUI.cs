using TMPro;
using UnityEngine;

public class PotionCountUI : MonoBehaviour
{
    [SerializeField] private PlayerPotionInventory potionInventory;
    [SerializeField] private TMP_Text potionCountText;
    [SerializeField] private string label = "Potion";

    private void Awake()
    {
        if (potionCountText == null)
        {
            potionCountText = GetComponent<TMP_Text>();
        }
    }

    private void Start()
    {
        if (potionInventory == null)
        {
            potionInventory = FindAnyObjectByType<PlayerPotionInventory>();
        }

        if (potionInventory == null)
        {
            SetPotionCount(0, 0);
            return;
        }

        potionInventory.onPotionCountChanged.AddListener(SetPotionCount);
        SetPotionCount(potionInventory.CurrentPotions, potionInventory.MaxPotions);
    }

    private void OnDestroy()
    {
        if (potionInventory != null)
        {
            potionInventory.onPotionCountChanged.RemoveListener(SetPotionCount);
        }
    }

    public void SetPotionCount(int currentPotions, int maxPotions)
    {
        if (potionCountText == null)
        {
            return;
        }

        potionCountText.text = $"{label}: {currentPotions}/{maxPotions}";
    }
}
