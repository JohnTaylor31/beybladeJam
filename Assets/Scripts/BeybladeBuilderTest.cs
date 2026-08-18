using UnityEngine;

/// <summary>
/// Test script to create sample Beyblade parts for testing the modular builder system.
/// Run this in the editor to generate sample ScriptableObject assets.
/// </summary>
public class BeybladeBuilderTest : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool createSampleParts = false;
    public string assetsPath = "Assets/Scripts/BeybladeParts";

    void Start()
    {
        if (createSampleParts)
        {
            CreateSampleParts();
            createSampleParts = false;
            Debug.Log("Sample beyblade parts created at: " + assetsPath);
        }
    }

    void CreateSampleParts()
    {
        // Ensure directory exists
        if (!System.IO.Directory.Exists(assetsPath))
        {
            System.IO.Directory.CreateDirectory(assetsPath);
        }

        // Create sample cores
        CreateCore("BalancedCore", 1f, 1f, 1f, 1f, 1f, 1f, "Balanced core with average stats.");
        CreateCore("AttackCore", 0.8f, 1.2f, 1.2f, 0.8f, 1.5f, 1.2f, "Attack-focused core with higher impact and spin.");
        CreateCore("DefenseCore", 1.3f, 0.9f, 0.8f, 1.2f, 0.8f, 0.9f, "Defense-focused core with higher mass and stability.");

        // Create sample rings
        CreateRing("BalanceRing", 1f, 1f, 1f, 1f, 1f, "Balanced ring with average movement and defense.");
        CreateRing("AttackRing", 1.2f, 0.8f, 1.5f, 0.8f, 0.8f, "Attack ring with higher movement and orbit force.");
        CreateRing("StaminaRing", 0.8f, 1.2f, 0.5f, 1.2f, 1.2f, "Stamina ring with better center pull and defense.");

        // Create sample tips
        CreateTip("FlatTip", 1f, 0.3f, 0.7f, 1f, 1f, 1f, 1f, "Flat tip with balanced friction and agility.");
        CreateTip("SharpTip", 0.8f, 0.5f, 0.9f, 0.8f, 1.2f, 1.2f, 0.8f, "Sharp tip with high grip but lower agility.");
        CreateTip("BallTip", 1.2f, 0.2f, 0.5f, 1.2f, 0.8f, 0.8f, 1.2f, "Ball tip with low friction and high agility.");

        Debug.Log("Created 3 cores, 3 rings, and 3 tips for testing.");
    }

    void CreateCore(string name, float mass, float spin, float stamina, float stability, float impact, float spinLoss, string description)
    {
        BeybladeCore core = ScriptableObject.CreateInstance<BeybladeCore>();
        core.name = name;
        core.massMultiplier = mass;
        core.spinMultiplier = spin;
        core.staminaDecayModifier = stamina;
        core.stabilityModifier = stability;
        core.impactForceMultiplier = impact;
        core.spinLossOnHitModifier = spinLoss;
        core.description = description;

        string path = $"{assetsPath}/{name}.asset";
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(core, path);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    void CreateRing(string name, float spinToMove, float centerPull, float orbit, float knockback, float burstDefense, string description)
    {
        BeybladeRing ring = ScriptableObject.CreateInstance<BeybladeRing>();
        ring.name = name;
        ring.spinToMoveModifier = spinToMove;
        ring.centerPullModifier = centerPull;
        ring.orbitForceModifier = orbit;
        ring.knockbackResistance = knockback;
        ring.burstDefense = burstDefense;
        ring.description = description;

        string path = $"{assetsPath}/{name}.asset";
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(ring, path);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    void CreateTip(string name, float friction, float highSpinGrip, float lowSpinGrip, float agility, float slopeFollow, float koGrace, float minSpin, string description)
    {
        BeybladeTip tip = ScriptableObject.CreateInstance<BeybladeTip>();
        tip.name = name;
        tip.frictionModifier = friction;
        tip.highSpinGrip = highSpinGrip;
        tip.lowSpinGrip = lowSpinGrip;
        tip.agilityModifier = agility;
        tip.slopeFollowingModifier = slopeFollow;
        tip.koGraceModifier = koGrace;
        tip.minSpinToLiveModifier = minSpin;
        tip.description = description;

        string path = $"{assetsPath}/{name}.asset";
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(tip, path);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
