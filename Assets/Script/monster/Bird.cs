using UnityEngine;

public class Bird : MonoBehaviour
{
    public float speed = 2f;
    private int direction = 1;

    public void Initialize(int dir, float moveSpeed)
    {
        direction = dir;
        speed = moveSpeed;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;
    }

    void Update()
    {
        transform.Translate(Vector2.right * direction * speed * Time.deltaTime);

        Camera cam = Camera.main;
        float screenWidth = cam.orthographicSize * cam.aspect;

        if (Mathf.Abs(transform.position.x - cam.transform.position.x) > screenWidth + 5f)
        {
            Destroy(gameObject);
        }
    }
}