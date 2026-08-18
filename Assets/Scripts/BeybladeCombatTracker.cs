using UnityEngine;

/// <summary>
/// Reports blade-vs-blade hits to MatchFlowManager. Counts each pair once
/// (lower instance ID reports) so both rigidbodies do not double-count.
/// </summary>
public class BeybladeCombatTracker : MonoBehaviour
{
    BeybladeController _self;

    void Awake()
    {
        _self = GetComponent<BeybladeController>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_self == null || collision.collider == null)
            return;

        BeybladeController other = collision.collider.GetComponentInParent<BeybladeController>();
        if (other == null || other == _self)
            return;

        if (_self.GetInstanceID() > other.GetInstanceID())
            return;

        MatchFlowManager.Instance?.RecordCollision(collision.relativeVelocity.magnitude);
    }
}
