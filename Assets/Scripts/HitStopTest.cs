using UnityEngine;

/// <summary>
/// Test script to verify hit-stop and impact FX functionality.
/// Attach to a test beyblade to visualize and debug collision effects.
/// </summary>
public class HitStopTest : MonoBehaviour
{
    [Header("Debug Visualization")]
    public bool showDebugInfo = true;
    public Color impactColor = Color.red;
    public Color hitStopColor = Color.yellow;
    
    BeybladeCollisionEffects collisionEffects;
    float lastImpactTime;
    float lastImpactVelocity;
    bool wasHitStopActive;
    int totalCollisions;
    float highestImpact;
    
    void Start()
    {
        collisionEffects = GetComponent<BeybladeCollisionEffects>();
        if (collisionEffects == null)
        {
            Debug.LogError("HitStopTest: No BeybladeCollisionEffects found on this GameObject!");
            enabled = false;
        }
        
        totalCollisions = 0;
        highestImpact = 0f;
    }
    
    void Update()
    {
        if (!showDebugInfo)
            return;
            
        // Monitor hit-stop state changes
        bool isHitStopActive = GetHitStopActive();
        
        if (isHitStopActive && !wasHitStopActive)
        {
            Debug.Log($"HIT-STOP ACTIVATED (impact: {lastImpactVelocity:F2})");
        }
        else if (!isHitStopActive && wasHitStopActive)
        {
            Debug.Log("HIT-STOP DEACTIVATED");
        }
        
        wasHitStopActive = isHitStopActive;
    }
    
    void OnGUI()
    {
        if (!showDebugInfo)
            return;
            
        GUILayout.BeginArea(new Rect(10, 10, 400, 250));
        GUILayout.Label("Hit-Stop & Impact FX Debug", GUI.skin.box);
        GUILayout.Label($"Hit-Stop Active: {GetHitStopActive()}");
        GUILayout.Label($"Time Scale: {Time.timeScale:F2}");
        GUILayout.Label($"Last Impact: {lastImpactVelocity:F2} ({Time.time - lastImpactTime:F2}s ago)");
        GUILayout.Label($"Total Collisions: {totalCollisions}");
        GUILayout.Label($"Highest Impact: {highestImpact:F2}");
        GUILayout.Label($"Match State: {GetMatchState()}");
        
        if (collisionEffects != null)
        {
            var thresholdField = typeof(BeybladeCollisionEffects).GetField("hitStopThreshold", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (thresholdField != null)
            {
                float threshold = (float)thresholdField.GetValue(collisionEffects);
                GUILayout.Label($"Hit-Stop Threshold: {threshold:F2}");
            }
            
            var burstThresholdField = typeof(BeybladeCollisionEffects).GetField("burstThreshold", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (burstThresholdField != null)
            {
                float burstThreshold = (float)burstThresholdField.GetValue(collisionEffects);
                GUILayout.Label($"Burst Threshold: {burstThreshold:F2}");
            }
        }
        
        GUILayout.EndArea();
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (!showDebugInfo)
            return;
            
        // Check if collision is with another Beyblade
        BeybladeController otherBeyblade = collision.collider.GetComponentInParent<BeybladeController>();
        if (otherBeyblade == null || otherBeyblade == GetComponent<BeybladeController>())
            return;
            
        float impact = collision.relativeVelocity.magnitude;
        lastImpactTime = Time.time;
        lastImpactVelocity = impact;
        totalCollisions++;
        
        if (impact > highestImpact)
        {
            highestImpact = impact;
        }
        
        Debug.Log($"COLLISION: Impact={impact:F2}, Count={totalCollisions}, Highest={highestImpact:F2}");
    }
    
    bool GetHitStopActive()
    {
        if (collisionEffects == null)
            return false;
            
        var field = typeof(BeybladeCollisionEffects).GetField("isHitStopActive", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (bool)field.GetValue(collisionEffects);
        return false;
    }
    
    string GetMatchState()
    {
        if (MatchFlowManager.Instance == null)
            return "No Flow Manager";
        return MatchFlowManager.Instance.CurrentState.ToString();
    }
    
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showDebugInfo)
            return;
            
        // Visualize recent collision point
        if (Time.time - lastImpactTime < 1f)
        {
            Gizmos.color = impactColor;
            float impactRatio = Mathf.Clamp01(lastImpactVelocity / 10f); // Assume max impact of 10
            Gizmos.DrawWireSphere(transform.position, 0.5f + impactRatio * 0.5f);
        }
        
        // Visualize hit-stop state
        if (GetHitStopActive())
        {
            Gizmos.color = hitStopColor;
            Gizmos.DrawWireSphere(transform.position, 0.8f);
        }
    }
#endif
}
