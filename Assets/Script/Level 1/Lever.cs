using UnityEngine;

public class Lever : MonoBehaviour
{
    [Header("Lever")]
    [SerializeField] private Animator animator;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("Box Drop")]
    [Tooltip("Check this on only one of the three levers.")]
    [SerializeField] private bool dropsBox;
    [SerializeField] private Rigidbody2D boxRigidbody;
    [SerializeField] private float playerBelowHorizontalRange = 0.75f;

    private bool playerInRange;
    private bool isPulled;
    private bool dropSequenceStarted;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            PullLever();
        }
    }

    public void PullLever()
    {
        if (isPulled)
        {
            return;
        }

        isPulled = true;
        PlayPulledAnimation();
        CheckAllLevers();
    }

    void CheckAllLevers()
    {
        Lever[] levers = FindObjectsByType<Lever>(FindObjectsSortMode.None);

        if (levers.Length < 3)
        {
            return;
        }

        foreach (Lever lever in levers)
        {
            if (!lever.isPulled)
            {
                return;
            }
        }

        foreach (Lever lever in levers)
        {
            if (lever.dropsBox)
            {
                lever.StartBoxDropSequence();
                break;
            }
        }
    }

    void StartBoxDropSequence()
    {
        if (dropSequenceStarted)
        {
            return;
        }

        if (boxRigidbody == null)
        {
            Debug.LogWarning($"{name} is marked to drop the box, but no box Rigidbody2D is assigned.", this);
            return;
        }

        dropSequenceStarted = true;
        StartCoroutine(WaitForPlayerBelowBox());
    }

    System.Collections.IEnumerator WaitForPlayerBelowBox()
    {
        GameObject player = null;

        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null;
        }

        while (!IsPlayerBelowBox(player.transform))
        {
            yield return null;
        }

        boxRigidbody.gravityScale = 1f;
        boxRigidbody.WakeUp();
    }

    bool IsPlayerBelowBox(Transform player)
    {
        Vector2 boxPosition = boxRigidbody.position;
        Vector2 playerPosition = player.position;

        bool isBelow = playerPosition.y < boxPosition.y;
        bool isHorizontallyAligned = Mathf.Abs(playerPosition.x - boxPosition.x)
            <= playerBelowHorizontalRange;

        return isBelow && isHorizontallyAligned;
    }

    void PlayPulledAnimation()
    {
        if (animator == null)
        {
            return;
        }

        string[] stateNames = { "On", "On2", "On3" };

        foreach (string stateName in stateNames)
        {
            int stateHash = Animator.StringToHash($"Base Layer.{stateName}");

            if (animator.HasState(0, stateHash))
            {
                animator.Play(stateHash, 0, 0f);
                return;
            }
        }

        Debug.LogWarning($"No On animation state was found for {name}.", this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
