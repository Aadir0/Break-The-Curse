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
    [SerializeField] private GameObject Orb;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Actualvolume.SetActive(!Actualvolume.activeSelf);
            can.SetActive(!can.activeSelf);
            Orb.SetActive(!Orb.activeSelf);
            GlobalVolume.SetActive(!GlobalVolume.activeSelf);
            InvertedObjects.SetActive(!InvertedObjects.activeSelf);
            ActualObjects.SetActive(!ActualObjects.activeSelf);
            InvertedBlocks.SetActive(!InvertedBlocks.activeSelf);
            ActualBlocks.SetActive(!ActualBlocks.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
