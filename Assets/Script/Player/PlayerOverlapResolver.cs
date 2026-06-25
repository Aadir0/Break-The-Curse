using System.Collections;
using UnityEngine;

public class PlayerOverlapResolver : MonoBehaviour
{
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private float searchStep = 0.05f;
    [SerializeField] private float maxSearchDistance = 2f;

    public IEnumerator ResolvePosition()
    {
        // Wait until Unity updates Tilemap colliders
        yield return new WaitForFixedUpdate();

        // If player isn't overlapping anything, we're done.
        if (!Physics2D.OverlapBox(
            playerCollider.bounds.center,
            playerCollider.bounds.size * 0.9f,
            0,
            groundLayer))
            yield break;

        Vector2 startPos = transform.position;

        Vector2[] directions =
        {
            Vector2.up,
            (Vector2.up + Vector2.right).normalized,
            (Vector2.up + Vector2.left).normalized,
            Vector2.right,
            Vector2.left,
            Vector2.down
        };

        int maxSteps = Mathf.CeilToInt(maxSearchDistance / searchStep);

        foreach (Vector2 dir in directions)
        {
            for (int i = 1; i <= maxSteps; i++)
            {
                Vector2 testPos = startPos + dir * (searchStep * i);

                bool blocked = Physics2D.OverlapBox(
                    testPos,
                    playerCollider.bounds.size * 0.9f,
                    0,
                    groundLayer);

                if (!blocked)
                {
                    transform.position = testPos;
                    yield break;
                }
            }
        }

        Debug.LogWarning("No free position found.");
    }
}