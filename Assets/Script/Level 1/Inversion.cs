using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using System.Collections.Generic;

public class Inversion : MonoBehaviour
{
    [SerializeField] private BoxCollider2D checkCollider;
    [SerializeField] private GameObject InvertedObjects;
    [SerializeField] private GameObject GlobalVolume;
    [SerializeField] private GameObject can;
    [SerializeField] private GameObject portal;
    [SerializeField] private GameObject TeenPlayer;
    [SerializeField] private Transform TeenPlayerTransform;
    [SerializeField] private GameObject OldPlayer;
    [SerializeField] private Transform OldPlayerTransform;
    [SerializeField] private CinemachineCamera vCam;
    [SerializeField] private HeroKnightPlayerController heroController;
    [SerializeField] private NewPlayerController newPlayerController;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private AmbientWriting ConversationCanvas;
    [SerializeField] private GameObject EndingScene;
    private PlayerOverlapResolver overlapResolver;
    private readonly List<EnemyHealth> trackedEnemies = new List<EnemyHealth>();
    public bool isInverted = false;
    private bool hasTriggered;
    private int enemiesRemaining;
    private float invertedTimer = 0f;
    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            overlapResolver = player.GetComponent<PlayerOverlapResolver>();
        }

        InitializeEnemyTracking();
        UpdatePortalState();
    }

    private void OnDestroy()
    {
        StopTrackingEnemies();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (isInverted)
        {
            RefreshEnemyProgress();
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasTriggered || !collision.CompareTag("Player"))
        {
            return;
        }

        hasTriggered = true;

        if (checkCollider != null)
        {
            checkCollider.enabled = false;
        }

        isInverted = !isInverted;

        if (isInverted)
        {
            SwitchToInvertedWorld();
            invertedTimer = 0f;
            ConversationCanvas.Display();
        }
        else
        {
            SwitchToNormalWorld();
        }
    }
    private void SwitchToInvertedWorld()
    {
        var targetStruct = vCam.Target;
        if (portal != null)
        {
            portal.SetActive(false);
        }
        can.SetActive(true);
        GlobalVolume.SetActive(true);
        InvertedObjects.SetActive(true);
        TeenPlayer.SetActive(false);
        OldPlayer.SetActive(true);
        targetStruct.TrackingTarget = OldPlayer.transform;
        vCam.Target = targetStruct;
        OldPlayerTransform.position = TeenPlayerTransform.position;
        if (overlapResolver != null)
        {
            StartCoroutine(overlapResolver.ResolvePosition());
        }
        vCam.UpdateTargetCache();
    }

    private void SwitchToNormalWorld()
    {
        var targetStruct = vCam.Target;
        isInverted = false;
        invertedTimer = 0f;
        UpdatePortalState();
        can.SetActive(false);
        GlobalVolume.SetActive(false);
        InvertedObjects.SetActive(false);
        TeenPlayer.SetActive(true);
        OldPlayer.SetActive(false);
        targetStruct.TrackingTarget = TeenPlayerTransform;
        vCam.Target = targetStruct;
        TeenPlayerTransform.position = OldPlayerTransform.position;
        if (overlapResolver != null)
        {
            StartCoroutine(overlapResolver.ResolvePosition());
        }
        vCam.UpdateTargetCache();
    }

    private void InitializeEnemyTracking()
    {
        StopTrackingEnemies();
        trackedEnemies.Clear();

        EnemyHealth[] enemies = InvertedObjects != null
            ? InvertedObjects.GetComponentsInChildren<EnemyHealth>(true)
            : FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            trackedEnemies.Add(enemy);
            enemy.onDied.AddListener(HandleEnemyDied);
        }

        RefreshEnemyProgress();
    }

    private void StopTrackingEnemies()
    {
        foreach (EnemyHealth enemy in trackedEnemies)
        {
            if (enemy != null)
            {
                enemy.onDied.RemoveListener(HandleEnemyDied);
            }
        }

        trackedEnemies.Clear();
    }

    private void HandleEnemyDied()
    {
        RefreshEnemyProgress();
    }

    private void RefreshEnemyProgress()
    {
        trackedEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);
        enemiesRemaining = trackedEnemies.Count;

        if (enemiesRemaining == 0)
        {
            EndingScene.SetActive(true);
            UpdatePortalState();

            if (isInverted)
            {
                SwitchToNormalWorld();
            }
        }
    }

    private void UpdatePortalState()
    {
        if (portal != null)
        {
            portal.SetActive(enemiesRemaining <= 0 && !isInverted);
        }
    }
    public float GetInvertedTime()
    {
        return invertedTimer;
    }
}
