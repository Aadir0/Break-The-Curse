using UnityEngine;

public class NewDisplay : MonoBehaviour
{
    [SerializeField] private TutorialWriting CanvasConttroller;
    [SerializeField] private GameObject manager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            manager.SetActive(true);
            if (CanvasConttroller == null)
            {
                CanvasConttroller = FindAnyObjectByType<TutorialWriting>();
            }

            if (CanvasConttroller != null)
            {
                CanvasConttroller.Display();
            }
        }
    }
}
