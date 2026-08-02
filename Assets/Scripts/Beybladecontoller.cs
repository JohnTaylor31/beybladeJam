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
    public float fallbackSpinDecay = 0.02f;     // spin decay per second
    public float fallbackBodyContactDecay = 0.5f; // spin decay when body collider touches arena
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

    [Header("Friction blending")]
    public float highSpinThreshold = 500f;      // spin speed above this = high slip
    public float lowSpinThreshold = 100f;       // spin speed below this = high grip

    // runtime
    Rigidbody rb;
    bool isKO = false;
    float koTimer = 0f;

    // landing state
    bool hasLanded = false;
    float landingBlend = 0f;

    // spinSpeed: tracks rotation magnitude, only decreases, never reverses
    public float spinSpeed = 0f;

    // cached tuning
    float initialSpin;
    float spinToMoveFactor;
    float centerPullStrength;
    float spinDecayPerSecond;
    float bodyContactSpinDecay;
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
            spinDecayPerSecond = stats.staminaDecayPerSecond;
            bodyContactSpinDecay = stats.staminaDecayPerSecond * 25f; // extra decay on body contact
            koGraceSeconds = stats.koGraceSeconds;
            rb.mass = baseMass * Mathf.Max(0.01f, stats.massMultiplier);
        }
        else
        {
            initialSpin = fallbackInitialSpin;
            spinToMoveFactor = fallbackSpinToMove;
            centerPullStrength = fallbackCenterPull;
            spinDecayPerSecond = fallbackSpinDecay;
            bodyContactSpinDecay = fallbackBodyContactDecay;
            koGraceSeconds = fallbackKoGrace;
            rb.mass = baseMass;
        }

        // Initialize spinSpeed to initial rotation
        spinSpeed = initialSpin;

        // Rigidbody basic tuning
        rb.centerOfMass = centerOfMassOffset;
        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxAngularVelocity = 4000f;
        Physics.defaultMaxAngularSpeed = 4000f;

        // Gravity scale must be 1.0
        rb.useGravity = true;

        // Apply initial spin around local up (driven by spinSpeed)
        rb.angularVelocity = transform.up * spinSpeed;

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

        // Check if beyblade has left the arena
        if (HasLeftArena())
        {
            HandleKO();
            return;
        }

        // Apply spin decay (natural friction loss)
        spinSpeed = Mathf.Max(0f, spinSpeed - spinDecayPerSecond * Time.fixedDeltaTime);

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
                // still airborne: drive angular velocity from spinSpeed, do not apply lateral forces
                rb.angularVelocity = transform.up * spinSpeed;
                return;
            }
        }

        // Blend in lateral forces smoothly after landing
        landingBlend = Mathf.Min(1f, landingBlend + Time.fixedDeltaTime / Mathf.Max(0.0001f, landingBlendTime));

        // Check if body colliders are touching the arena (extra friction/decay)
        float extraSpinDecay = 0f;
        if (bodyColliders != null && bodyColliders.Length > 0)
        {
            foreach (Collider bodyCol in bodyColliders)
            {
                if (bodyCol == null) continue;
                Collider[] hits = Physics.OverlapSphere(bodyCol.bounds.center, bodyCol.bounds.extents.magnitude * 0.5f, arenaLayer);
                if (hits.Length > 0)
                {
                    extraSpinDecay = bodyContactSpinDecay;
                    break;
                }
            }
        }

        // Apply extra decay from body contact
        spinSpeed = Mathf.Max(0f, spinSpeed - extraSpinDecay * Time.fixedDeltaTime);

        // Determine contact normal under tip (so movement follows slope)
        Vector3 contactPoint = (tipCollider != null) ? tipCollider.transform.position : transform.position;
        RaycastHit contactHit;
        Vector3 surfaceNormal = Vector3.up;
        if (Physics.Raycast(contactPoint, -transform.up, out contactHit, contactRayDistance, arenaLayer))
        {
            surfaceNormal = contactHit.normal;
            contactPoint = contactHit.point;
        }

        // Determine friction based on spinSpeed (high spin = slip, low spin = grip)
        float frictionLerp = Mathf.InverseLerp(lowSpinThreshold, highSpinThreshold, spinSpeed);
        float slipFactor = Mathf.Lerp(0.2f, 0.8f, frictionLerp); // 0.2 = high grip, 0.8 = high slip

        // Tangent direction for lateral movement
        Vector3 tangent = Vector3.Cross(surfaceNormal, transform.up).normalized;
        if (tangent.sqrMagnitude < 1e-6f) tangent = transform.forward;
        Vector3 moveDir = tangent; // direction, will be scaled by slip

        // Compute lateral force from spin (scaled by landingBlend and slip factor)
        float slopeFactor = 1f + (1f - Vector3.Dot(surfaceNormal, Vector3.up)) * 1.5f;
        float rawMag = spinSpeed * spinToMoveFactor * slopeFactor * landingBlend * slipFactor;
        float clampedMag = Mathf.Clamp(rawMag, 0f, 200f);
        Vector3 lateralForce = moveDir * clampedMag;
        rb.AddForceAtPosition(lateralForce, contactPoint, ForceMode.Acceleration);

        // Lateral damping based on grip (high spin = less damping, low spin = more damping)
        float gripFactor = 1f - slipFactor; // inverse of slip
        Vector3 vertical = Vector3.Project(rb.linearVelocity, transform.up);
        Vector3 lateral = Vector3.ProjectOnPlane(rb.linearVelocity, transform.up);
        rb.linearVelocity = vertical + lateral * (0.96f + gripFactor * 0.03f);

        // Gentle pull toward opponent or arena center (less pull at low spin)
        Vector3 target = (opponent != null) ? opponent.transform.position : Vector3.zero;
        Vector3 dir = (target - transform.position);
        if (dir.sqrMagnitude > 0.01f)
        {
            float attractionScale = Mathf.Clamp01(spinSpeed / highSpinThreshold); // less attraction at low spin
            Vector3 attraction = dir.normalized * (centerPullStrength / Mathf.Max(1f, dir.magnitude)) * landingBlend * attractionScale;
            rb.AddForce(attraction, ForceMode.Acceleration);
        }

        // Drive angular velocity from spinSpeed (no upright stabilization torque)
        rb.angularVelocity = transform.up * spinSpeed;

        // KO detection: spinSpeed reaches zero OR beyblade left arena
        if (spinSpeed <= 0f)
        {
            koTimer += Time.fixedDeltaTime;
            if (koTimer >= koGraceSeconds) HandleKO();
        }
        else koTimer = 0f;
    }

    bool HasLeftArena()
    {
        // Simple check: if tip is too far below arena or outside a large boundary
        if (tipCollider != null)
        {
            Vector3 tipPos = tipCollider.transform.position;
            // Check if tip has gone too low (fell out bottom)
            if (tipPos.y < -50f) return true;
            // Check if too far horizontally (customize based on your arena)
            if (new Vector2(tipPos.x, tipPos.z).magnitude > 100f) return true;
        }
        return false;
    }

    void HandleKO()
    {
        if (isKO) return;
        isKO = true;
        rb.angularDamping = Mathf.Max(rb.angularDamping, 1f);
        rb.linearDamping = Mathf.Max(rb.linearDamping, 1f);
        GameMode.Instance?.OnBeybladeKO(this);
    }

    // External spin add (e.g., player input) - only increases spinSpeed, never reverses
    public void AddSpin(float spinAmount)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        // Only add spin if amount is positive, never reverse
        if (spinAmount > 0f)
        {
            spinSpeed += spinAmount;
            rb.angularVelocity = transform.up * spinSpeed;
            isKO = false;
            koTimer = 0f;
            hasLanded = false;
        }
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