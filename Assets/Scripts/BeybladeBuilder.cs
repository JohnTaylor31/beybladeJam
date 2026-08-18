using UnityEngine;

/// <summary>
/// Builds BeybladeStats by combining Core, Ring, and Tip parts.
/// Creates a runtime stats object that merges all modifiers from the selected parts.
/// </summary>
public class BeybladeBuilder : MonoBehaviour
{
    public static BeybladeBuilder Instance { get; private set; }

    [Header("Available Parts")]
    public BeybladeCore[] availableCores;
    public BeybladeRing[] availableRings;
    public BeybladeTip[] availableTips;

    [Header("Player Selection")]
    public int selectedCoreIndex = 0;
    public int selectedRingIndex = 0;
    public int selectedTipIndex = 0;

    [Header("Opponent Selection")]
    public int opponentCoreIndex = 0;
    public int opponentRingIndex = 0;
    public int opponentTipIndex = 0;

    [Header("Base Stats")]
    public BeybladeStats baseStats; // Reference to base stats for default values

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    /// <summary>
    /// Builds combined stats for player beyblade
    /// </summary>
    public BeybladeStats BuildPlayerStats()
    {
        return BuildStats(selectedCoreIndex, selectedRingIndex, selectedTipIndex);
    }

    /// <summary>
    /// Builds combined stats for opponent beyblade
    /// </summary>
    public BeybladeStats BuildOpponentStats()
    {
        return BuildStats(opponentCoreIndex, opponentRingIndex, opponentTipIndex);
    }

    /// <summary>
    /// Combines selected parts into a runtime BeybladeStats
    /// </summary>
    BeybladeStats BuildStats(int coreIndex, int ringIndex, int tipIndex)
    {
        // Validate indices
        coreIndex = Mathf.Clamp(coreIndex, 0, availableCores.Length - 1);
        ringIndex = Mathf.Clamp(ringIndex, 0, availableRings.Length - 1);
        tipIndex = Mathf.Clamp(tipIndex, 0, availableTips.Length - 1);

        BeybladeCore core = availableCores[coreIndex];
        BeybladeRing ring = availableRings[ringIndex];
        BeybladeTip tip = availableTips[tipIndex];

        // Create runtime stats (not saved as asset)
        BeybladeStats combinedStats = ScriptableObject.CreateInstance<BeybladeStats>();

        // Start with base stats if available, otherwise use defaults
        float baseInitialSpin = baseStats != null ? baseStats.initialSpin : 3000f;
        float baseSpinToMove = baseStats != null ? baseStats.spinToMoveFactor : 0.15f;
        float baseMass = baseStats != null ? baseStats.massMultiplier : 1f;
        float baseStaminaDecay = baseStats != null ? baseStats.staminaDecayPerSecond : 0.02f;
        float baseSpeedDecay = baseStats != null ? baseStats.speedDecayPerSecond : 0.01f;
        float baseImpactForce = baseStats != null ? baseStats.impactForce : 10f;
        float baseSpinLossOnHit = baseStats != null ? baseStats.spinLossOnHit : 0.95f;
        float baseOrbitForce = baseStats != null ? baseStats.orbitForce : 3f;
        float baseCenterPull = baseStats != null ? baseStats.centerPullStrength : 0.8f;
        float baseMinSpin = baseStats != null ? baseStats.minSpinToLive : 30f;
        float baseKoGrace = baseStats != null ? baseStats.koGraceSeconds : 0.5f;
        float baseAssistCost = baseStats != null ? baseStats.assistSpinCost : 0.5f;
        float baseAssistForce = baseStats != null ? baseStats.assistForce : 50f;

        // Apply Core modifiers
        combinedStats.initialSpin = baseInitialSpin * core.spinMultiplier;
        combinedStats.massMultiplier = baseMass * core.massMultiplier;
        combinedStats.staminaDecayPerSecond = baseStaminaDecay * core.staminaDecayModifier;
        combinedStats.impactForce = baseImpactForce * core.impactForceMultiplier;
        combinedStats.spinLossOnHit = Mathf.Clamp01(baseSpinLossOnHit * core.spinLossOnHitModifier);

        // Apply Ring modifiers
        combinedStats.spinToMoveFactor = baseSpinToMove * ring.spinToMoveModifier;
        combinedStats.centerPullStrength = baseCenterPull * ring.centerPullModifier;
        combinedStats.orbitForce = baseOrbitForce * ring.orbitForceModifier;

        // Apply Tip modifiers
        combinedStats.koGraceSeconds = baseKoGrace * tip.koGraceModifier;
        combinedStats.minSpinToLive = baseMinSpin * tip.minSpinToLiveModifier;

        // Apply assist modifiers (based on tip agility)
        combinedStats.assistSpinCost = baseAssistCost / tip.agilityModifier;
        combinedStats.assistForce = baseAssistForce * tip.agilityModifier;

        // Set name for debugging
        combinedStats.name = $"Combined_{core.name}_{ring.name}_{tip.name}";

        return combinedStats;
    }

    /// <summary>
    /// Gets the description for a selected part combination
    /// </summary>
    public string GetBuildDescription(int coreIndex, int ringIndex, int tipIndex)
    {
        if (availableCores.Length == 0 || availableRings.Length == 0 || availableTips.Length == 0)
            return "No parts available";

        coreIndex = Mathf.Clamp(coreIndex, 0, availableCores.Length - 1);
        ringIndex = Mathf.Clamp(ringIndex, 0, availableRings.Length - 1);
        tipIndex = Mathf.Clamp(tipIndex, 0, availableTips.Length - 1);

        BeybladeCore core = availableCores[coreIndex];
        BeybladeRing ring = availableRings[ringIndex];
        BeybladeTip tip = availableTips[tipIndex];

        return $"Core: {core.name}\nRing: {ring.name}\nTip: {tip.name}";
    }

    /// <summary>
    /// Randomizes opponent part selection
    /// </summary>
    public void RandomizeOpponent()
    {
        if (availableCores.Length > 0)
            opponentCoreIndex = Random.Range(0, availableCores.Length);
        if (availableRings.Length > 0)
            opponentRingIndex = Random.Range(0, availableRings.Length);
        if (availableTips.Length > 0)
            opponentTipIndex = Random.Range(0, availableTips.Length);
    }
}
