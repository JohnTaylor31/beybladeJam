using UnityEngine;

[CreateAssetMenu(fileName = "BeybladeRing", menuName = "Beyblade/Parts/Ring")]
public class BeybladeRing : ScriptableObject
{
    [Header("Movement Stats")]
    [Tooltip("Spin to movement factor modifier")]
    public float spinToMoveModifier = 1f;
    
    [Tooltip("Center pull strength modifier")]
    public float centerPullModifier = 1f;
    
    [Tooltip("Orbit force modifier")]
    public float orbitForceModifier = 1f;
    
    [Header("Defense Stats")]
    [Tooltip("Knockback resistance (higher = harder to push)")]
    public float knockbackResistance = 1f;
    
    [Tooltip("Burst defense (higher = harder to burst finish)")]
    public float burstDefense = 1f;
    
    [Header("Description")]
    [TextArea(2, 4)]
    public string description = "Ring component affects movement patterns and defense.";
}
