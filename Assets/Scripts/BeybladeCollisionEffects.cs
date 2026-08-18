using UnityEngine;
using System.Collections;

public class BeybladeCollisionEffects : MonoBehaviour
{
    [Header("Particle Effect")]
    public ParticleSystem collisionEffectPrefab;   // assign in Inspector

    [Header("Impact Settings")]
    public float minImpactVelocity = 0.5f;         // ignore tiny bumps
    public float maxImpactVelocity = 10f;          // clamp intensity
    public float hitStopThreshold = 3f;            // impact velocity threshold for hit-stop
    public float hitStopDuration = 0.05f;          // duration of hit-stop in seconds
    public float hitStopTimeScale = 0.1f;          // time scale during hit-stop

    [Header("Burst Finish Settings")]
    public float burstThreshold = 8f;             // extremely high impact threshold for burst finish (rare)
    public ParticleSystem burstEffectPrefab;       // special VFX for burst finish
    public float cameraShakeIntensity = 2f;        // camera shake intensity
    public float cameraShakeDuration = 0.3f;       // camera shake duration

    [Header("Optional Sound")]
    public AudioSource collisionSound;             // optional

    private bool isHitStopActive = false;
    private Coroutine hitStopCoroutine;

    void OnCollisionEnter(Collision collision)
    {
        // Check if collision is with another Beyblade (using component instead of tag)
        BeybladeController otherBeyblade = collision.collider.GetComponentInParent<BeybladeController>();
        if (otherBeyblade == null)
            return;

        // Don't trigger if hitting self
        if (otherBeyblade == GetComponent<BeybladeController>())
            return;

        // Check if match is in KO state (don't do hit-stop during KO)
        if (MatchFlowManager.Instance != null && MatchFlowManager.Instance.CurrentState == MatchState.KO)
            return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact < minImpactVelocity)
            return;

        // Trigger hit-stop for high-impact collisions
        if (impact >= hitStopThreshold && !isHitStopActive)
        {
            TriggerHitStop();
        }

        // Check for burst finish (extremely high impact)
        if (impact >= burstThreshold)
        {
            TriggerBurstFinish(collision, impact, otherBeyblade);
            return; // Don't spawn normal effects for burst finish
        }

        // Spawn particles at the first contact point
        SpawnImpactEffects(collision, impact);
    }

    void SpawnImpactEffects(Collision collision, float impact)
    {
        if (collisionEffectPrefab != null)
        {
            ContactPoint contact = collision.GetContact(0);

            ParticleSystem ps = Instantiate(
                collisionEffectPrefab,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );

            // Scale intensity based on impact strength
            var main = ps.main;
            float t = Mathf.InverseLerp(minImpactVelocity, maxImpactVelocity, impact);
            main.startSizeMultiplier *= Mathf.Lerp(0.5f, 2f, t);
            main.startSpeedMultiplier *= Mathf.Lerp(0.5f, 2f, t);

            ps.Play();
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }

        // Optional sound
        if (collisionSound != null)
        {
            collisionSound.pitch = Random.Range(0.9f, 1.1f);
            collisionSound.Play();
        }
    }

    void TriggerHitStop()
    {
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
        }
        hitStopCoroutine = StartCoroutine(HitStopCoroutine());
    }

    IEnumerator HitStopCoroutine()
    {
        isHitStopActive = true;

        // Store original time scale
        float originalTimeScale = Time.timeScale;

        // Apply hit-stop time scale
        Time.timeScale = hitStopTimeScale;

        // Wait for hit-stop duration using unscaled time
        yield return new WaitForSecondsRealtime(hitStopDuration);

        // Restore original time scale
        Time.timeScale = originalTimeScale;

        isHitStopActive = false;
        hitStopCoroutine = null;
    }

    void OnDestroy()
    {
        // Clean up coroutine if object is destroyed
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
        }
        // Ensure time scale is restored
        if (isHitStopActive)
        {
            Time.timeScale = 1f;
        }
    }

    void TriggerBurstFinish(Collision collision, float impact, BeybladeController otherBeyblade)
    {
        Debug.Log($"BURST FINISH! Impact: {impact:F2}");

        // Spawn burst VFX at collision point
        if (burstEffectPrefab != null)
        {
            ContactPoint contact = collision.GetContact(0);
            ParticleSystem burstPs = Instantiate(
                burstEffectPrefab,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );

            // Scale burst based on impact intensity
            var main = burstPs.main;
            float intensityRatio = Mathf.InverseLerp(burstThreshold, maxImpactVelocity * 2f, impact);
            main.startSizeMultiplier *= Mathf.Lerp(1f, 3f, intensityRatio);
            main.startSpeedMultiplier *= Mathf.Lerp(1f, 2f, intensityRatio);

            burstPs.Play();
            Destroy(burstPs.gameObject, burstPs.main.duration + burstPs.main.startLifetime.constantMax);
        }
        else
        {
            // Fallback to collision effect if no burst effect assigned
            SpawnImpactEffects(collision, impact);
        }

        // Trigger camera shake
        TriggerCameraShake();

        // Instant KO the hit beyblade
        if (otherBeyblade != null)
        {
            // Use reflection to call the private HandleKO method
            var handleKOMethod = typeof(BeybladeController).GetMethod("HandleKO", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (handleKOMethod != null)
            {
                handleKOMethod.Invoke(otherBeyblade, null);
            }
            else
            {
                // Fallback: force KO through the KO system
                ForceKO(otherBeyblade);
            }
        }
    }

    void TriggerCameraShake()
    {
        // Find main camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = Object.FindFirstObjectByType<Camera>();
        }

        if (mainCamera != null)
        {
            StartCoroutine(CameraShakeCoroutine(mainCamera));
        }
    }

    IEnumerator CameraShakeCoroutine(Camera camera)
    {
        Vector3 originalPosition = camera.transform.position;
        float elapsed = 0f;

        while (elapsed < cameraShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            // Generate random shake offset
            float shakeAmount = cameraShakeIntensity * (1f - elapsed / cameraShakeDuration);
            Vector3 shakeOffset = new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount)
            );

            camera.transform.position = originalPosition + shakeOffset;

            yield return null;
        }

        // Restore original position
        camera.transform.position = originalPosition;
    }

    void ForceKO(BeybladeController target)
    {
        // Fallback KO method: directly access and modify KO state
        var isKOField = typeof(BeybladeController).GetField("isKO", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rbField = typeof(BeybladeController).GetField("rb", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (isKOField != null && rbField != null)
        {
            Rigidbody rb = (Rigidbody)rbField.GetValue(target);
            isKOField.SetValue(target, true);

            if (rb != null)
            {
                rb.angularDamping = Mathf.Max(rb.angularDamping, 1f);
                rb.linearDamping = Mathf.Max(rb.linearDamping, 1f);
            }

            // Notify GameMode
            if (GameMode.Instance != null)
            {
                var onBeybladeKOMethod = typeof(GameMode).GetMethod("OnBeybladeKO", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (onBeybladeKOMethod != null)
                {
                    onBeybladeKOMethod.Invoke(GameMode.Instance, new object[] { target });
                }
            }
        }
    }
}
