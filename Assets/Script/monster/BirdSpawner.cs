using UnityEngine;
using System.Collections;

public class BirdSpawner : MonoBehaviour
{
    public GameObject birdPrefab;

    [Header("Spawn Timing")]
    public float minSpawnDelay = 10f;
    public float maxSpawnDelay = 25f;

    [Header("Flock Size")]
    public int minBirds = 3;
    public int maxBirds = 8;

    [Header("Movement")]
    public float minSpeed = 1.5f;
    public float maxSpeed = 3f;

    [Header("Height")]
    public float minHeight = 2f;
    public float maxHeight = 6f;

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        StartCoroutine(SpawnFlocks());
    }

    IEnumerator SpawnFlocks()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                Random.Range(minSpawnDelay, maxSpawnDelay));

            SpawnFlock();
        }
    }

    void SpawnFlock()
    {
        int birdCount = Random.Range(minBirds, maxBirds + 1);

        float screenWidth = cam.orthographicSize * cam.aspect;

        bool leftToRight = Random.value > 0.5f;

        float spawnY = Random.Range(minHeight, maxHeight);

        float spawnX = leftToRight
            ? cam.transform.position.x - screenWidth - 2f
            : cam.transform.position.x + screenWidth + 2f;

        int direction = leftToRight ? 1 : -1;

        float speed = Random.Range(minSpeed, maxSpeed);

        for (int i = 0; i < birdCount; i++)
        {
            Vector3 pos = new Vector3(
                spawnX + Random.Range(-0.5f, 0.5f),
                spawnY + Random.Range(-0.3f, 0.3f),
                0f);

            GameObject birdObj =
                Instantiate(birdPrefab, pos, Quaternion.identity);

            Bird bird = birdObj.GetComponent<Bird>();

            if (bird != null)
            {
                bird.Initialize(direction, speed);
            }
        }
    }
}