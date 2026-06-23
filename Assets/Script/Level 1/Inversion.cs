using System;
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
    [SerializeField] private GameObject orbPrefab;
    [SerializeField] private float orbSpawnDistance = 8f;
    private GameObject currentOrb;
    public bool isInverted = false;
    private float invertedTimer = 0f;
    void Update()
    {
        if (isInverted)
        {
            invertedTimer += Time.deltaTime;
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            isInverted = !isInverted;

            Actualvolume.SetActive(!Actualvolume.activeSelf);
            can.SetActive(!can.activeSelf);
            GlobalVolume.SetActive(!GlobalVolume.activeSelf);
            InvertedObjects.SetActive(!InvertedObjects.activeSelf);
            ActualObjects.SetActive(!ActualObjects.activeSelf);
            InvertedBlocks.SetActive(!InvertedBlocks.activeSelf);
            ActualBlocks.SetActive(!ActualBlocks.activeSelf);

            if (isInverted)
            {
                invertedTimer = 0f;
                SpawnOrb();
            }
            else
            {
                if (currentOrb != null)
                    Destroy(currentOrb);
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
    public float GetInvertedTime()
    {
        return invertedTimer;
    }
}
