using UnityEngine;
using UnityEngine.InputSystem;

public class BallSpawner : MonoBehaviour
{
    public GameObject ballPrefab;

    [Header("Spawn Settings")]
    public float spawnHeight = 50f;
    public float spawnRadius = 10f;

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SpawnBall();
        }
    }

    void SpawnBall()
    {
        // Random XZ around the spawner's own position
        Vector2 randomXZ = Random.insideUnitCircle * spawnRadius;

        Vector3 spawnPos = new Vector3(
            transform.position.x + randomXZ.x,
            transform.position.y + spawnHeight,
            transform.position.z + randomXZ.y
        );

        Instantiate(ballPrefab, spawnPos, Quaternion.identity);
    }
}
