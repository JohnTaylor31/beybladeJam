using UnityEngine;

[CreateAssetMenu(fileName = "BeybladeTip", menuName = "Beyblade/Parts/Tip")]
public class BeybladeTip : ScriptableObject
{
    [Header("Friction Stats")]
    [Tooltip("Friction coefficient modifier (affects grip vs slip)")]
    public float frictionModifier = 1f;
    
    [Tooltip("Grip at high spin speeds")]
    public float highSpinGrip = 0.2f;
    
    [Tooltip("Grip at low spin speeds")]
    public float lowSpinGrip = 0.8f;
    
    [Header("Movement Stats")]
    [Tooltip("Agility modifier (affects response to directional assist)")]
    public float agilityModifier = 1f;
    
    [Tooltip("Slope following modifier")]
    public float slopeFollowingModifier = 1f;
    
    [Header("KO Stats")]
    [Tooltip("KO grace period modifier")]
    public float koGraceModifier = 1f;
    
    [Tooltip("Minimum spin to live modifier")]
    public float minSpinToLiveModifier = 1f;
    
    [Header("Description")]
    [TextArea(2, 4)]
    public string description = "Tip component affects friction, agility, and KO resistance.";
}
