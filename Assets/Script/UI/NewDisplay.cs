using UnityEngine;

public class NewDisplay : MonoBehaviour
{
    [SerializeField] private TutorialWriting CanvasConttroller;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
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
