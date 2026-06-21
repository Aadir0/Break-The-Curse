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
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Actualvolume.SetActive(!Actualvolume.activeSelf);
            can.SetActive(!can.activeSelf);
            GlobalVolume.SetActive(!GlobalVolume.activeSelf);
            InvertedObjects.SetActive(!InvertedObjects.activeSelf);
            ActualObjects.SetActive(!ActualObjects.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
