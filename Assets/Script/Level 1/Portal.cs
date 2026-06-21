using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Portal : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float centerTolerance = 0.1f;
    [SerializeField] private float shrinkDuration = 0.5f;

    private bool transitionStarted;

    void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            TryStartTransition();
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            TryStartTransition();
        }
    }

    void TryStartTransition()
    {
        if (transitionStarted || player == null)
        {
            return;
        }

        if (!IsPlayerCentered())
        {
            return;
        }

        transitionStarted = true;
        StartCoroutine(LoadNextScene());
    }

    bool IsPlayerCentered()
    {
        Vector3 portalPosition = transform.position;
        Vector3 playerPosition = player.position;

        return Mathf.Abs(playerPosition.x - portalPosition.x) <= centerTolerance
            && Mathf.Abs(playerPosition.y - portalPosition.y) <= centerTolerance;
    }

    private IEnumerator LoadNextScene()
    {
        if (player == null)
        {
            yield break;
        }

        Vector3 startingScale = player.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < shrinkDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / shrinkDuration);
            player.localScale = Vector3.Lerp(startingScale, Vector3.zero, progress);
            yield return null;
        }

        player.localScale = Vector3.zero;
        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
