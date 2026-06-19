using UnityEngine;

public class TheRock : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Wallbreak wall =
            collision.gameObject.GetComponent<Wallbreak>();

        if (wall != null)
        {
            wall.BreakWall();
        }
    }
}