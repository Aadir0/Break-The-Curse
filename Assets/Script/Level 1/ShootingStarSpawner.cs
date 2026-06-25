using UnityEngine;
using System.Collections;

public class ShootingStarSpawner : MonoBehaviour
{
    public ParticleSystem star;

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnStar();

            yield return new WaitForSeconds(
                Random.Range(2f, 6f)
            );
        }
    }

    void SpawnStar()
    {
        Vector3 pos = Camera.main.ViewportToWorldPoint(
            new Vector3(Random.value, 1.1f, 0)
        );

        pos.z = 0;

        star.transform.position = pos;

        star.transform.rotation =
            Quaternion.Euler(0, 0, Random.Range(-150f, -120f));

        star.Emit(1);
    }
}