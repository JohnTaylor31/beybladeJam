using UnityEngine;

/// <summary>
/// Minimal arena safety script. Attach to the arena root. Ensures the arena uses the correct layer
/// and that its MeshCollider is non-convex (concave) and not a trigger so dynamic blades collide correctly.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ArenaSetup : MonoBehaviour
{
    [Tooltip("Layer name to assign to the arena")]
    public string arenaLayerName = "Arena";

    void Reset()
    {
        // Try to auto-assign MeshCollider settings in editor
#if UNITY_EDITOR
        var mc = GetComponent<MeshCollider>();
        if (mc != null)
        {
            mc.convex = false;
            mc.isTrigger = false;
            UnityEditor.EditorUtility.SetDirty(mc);
        }
#endif
    }

    void Start()
    {
        int layer = LayerMask.NameToLayer(arenaLayerName);
        if (layer == -1)
        {
            Debug.LogWarning("ArenaSetup: Layer not found: " + arenaLayerName + ". Please create it in Project Settings > Tags and Layers.");
        }
        else
        {
            gameObject.layer = layer;
        }

        // Ensure collider is not set to trigger
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        // If there's a MeshCollider, ensure it's non-convex (concave) for static arena
        var meshCol = GetComponent<MeshCollider>();
        if (meshCol != null)
        {
            meshCol.convex = false;
        }
    }
}