using UnityEngine;
using UnityEngine.Tilemaps;

public class Wallbreak : MonoBehaviour
{
    [SerializeField] private Tilemap wallTilemap;

    private bool broken = false;

    public void BreakWall()
    {
        if (broken)
            return;

        broken = true;

        wallTilemap.ClearAllTiles();

        Debug.Log("Wall Broken!");
    }
}