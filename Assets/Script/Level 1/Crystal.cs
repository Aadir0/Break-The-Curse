using UnityEngine;
using UnityEngine.SceneManagement;

public class Crystal : MonoBehaviour
{
    private static readonly System.Collections.Generic.HashSet<string> CollectedCrystalIds =
        new System.Collections.Generic.HashSet<string>();

    private static GameObject levelWinCanvas;
    private static GameObject levelNextRoomPortal;
    private static int resetSceneHandle = -1;
    private static int completedWinSceneBuildIndex = -1;

    public static bool IsCrystalWinComplete { get; private set; }
    public static bool IsCrystalWinCompleteInActiveScene =>
        IsCrystalWinComplete
        && SceneManager.GetActiveScene().buildIndex == completedWinSceneBuildIndex;

    [SerializeField] private int crystalsRequiredToWin = 5;
    [SerializeField] private int firstCrystalLevelBuildIndex = 1;
    [SerializeField] private int winLevelBuildIndex = 2;
    [SerializeField] private float winCanvasDuration = 2.5f;
    [SerializeField] private GameObject winCanvas;
    [SerializeField] private GameObject nextRoomPortal;

    private string crystalId;
    private bool collected;

    private void Start()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.buildIndex == firstCrystalLevelBuildIndex && resetSceneHandle != activeScene.handle)
        {
            CollectedCrystalIds.Clear();
            resetSceneHandle = activeScene.handle;
            levelWinCanvas = null;
            levelNextRoomPortal = null;
            IsCrystalWinComplete = false;
            completedWinSceneBuildIndex = -1;
        }

        crystalId = GetCrystalId(activeScene);

        RegisterWinObjects();

        if (activeScene.buildIndex == winLevelBuildIndex && CollectedCrystalIds.Count >= crystalsRequiredToWin)
        {
            CompleteCrystalWin(activeScene.buildIndex);
            RestoreCompletedLevelState();
        }

        if (CollectedCrystalIds.Contains(crystalId))
        {
            Destroy(gameObject);
            return;
        }

        if (activeScene.buildIndex == winLevelBuildIndex && CollectedCrystalIds.Count < crystalsRequiredToWin)
        {
            SetWinObjectsActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !CompareTag("Crystal") || !other.CompareTag("Player"))
        {
            return;
        }

        collected = true;
        CollectedCrystalIds.Add(crystalId);

        if (SceneManager.GetActiveScene().buildIndex == winLevelBuildIndex
            && CollectedCrystalIds.Count >= crystalsRequiredToWin)
        {
            CompleteCrystalWin(SceneManager.GetActiveScene().buildIndex);
            SetWinObjectsActive(true);
        }

        Destroy(gameObject);
    }

    private string GetCrystalId(Scene scene)
    {
        Vector3 position = transform.position;
        return scene.buildIndex + ":"
            + gameObject.name + ":"
            + position.x.ToString("F3") + ":"
            + position.y.ToString("F3") + ":"
            + position.z.ToString("F3");
    }

    private void SetWinObjectsActive(bool isActive)
    {
        RegisterWinObjects();

        if (levelWinCanvas != null)
        {
            CrystalWinCanvasAutoHide autoHide =
                levelWinCanvas.GetComponent<CrystalWinCanvasAutoHide>();

            if (autoHide == null)
            {
                autoHide = levelWinCanvas.AddComponent<CrystalWinCanvasAutoHide>();
            }

            autoHide.SetActiveForDuration(isActive, winCanvasDuration);
        }

        if (levelNextRoomPortal != null)
        {
            levelNextRoomPortal.SetActive(isActive);
        }
    }

    private void RestoreCompletedLevelState()
    {
        RegisterWinObjects();

        if (levelWinCanvas != null)
        {
            levelWinCanvas.SetActive(false);
        }

        if (levelNextRoomPortal != null)
        {
            levelNextRoomPortal.SetActive(true);
        }
    }

    private void RegisterWinObjects()
    {
        if (winCanvas != null)
        {
            levelWinCanvas = winCanvas;
        }

        if (nextRoomPortal != null)
        {
            levelNextRoomPortal = nextRoomPortal;
        }
    }

    private static void CompleteCrystalWin(int sceneBuildIndex)
    {
        IsCrystalWinComplete = true;
        completedWinSceneBuildIndex = sceneBuildIndex;
    }
}

public class CrystalWinCanvasAutoHide : MonoBehaviour
{
    private Coroutine hideCoroutine;

    public void SetActiveForDuration(bool isActive, float duration)
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        gameObject.SetActive(isActive);

        if (isActive)
        {
            hideCoroutine = StartCoroutine(HideAfterDelay(duration));
        }
    }

    private System.Collections.IEnumerator HideAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        hideCoroutine = null;
        gameObject.SetActive(false);
    }
}
