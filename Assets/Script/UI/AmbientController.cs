using UnityEngine;

public class AmbientController : MonoBehaviour
{
    [SerializeField] private AmbientWriting CanvasConttroller;

    private void Start()
    {
        if (CanvasConttroller == null)
        {
            CanvasConttroller = FindAnyObjectByType<AmbientWriting>();
        }

        if (CanvasConttroller != null)
        {
            CanvasConttroller.Display();
        }
    }
}
