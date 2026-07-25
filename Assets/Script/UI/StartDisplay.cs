using UnityEngine;

public class StartDisplay : MonoBehaviour
{
    [SerializeField] private TutorialWriting CanvasConttroller;

    private void Start()
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
