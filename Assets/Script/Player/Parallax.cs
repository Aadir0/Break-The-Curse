using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float parallaxSpeed = 0.5f;
    [SerializeField] private float movementThreshold = 0.01f;

    private Vector3 lastPlayerPosition;

    void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (player != null)
        {
            lastPlayerPosition = player.position;
        }
    }

    void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        Vector3 currentPlayerPosition = player.position;
        float deltaX = currentPlayerPosition.x - lastPlayerPosition.x;

        if (Mathf.Abs(deltaX) > movementThreshold)
        {
            transform.position += new Vector3(-deltaX * parallaxSpeed, 0f, 0f);
        }

        lastPlayerPosition = currentPlayerPosition;
    }
}
