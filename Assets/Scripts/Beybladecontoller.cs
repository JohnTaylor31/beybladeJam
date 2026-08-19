using UnityEngine;
using UnityEngine.InputSystem;

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
    public float fallbackAssistSpinCost = 0.5f; // spin cost per second while using assist
    public float fallbackAssistForce = 50f;     // lateral force magnitude for assist

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
    [Range(0f, 1f)] public float launchPower = 1f;
    public ParticleSystem launchBurstPrefab;

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
    float assistSpinCost;
    float assistForce;
    bool _launchReady;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        LoadStats();

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

        _launchReady = true;
        ApplyLaunchPower(launchPower);

        // Increase solver iterations for more stable impacts
        Physics.defaultSolverIterations = Mathf.Max(Physics.defaultSolverIterations, 12);
        Physics.defaultSolverVelocityIterations = Mathf.Max(Physics.defaultSolverVelocityIterations, 4);
    }

    void LoadStats()
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
            assistSpinCost = stats.assistSpinCost;
            assistForce = stats.assistForce;
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
            assistSpinCost = fallbackAssistSpinCost;
            assistForce = fallbackAssistForce;
            rb.mass = baseMass;
        }
    }

    public void ReloadStats()
    {
        LoadStats();
    }

    public void ApplyLaunchPower(float power)
    {
        launchPower = Mathf.Clamp01(power);
        if (!_launchReady || rb == null)
            return;

        spinSpeed = initialSpin * launchPower;
        rb.angularVelocity = transform.up * spinSpeed;

        if (tipCollider != null && launchImpulse > 0f)
        {
            Vector3 horizontalForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            rb.AddForceAtPosition(horizontalForward * launchImpulse * launchPower, tipCollider.transform.position, ForceMode.Impulse);
        }

        PlayLaunchBurst();
    }

    void PlayLaunchBurst()
    {
        ParticleSystem prefab = launchBurstPrefab;
        if (prefab == null)
        {
            BeybladeCollisionEffects collisionFx = GetComponent<BeybladeCollisionEffects>();
            if (collisionFx != null)
                prefab = collisionFx.collisionEffectPrefab;
        }

        Vector3 pos = transform.position + Vector3.up * 0.6f;
        float power = Mathf.Clamp01(launchPower);

        if (prefab != null)
        {
            ParticleSystem ps = Instantiate(prefab, pos, Quaternion.Euler(-90f, 0f, 0f));
            var main = ps.main;
            main.loop = false;
            main.startSizeMultiplier *= Mathf.Lerp(0.5f, 1.8f, power);
            main.startSpeedMultiplier *= Mathf.Lerp(0.5f, 2f, power);
            ps.Play();
            Destroy(ps.gameObject, main.duration + 0.5f);
            return;
        }

        GameObject go = new GameObject("LaunchBurst");
        go.transform.position = pos;
        ParticleSystem fallback = go.AddComponent<ParticleSystem>();
        var fbMain = fallback.main;
        fbMain.loop = false;
        fbMain.playOnAwake = false;
        fbMain.startLifetime = 0.4f;
        fbMain.startSpeed = Mathf.Lerp(5f, 16f, power);
        fbMain.startSize = Mathf.Lerp(0.12f, 0.4f, power);
        fbMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.7f, 0.15f), new Color(1f, 0.35f, 0.05f));
        fbMain.gravityModifier = 0.35f;
        fbMain.maxParticles = 64;
        fbMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = fallback.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(Mathf.Lerp(10f, 42f, power))) });

        var shape = fallback.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 28f;
        shape.radius = 0.15f;

        ParticleSystemRenderer psRenderer = go.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
            psRenderer.material = new Material(shader);

        fallback.Play();
        Destroy(go, 1.5f);
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

        // Apply directional assist (only during Playing state and after landing)
        if (hasLanded && IsPlayingState())
        {
            ApplyDirectionalAssist();
        }

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

    bool IsPlayingState()
    {
        if (MatchFlowManager.Instance == null)
            return true; // Fallback: allow assist if no flow manager
        return MatchFlowManager.Instance.CurrentState == MatchState.Playing;
    }

    void ApplyDirectionalAssist()
    {
        // Read input from WASD or left stick
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
        }

        if (Gamepad.current != null)
        {
            Vector2 stickInput = Gamepad.current.leftStick.ReadValue();
            if (stickInput.magnitude > 0.1f)
                input = stickInput;
        }

        // Normalize input and check if there's meaningful input
        if (input.magnitude < 0.1f)
            return;

        input = input.normalized;

        // Convert 2D input to 3D world space direction
        Vector3 worldDirection = new Vector3(input.x, 0f, input.y);

        // Project onto arena surface using current surface normal
        Vector3 contactPoint = (tipCollider != null) ? tipCollider.transform.position : transform.position;
        RaycastHit contactHit;
        Vector3 surfaceNormal = Vector3.up;
        if (Physics.Raycast(contactPoint, -transform.up, out contactHit, contactRayDistance, arenaLayer))
        {
            surfaceNormal = contactHit.normal;
        }

        // Project direction onto the surface plane
        Vector3 surfaceDirection = Vector3.ProjectOnPlane(worldDirection, surfaceNormal).normalized;
        if (surfaceDirection.sqrMagnitude < 1e-6f)
            return;

        // Reduce assist strength when spin is low
        float spinFactor = Mathf.Clamp01(spinSpeed / lowSpinThreshold);

        // Spin-scaled assist force
        Vector3 assistForceVector = surfaceDirection * assistForce * spinFactor;

        rb.AddForceAtPosition(assistForceVector, contactPoint, ForceMode.Acceleration);

        // Apply spin cost for using assist
        spinSpeed = Mathf.Max(0f, spinSpeed - assistSpinCost * Time.fixedDeltaTime);

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