using UnityEngine;

public class BeybladeCollisionEffects : MonoBehaviour
{
    [Header("Particle Effect")]
    public ParticleSystem collisionEffectPrefab;   // assign in Inspector

    [Header("Impact Settings")]
    public float minImpactVelocity = 0.5f;         // ignore tiny bumps
    public float maxImpactVelocity = 10f;          // clamp intensity

    [Header("Optional Sound")]
    public AudioSource collisionSound;             // optional

    void OnCollisionEnter(Collision collision)
    {
        // Only trigger when hitting another Beyblade
        Transform otherRoot = collision.collider.transform.root;
        if (!otherRoot.CompareTag("Beyblade"))
            return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact < minImpactVelocity)
            return;

        // Spawn particles at the first contact point
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
}
