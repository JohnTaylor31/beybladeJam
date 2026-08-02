using UnityEngine;

public class BeybladeCorePhysics_Modular : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Collider tipCollider;
    public LayerMask arenaLayer;

    [Header("Spin")]
    public float initialSpin = 3000f;
    public float spinToMoveFactor = 0.15f;

    [Header("Feature Toggles")]
    public bool enableSlopeMovement = true;
    public bool enableCentrePull = true;
    public bool enableOpponentAttraction = false;
    public bool enableUprightTorque = false;
    public bool enableDamping = true;
    public bool enableKO = true;

    [Header("Movement")]
    public float landingRayDistance = 0.25f;
    public float contactRayDistance = 2f;
    public float centrePullStrength = 0.8f;
    public float attractionStrength = 1f;
    public Transform opponent;

    [Header("Damping")]
    public float linearDamping = 0.05f;
    public float angularDamping = 0.01f;

    [Header("KO")]
    public float minSpinToLive = 30f;
    public float koGraceSeconds = 0.5f;

    bool grounded = false;
    float koTimer = 0f;

    void Start()
    {
        if (!rb) rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxAngularVelocity = 4000f;

        rb.AddTorque(transform.up * initialSpin, ForceMode.VelocityChange);
    }

    void FixedUpdate()
    {
        UpdateGroundedState();

        if (!grounded)
        {
            if (enableUprightTorque)
                ApplyUprightTorqueAirborne();
            return;
        }

        if (enableSlopeMovement)
            ApplySlopeMovement();

        if (enableCentrePull)
            ApplyCentrePull();

        if (enableOpponentAttraction && opponent)
            ApplyOpponentAttraction();

        if (enableDamping)
            ApplyDamping();

        if (enableKO)
            CheckKO();
    }

    void UpdateGroundedState()
    {
        Vector3 origin = tipCollider ? tipCollider.transform.position : transform.position;
        grounded = Physics.Raycast(origin, -transform.up, landingRayDistance, arenaLayer);
    }

    void ApplySlopeMovement()
    {
        Vector3 origin = tipCollider ? tipCollider.transform.position : transform.position;
        RaycastHit hit;

        if (!Physics.Raycast(origin, -transform.up, out hit, contactRayDistance, arenaLayer))
            return;

        Vector3 normal = hit.normal;
        float spinUp = Vector3.Dot(rb.angularVelocity, transform.up);

        Vector3 tangent = Vector3.Cross(normal, transform.up).normalized;
        if (tangent.sqrMagnitude < 0.01f) tangent = transform.forward;

        Vector3 moveDir = tangent * Mathf.Sign(spinUp);
        float mag = Mathf.Abs(spinUp) * spinToMoveFactor;

        rb.AddForceAtPosition(moveDir * mag, hit.point, ForceMode.Acceleration);
    }

    void ApplyCentrePull()
    {
        Vector3 dir = -transform.position;
        rb.AddForce(dir.normalized * centrePullStrength, ForceMode.Acceleration);
    }

    void ApplyOpponentAttraction()
    {
        Vector3 dir = opponent.position - transform.position;
        rb.AddForce(dir.normalized * attractionStrength, ForceMode.Acceleration);
    }

    void ApplyUprightTorqueAirborne()
    {
        Vector3 correction = Vector3.Cross(transform.up, Vector3.up);
        rb.AddTorque(correction * angularDamping, ForceMode.Acceleration);
    }

    void ApplyDamping()
    {
        Vector3 vertical = Vector3.Project(rb.linearVelocity, transform.up);
        Vector3 lateral = Vector3.ProjectOnPlane(rb.linearVelocity, transform.up);
        rb.linearVelocity = vertical + lateral * (1f - linearDamping);
    }

    void CheckKO()
    {
        float spinUp = Mathf.Abs(Vector3.Dot(rb.angularVelocity, transform.up));

        if (spinUp < minSpinToLive)
        {
            koTimer += Time.fixedDeltaTime;
            if (koTimer >= koGraceSeconds)
            {
                // KO event here
            }
        }
        else koTimer = 0f;
    }
}
