using UnityEngine;

[CreateAssetMenu(fileName = "BeybladeCore", menuName = "Beyblade/Parts/Core")]
public class BeybladeCore : ScriptableObject
{
    [Header("Core Stats")]
    [Tooltip("Multiplier for base mass")]
    public float massMultiplier = 1f;
    
    [Tooltip("Base spin speed multiplier")]
    public float spinMultiplier = 1f;
    
    [Tooltip("Stamina decay modifier (lower = better stamina)")]
    public float staminaDecayModifier = 1f;
    
    [Tooltip("Stability modifier (affects wobble resistance)")]
    public float stabilityModifier = 1f;
    
    [Header("Combat Stats")]
    [Tooltip("Impact force multiplier")]
    public float impactForceMultiplier = 1f;
    
    [Tooltip("Spin loss on hit modifier (lower = retains more spin)")]
    public float spinLossOnHitModifier = 1f;
    
    [Header("Description")]
    [TextArea(2, 4)]
    public string description = "Core component affects base stats and stability.";
}
