using UnityEngine;

public class BeybladeColliderSetup_Modular : MonoBehaviour
{
    [Header("Colliders")]
    public Collider tipCollider;
    public Collider middleCollider;
    public Collider topPartCollider;
    public Collider[] bladeColliders;

    [Header("Arena")]
    public LayerMask arenaLayer;

    [Header("Optional")]
    public PhysicsMaterial lowFrictionMaterial;

    void Start()
    {
        // Apply friction material
        if (lowFrictionMaterial != null)
        {
            if (tipCollider) tipCollider.material = lowFrictionMaterial;
            if (middleCollider) middleCollider.material = lowFrictionMaterial;
            if (topPartCollider) topPartCollider.material = lowFrictionMaterial;
            foreach (var bc in bladeColliders) if (bc) bc.material = lowFrictionMaterial;
        }

        // Find all arena colliders (Unity 2023+)
        Collider[] all = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);

        foreach (var c in all)
        {
            if (((1 << c.gameObject.layer) & arenaLayer) == 0)
                continue;

            // Tip collides with arena
            if (tipCollider)
                Physics.IgnoreCollision(c, tipCollider, false);

            // Middle + top + blades ignore arena
            if (middleCollider)
                Physics.IgnoreCollision(c, middleCollider, true);

            if (topPartCollider)
                Physics.IgnoreCollision(c, topPartCollider, true);

            foreach (var bc in bladeColliders)
                if (bc) Physics.IgnoreCollision(c, bc, true);
        }
    }
}
