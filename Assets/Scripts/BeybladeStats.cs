using UnityEngine;

[CreateAssetMenu(fileName = "BeybladeStats", menuName = "Beyblade/Stats")]
public class BeybladeStats : ScriptableObject
{
    [Header("Core")]
    public float initialSpin = 3000f;          // initial angular velocity applied as torque
    public float spinToMoveFactor = 0.15f;       // converts spin to lateral force

    [Header("Mass")]
    public float massMultiplier = 1f;            // multiplies base Rigidbody.mass (useful for presets)

    [Header("Decay")]
    [Tooltip("Fraction lost per second (0.02 = 2%/s)")]
    public float staminaDecayPerSecond = 0.02f;
    [Tooltip("Fraction lost per second for lateral speed")]
    public float speedDecayPerSecond = 0.01f;

    [Header("Combat")]
    public float impactForce = 10f;              // impulse applied on hit (optional)
    [Range(0.5f, 1f)]
    public float spinLossOnHit = 0.95f;          // multiply spin by this on hit

    [Header("Movement")]
    public float orbitForce = 3f;                // optional orbiting force
    public float centerPullStrength = 0.8f;      // pull toward center or opponent

    [Header("KO")]
    public float minSpinToLive = 30f;
    public float koGraceSeconds = 0.5f;
}