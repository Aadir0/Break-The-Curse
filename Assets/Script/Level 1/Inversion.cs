using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Inversion : MonoBehaviour
{
    private const int LevelOneSceneIndex = 1;
    [SerializeField] private GameObject InvertedObjects;
    [SerializeField] private GameObject GlobalVolume;
    [SerializeField] private GameObject ActualObjects;
    [SerializeField] private GameObject can;
    [SerializeField] private GameObject Actualvolume;
    [SerializeField] private GameObject InvertedBlocks;
    [SerializeField] private GameObject ActualBlocks;
    [SerializeField] private GameObject winningScene;
    [SerializeField] private GameObject portal;
    [SerializeField] private GameObject orbPrefab;
    [SerializeField] private float orbSpawnDistance = 8f;
    [SerializeField] private float winningSceneDuration = 2.5f;
    [SerializeField] private float timeToWin = 5f;
    private GameObject currentOrb;
    public bool isInverted = false;
    private float invertedTimer = 0f;
    private float winningSceneTimer = 0f;
    private bool winningSequenceTriggered = false;
    private bool winningSceneHidden = false;
    private PlayerController playerController;

    void Update()
    {
        bool isLevelOneScene = SceneManager.GetActiveScene().buildIndex == LevelOneSceneIndex;

        if (IsPlayerDead())
        {
            if (isLevelOneScene)
            {
                HideWinObjectsUntilAlive();
            }

            return;
        }

        if (isInverted && isLevelOneScene)
        {
            invertedTimer += Time.deltaTime;

            if (!winningSequenceTriggered && invertedTimer >= timeToWin)
            {
                winningSequenceTriggered = true;
                winningSceneTimer = 0f;

                SwitchToNormalWorld();

                if (winningScene != null)
                {
                    winningScene.SetActive(true);
                }

                if (portal != null)
                {
                    portal.SetActive(false);
                }
            }

        }

        if (winningSequenceTriggered && !winningSceneHidden)
        {
            winningSceneTimer += Time.deltaTime;

            if (winningSceneTimer >= winningSceneDuration)
            {
                winningSceneHidden = true;

                if (winningScene != null)
                {
                    winningScene.SetActive(false);
                }

                if (portal != null)
                {
                    portal.SetActive(true);
                }
            }
        }

        if (!isLevelOneScene)
        {
            winningSequenceTriggered = false;
            winningSceneHidden = false;
            winningSceneTimer = 0f;
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            isInverted = !isInverted;

            if (isInverted)
            {
                SwitchToInvertedWorld();
                invertedTimer = 0f;
                winningSceneTimer = 0f;
                winningSceneHidden = false;

                if (!winningSequenceTriggered && winningScene != null)
                {
                    winningScene.SetActive(false);
                }

                if (CanHidePortal() && portal != null)
                {
                    portal.SetActive(false);
                }

                ShowPortalIfCrystalWinComplete();

                SpawnOrb();
            }
            else
            {
                SwitchToNormalWorld();

                if (currentOrb != null)
                {
                    Destroy(currentOrb);
                    currentOrb = null;
                }

                if (!winningSequenceTriggered && winningScene != null)
                {
                    winningScene.SetActive(false);
                }

                if (CanHidePortal() && portal != null)
                {
                    portal.SetActive(false);
                }

                ShowPortalIfCrystalWinComplete();
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
    private void SpawnOrb()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        float angle = UnityEngine.Random.Range(0f, 360f);

        Vector2 offset =
            new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ) * orbSpawnDistance;

        currentOrb = Instantiate(
            orbPrefab,
            (Vector2)player.transform.position + offset,
            Quaternion.identity);

        Orb orbScript = currentOrb.GetComponent<Orb>();
        orbScript.SetInversion(this);
    }

    private void SwitchToInvertedWorld()
    {
        Actualvolume.SetActive(false);
        can.SetActive(true);
        GlobalVolume.SetActive(true);
        InvertedObjects.SetActive(true);
        ActualObjects.SetActive(false);
        InvertedBlocks.SetActive(true);
        ActualBlocks.SetActive(false);
    }

    private void SwitchToNormalWorld()
    {
        isInverted = false;
        invertedTimer = 0f;

        Actualvolume.SetActive(true);
        can.SetActive(false);
        GlobalVolume.SetActive(false);
        InvertedObjects.SetActive(false);
        ActualObjects.SetActive(true);
        InvertedBlocks.SetActive(false);
        ActualBlocks.SetActive(true);

        if (currentOrb != null)
        {
            Destroy(currentOrb);
            currentOrb = null;
        }
    }
    public float GetInvertedTime()
    {
        return invertedTimer;
    }

    private bool IsPlayerDead()
    {
        if (playerController == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
            }
        }

        return playerController != null && playerController.IsDead;
    }

    private void HideWinObjectsUntilAlive()
    {
        if (winningScene != null)
        {
            winningScene.SetActive(false);
        }

        if (CanHidePortal() && portal != null)
        {
            portal.SetActive(false);
        }
    }

    private bool CanHidePortal()
    {
        return !winningSequenceTriggered && !Crystal.IsCrystalWinCompleteInActiveScene;
    }

    private void ShowPortalIfCrystalWinComplete()
    {
        if (Crystal.IsCrystalWinCompleteInActiveScene && portal != null)
        {
            portal.SetActive(true);
        }
    }
}
