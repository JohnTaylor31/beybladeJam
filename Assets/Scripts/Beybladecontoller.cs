using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BeybladeController : MonoBehaviour
{
    [Header("References")]
    public Collider tipCollider;                // SphereCollider for tip (assign)
    public Collider[] bodyColliders;            // Rim/body colliders (assign)
    public LayerMask arenaLayer;                // set to Arena layer mask in Inspector
    public BeybladeController opponent;         // set by GameMode (optional)

    [Header("Optional Stats")]
    public BeybladeStats stats;                 // optional ScriptableObject; if null, fallback values used

    [Header("Fallback tuning")]
    public float fallbackInitialSpin = 3000f;
    public float fallbackSpinToMove = 0.15f;
    public float fallbackCenterPull = 0.8f;
    public float fallbackMinSpinToLive = 30f;
    public float fallbackKoGrace = 0.5f;

    [Header("Rigidbody tuning")]
    public float baseMass = 200f;               // base mass; multiplied by stats.massMultiplier if provided
    public Vector3 centerOfMassOffset = new Vector3(0f, -10f, 0f); // tuned for large scale setups

    [Header("Stability")]
    public float linearDamping = 0.05f;
    public float angularDamping = 0.01f;
    public float contactRayDistance = 2f;

    [Header("Landing / Launch")]
    public float launchImpulse = 1f;            // small horizontal impulse applied at tip (tune or set to 0)
    public float landingRayDistance = 0.25f;    // short ray to detect tip contact
    public float landingBlendTime = 0.12f;      // smooth in lateral forces after landing

    [Header("Upright stabilization")]
    public float uprightTorqueBase = 40f;       // base upright torque
    public float uprightTorqueSpinScale = 0.02f;// additional torque per unit spin

    // runtime
    Rigidbody rb;
    bool isKO = false;
    float koTimer = 0f;

    // landing state
    bool hasLanded = false;
    float landingBlend = 0f;

    // cached tuning
    float initialSpin;
    float spinToMoveFactor;
    float centerPullStrength;
    float minSpinToLive;
    float koGraceSeconds;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Load tuning from stats or fallback
        if (stats != null)
        {
            initialSpin = stats.initialSpin;
            spinToMoveFactor = stats.spinToMoveFactor;
            centerPullStrength = stats.centerPullStrength;
            minSpinToLive = stats.minSpinToLive;
            koGraceSeconds = stats.koGraceSeconds;
            rb.mass = baseMass * Mathf.Max(0.01f, stats.massMultiplier);
        }
        else
        {
            initialSpin = fallbackInitialSpin;
            spinToMoveFactor = fallbackSpinToMove;
            centerPullStrength = fallbackCenterPull;
            minSpinToLive = fallbackMinSpinToLive;
            koGraceSeconds = fallbackKoGrace;
            rb.mass = baseMass;
        }

        // Rigidbody basic tuning
        rb.centerOfMass = centerOfMassOffset;
        rb.linearDamping = 0f;
        rb.angularDamping = angularDamping;
        rb.linearDamping = linearDamping;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxAngularVelocity = 4000f;
        Physics.defaultMaxAngularSpeed = 4000f;

        // Apply clean spin only around local up
        rb.AddTorque(transform.up * initialSpin, ForceMode.VelocityChange);

        // Small horizontal impulse at tip (no vertical component)
        if (tipCollider != null && launchImpulse > 0f)
        {
            Vector3 horizontalForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            rb.AddForceAtPosition(horizontalForward * launchImpulse, tipCollider.transform.position, ForceMode.Impulse);
        }

        // Increase solver iterations for more stable impacts
        Physics.defaultSolverIterations = Mathf.Max(Physics.defaultSolverIterations, 12);
        Physics.defaultSolverVelocityIterations = Mathf.Max(Physics.defaultSolverVelocityIterations, 4);
    }

    void FixedUpdate()
    {
        if (isKO) return;

        // If not landed yet, only allow spin; detect landing with a short raycast from the tip
        if (!hasLanded)
        {
            Vector3 origin = (tipCollider != null) ? tipCollider.transform.position : transform.position;
            RaycastHit hit;
            if (Physics.Raycast(origin, -transform.up, out hit, landingRayDistance, arenaLayer))
            {
                hasLanded = true;
                landingBlend = 0f;
                // remove vertical velocity component to avoid bounce
                Vector3 v = rb.linearVelocity;
                rb.linearVelocity = Vector3.ProjectOnPlane(v, transform.up);
            }
            else
            {
                // still airborne: do not apply lateral forces or attraction
                return;
            }
        }

        // Blend in lateral forces smoothly after landing
        landingBlend = Mathf.Min(1f, landingBlend + Time.fixedDeltaTime / Mathf.Max(0.0001f, landingBlendTime));

        // Determine contact normal under tip (so movement follows slope)
        Vector3 contactPoint = (tipCollider != null) ? tipCollider.transform.position : transform.position;
        RaycastHit contactHit;
        Vector3 surfaceNormal = Vector3.up;
        if (Physics.Raycast(contactPoint, -transform.up, out contactHit, contactRayDistance, arenaLayer))
        {
            surfaceNormal = contactHit.normal;
            contactPoint = contactHit.point;
        }

        // Spin along local up
        float spinAlongUp = Vector3.Dot(rb.angularVelocity, transform.up);

        // Tangent direction for lateral movement
        Vector3 tangent = Vector3.Cross(surfaceNormal, transform.up).normalized;
        if (tangent.sqrMagnitude < 1e-6f) tangent = transform.forward;
        Vector3 moveDir = tangent * Mathf.Sign(spinAlongUp);

        // Compute lateral force from spin (scaled by landingBlend)
        float slopeFactor = 1f + (1f - Vector3.Dot(surfaceNormal, Vector3.up)) * 1.5f;
        float rawMag = Mathf.Abs(spinAlongUp) * spinToMoveFactor * slopeFactor * landingBlend;
        float clampedMag = Mathf.Clamp(rawMag, 0f, 200f);
        Vector3 lateralForce = moveDir * clampedMag;
        rb.AddForceAtPosition(lateralForce, contactPoint, ForceMode.Acceleration);

        // Gentle lateral damping (preserve vertical velocity)
        Vector3 vertical = Vector3.Project(rb.linearVelocity, transform.up);
        Vector3 lateral = Vector3.ProjectOnPlane(rb.linearVelocity, transform.up);
        rb.linearVelocity = vertical + lateral * 0.96f;

        // Gentle pull toward opponent or arena center
        Vector3 target = (opponent != null) ? opponent.transform.position : Vector3.zero;
        Vector3 dir = (target - transform.position);
        if (dir.sqrMagnitude > 0.01f)
        {
            Vector3 attraction = dir.normalized * (centerPullStrength / Mathf.Max(1f, dir.magnitude)) * landingBlend;
            rb.AddForce(attraction, ForceMode.Acceleration);
        }

        // Upright stabilization torque scaled by spin magnitude
        float spinMag = Mathf.Abs(spinAlongUp);
        Vector3 correction = Vector3.Cross(transform.up, Vector3.up);
        float torque = uprightTorqueBase + spinMag * uprightTorqueSpinScale;
        rb.AddTorque(correction * torque, ForceMode.Acceleration);

        // KO detection
        if (Mathf.Abs(spinAlongUp) < minSpinToLive)
        {
            koTimer += Time.fixedDeltaTime;
            if (koTimer >= koGraceSeconds) HandleKO();
        }
        else koTimer = 0f;
    }

    void HandleKO()
    {
        if (isKO) return;
        isKO = true;
        rb.angularDamping = Mathf.Max(rb.angularDamping, 1f);
        rb.linearDamping = Mathf.Max(rb.linearDamping, 1f);
        GameMode.Instance?.OnBeybladeKO(this);
    }

    // External spin add (e.g., player input)
    public void AddSpin(float spinAmount)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.AddTorque(transform.up * spinAmount, ForceMode.VelocityChange);
        isKO = false;
        koTimer = 0f;
        // allow re-landing behavior if needed
        hasLanded = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (rb != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(rb.centerOfMass), 0.5f);
        }
        if (tipCollider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(tipCollider.transform.position, 0.5f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(tipCollider.transform.position, tipCollider.transform.position - transform.up * landingRayDistance);
        }
    }
#endif
}