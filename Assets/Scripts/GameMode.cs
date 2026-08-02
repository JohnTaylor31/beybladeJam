using UnityEngine;

public class GameMode : MonoBehaviour
{
    public static GameMode Instance { get; private set; }
    public GameObject beybladePrefab;
    public Transform arenaCenter;
    public float spawnRadius = 3f;
    public float spawnHeight = 0.1f;
    public float restartDelay = 2f;

    [Header("Inspector Spawning")]
    public GameObject playerBeyblade;
    public GameObject opponentBeyblade;
    public bool useInspectorSpawn = false;

    GameObject _a;
    GameObject _b;
    public GameObject a { get { return _a; } private set { _a = value; } }
    public GameObject b { get { return _b; } private set { _b = value; } }

    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    void Start() { StartMatch(); }

    public void StartMatch()
    {
        // Always clean up old beyblades (both stopped ones and active ones)
        CleanupBeyblades();

        if (useInspectorSpawn && playerBeyblade != null && opponentBeyblade != null)
        {
            // Check if the assigned beyblades are actually in the scene (not prefabs)
            // If they are scene instances, use them directly
            if (playerBeyblade.scene.isLoaded && opponentBeyblade.scene.isLoaded)
            {
                a = playerBeyblade;
                b = opponentBeyblade;
            }
            else
            {
                // They are prefabs, so instantiate them
                Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;
                a = Instantiate(playerBeyblade, center + Quaternion.Euler(0, 0f, 0) * Vector3.right * spawnRadius + Vector3.up * spawnHeight, Quaternion.identity);
                b = Instantiate(opponentBeyblade, center + Quaternion.Euler(0, 180f, 0) * Vector3.right * spawnRadius + Vector3.up * spawnHeight, Quaternion.identity);
                a.name = "Beyblade_A";
                b.name = "Beyblade_B";
            }
        }
        else
        {
            // Spawn new beyblades using the prefab
            Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;
            a = Spawn(center, 0f, "A");
            b = Spawn(center, 180f, "B");
        }

        var ca = a.GetComponent<BeybladeController>();
        var cb = b.GetComponent<BeybladeController>();
        if (ca != null && cb != null) { ca.opponent = cb; cb.opponent = ca; }
    }

    void CleanupBeyblades()
    {
        // Destroy all Beyblades in the scene (both old spawned ones and stopped ones)
        BeybladeController[] allBeyblades = Object.FindObjectsByType<BeybladeController>(FindObjectsSortMode.None);
        foreach (BeybladeController beyblade in allBeyblades)
        {
            Destroy(beyblade.gameObject);
        }

        a = null;
        b = null;
    }

    GameObject Spawn(Vector3 center, float angle, string id)
    {
        Vector3 pos = center + Quaternion.Euler(0, angle, 0) * Vector3.right * spawnRadius + Vector3.up * spawnHeight;
        var go = Instantiate(beybladePrefab, pos, Quaternion.identity);
        go.name = "Beyblade_" + id;
        return go;
    }

    public void OnBeybladeKO(BeybladeController ko)
    {
        Invoke(nameof(Restart), restartDelay);
    }

    void Restart() { StartMatch(); }
}