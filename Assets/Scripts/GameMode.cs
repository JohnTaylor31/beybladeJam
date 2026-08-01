using UnityEngine;

public class GameMode : MonoBehaviour
{
    public static GameMode Instance { get; private set; }
    public GameObject beybladePrefab;
    public Transform arenaCenter;
    public float spawnRadius = 3f;
    public float spawnHeight = 0.1f;
    public float restartDelay = 2f;

    GameObject a, b;

    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    void Start() { StartMatch(); }

    public void StartMatch()
    {
        if (a != null) Destroy(a);
        if (b != null) Destroy(b);
        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        a = Spawn(center, 0f, "A");
        b = Spawn(center, 180f, "B");
        var ca = a.GetComponent<BeybladeController>();
        var cb = b.GetComponent<BeybladeController>();
        if (ca != null && cb != null) { ca.opponent = cb; cb.opponent = ca; }
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