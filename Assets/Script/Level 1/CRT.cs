using UnityEngine;

public class CRT : MonoBehaviour
{
    public CanvasGroup cg;
    void Update()
    {
        cg.alpha = Random.Range(0f, 0.8f);
    }
}