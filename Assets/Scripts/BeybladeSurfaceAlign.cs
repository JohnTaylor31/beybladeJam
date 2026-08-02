using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BeybladeSurfaceAlign : MonoBehaviour
{
    [Header("Alignment Settings")]
    public float alignSpeed = 10f;
    public float maxTiltAngle = 20f;
    public float contactRayDistance = 0.3f; // one distance, used for both grounded check and alignment

    [Header("References")]
    public Collider tipCollider;
    public LayerMask arenaLayer;

    Rigidbody rb;
    bool isGrounded = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 origin = (tipCollider != null) ? tipCollider.transform.position : transform.position;

        if (Physics.Raycast(origin, -transform.up, out RaycastHit hit, contactRayDistance, arenaLayer))
        {
            isGrounded = true;
            AlignToSurfaceNormal(hit.normal);
        }
        else
        {
            isGrounded = false;
        }
    }

    void AlignToSurfaceNormal(Vector3 surfaceNormal)
    {
        float angle = Vector3.Angle(surfaceNormal, Vector3.up);
        if (angle > maxTiltAngle)
        {
            surfaceNormal = Vector3.RotateTowards(
                surfaceNormal,
                Vector3.up,
                Mathf.Deg2Rad * (angle - maxTiltAngle),
                0f
            );
        }

        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, surfaceNormal) * transform.rotation;
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * alignSpeed));
    }
}