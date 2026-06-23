using UnityEngine;
using UnityEngine.SceneManagement;

public class Inversion : MonoBehaviour
{
    [SerializeField] private GameObject InvertedObjects;
    [SerializeField] private GameObject GlobalVolume;
    [SerializeField] private GameObject ActualObjects;
    [SerializeField] private GameObject can;
    [SerializeField] private GameObject Actualvolume;
    [SerializeField] private GameObject InvertedBlocks;
    [SerializeField] private GameObject ActualBlocks;
    [SerializeField] private GameObject Orb;

    public static bool IsInverted { get; private set; }
    public static event System.Action<bool> WorldChanged;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            SetInverted(!IsInverted);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void Start()
    {
        IsInverted = InvertedObjects != null && InvertedObjects.activeSelf;
        ApplyWorldState(IsInverted);
        WorldChanged?.Invoke(IsInverted);
    }

    void SetInverted(bool inverted)
    {
        IsInverted = inverted;
        ApplyWorldState(IsInverted);
        WorldChanged?.Invoke(IsInverted);
    }

    void ApplyWorldState(bool inverted)
    {
        SetActiveIfAssigned(Actualvolume, !inverted);
        SetActiveIfAssigned(can, !inverted);
        SetActiveIfAssigned(GlobalVolume, inverted);
        SetActiveIfAssigned(InvertedObjects, inverted);
        SetActiveIfAssigned(ActualObjects, !inverted);
        SetActiveIfAssigned(InvertedBlocks, inverted);
        SetActiveIfAssigned(ActualBlocks, !inverted);

        if (Orb != null)
        {
            Orb.SetActive(inverted);
        }
    }

    void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
