using UnityEngine;

public class BeybladeSurfaceAlign_Modular : MonoBehaviour
{
    public float alignSpeed = 10f;
    public float maxTiltAngle = 20f;
    public float raycastDistance = 2f;

    public Collider tipCollider;
    public LayerMask arenaLayer;

    Rigidbody rb;
    bool grounded = false;
    float groundedCheckDistance = 0.3f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        UpdateGroundedState();
        if (!grounded) return;
        AlignToSurface();
    }

    void UpdateGroundedState()
    {
        Vector3 origin = tipCollider ? tipCollider.transform.position : transform.position;
        grounded = Physics.Raycast(origin, -transform.up, groundedCheckDistance, arenaLayer);
    }

    void AlignToSurface()
    {
        Vector3 origin = tipCollider ? tipCollider.transform.position : transform.position;
        RaycastHit hit;

        if (Physics.Raycast(origin, -transform.up, out hit, raycastDistance, arenaLayer))
        {
            Vector3 normal = hit.normal;

            float angle = Vector3.Angle(normal, Vector3.up);
            if (angle > maxTiltAngle)
            {
                normal = Vector3.RotateTowards(normal, Vector3.up,
                    Mathf.Deg2Rad * (angle - maxTiltAngle), 0f);
            }

            Quaternion target =
                Quaternion.FromToRotation(transform.up, normal) * transform.rotation;

            rb.MoveRotation(
                Quaternion.Slerp(rb.rotation, target, Time.fixedDeltaTime * alignSpeed)
            );
        }
    }
}
