using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BeybladeColliderSetup : MonoBehaviour
{
    [Tooltip("Tip collider that should contact the arena")]
    public Collider tipCollider;

    [Tooltip("Body colliders that should ignore the arena")]
    public Collider[] bodyColliders;

    [Tooltip("Layer name used by the arena (set in Inspector)")]
    public string arenaLayerName = "Arena";

    [Tooltip("Optional physic material to apply to tip and body colliders")]
    public PhysicsMaterial lowFrictionMaterial;

    void Start()
    {
        // Apply physic material if provided
        if (lowFrictionMaterial != null)
        {
            if (tipCollider != null) tipCollider.material = lowFrictionMaterial;
            if (bodyColliders != null)
            {
                foreach (var c in bodyColliders) if (c != null) c.material = lowFrictionMaterial;
            }
        }

        // Resolve arena layer
        int arenaLayer = LayerMask.NameToLayer(arenaLayerName);
        if (arenaLayer == -1)
        {
            Debug.LogWarning("BeybladeColliderSetup: Arena layer not found: " + arenaLayerName);
            return;
        }

        // Find all colliders in the scene. Use the new API when available for better performance.
#if UNITY_2023_2_OR_NEWER
        Collider[] arenaColliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
#else
        Collider[] arenaColliders = FindObjectsOfType<Collider>();
#endif

        foreach (var ac in arenaColliders)
        {
            if (ac == null) continue;
            if (ac.gameObject.layer != arenaLayer) continue;

            // Tip should still collide with arena; do not ignore
            if (tipCollider != null)
            {
                Physics.IgnoreCollision(ac, tipCollider, false);
            }

            // Body colliders should ignore arena
            if (bodyColliders != null)
            {
                foreach (var bc in bodyColliders)
                {
                    if (bc != null) Physics.IgnoreCollision(ac, bc, true);
                }
            }
        }
    }
}