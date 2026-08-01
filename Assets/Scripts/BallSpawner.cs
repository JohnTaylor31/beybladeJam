using UnityEngine;
using UnityEngine.InputSystem;

public class BallSpawner : MonoBehaviour
{
    public GameObject ballPrefab;

    [Header("Arena Switching")]
    public Transform[] arenas;   // assign your arena GameObjects here
    private int currentArena = 0;

    [Header("Camera")]
    public Camera mainCamera;
    public Vector3 cameraOffset = new Vector3(0, 8, -20);

    [Header("Spawn Settings")]
    public float spawnHeight = 5f;
    public float spawnRadius = 10f;

    void Update()
    {
        // Switch arena with TAB
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            currentArena = (currentArena + 1) % arenas.Length;
            MoveCameraToArena();
        }

        // Spawn ball with SPACE
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SpawnBall();
        }
    }

    void MoveCameraToArena()
    {
        Transform arena = arenas[currentArena];

        // Move camera to arena + offset
        mainCamera.transform.position = arena.position + cameraOffset;

        // Make camera look at arena centre
        mainCamera.transform.LookAt(arena.position);
    }

    void SpawnBall()
    {
        Transform arena = arenas[currentArena];

        Vector2 randomXZ = Random.insideUnitCircle * spawnRadius;

        Vector3 spawnPos = new Vector3(
            arena.position.x + randomXZ.x,
            arena.position.y + spawnHeight,
            arena.position.z + randomXZ.y
        );

        Instantiate(ballPrefab, spawnPos, Quaternion.identity);
    }
}
