using UnityEngine;

/// <summary>
/// Test script to verify burst finish functionality.
/// Attach to a test beyblade to visualize and debug burst finish events.
/// </summary>
public class BurstFinishTest : MonoBehaviour
{
    [Header("Debug Visualization")]
    public bool showDebugInfo = true;
    public Color burstColor = Color.magenta;
    
    BeybladeCollisionEffects collisionEffects;
    BeybladeController controller;
    int burstCount;
    float highestBurstImpact;
    
    void Start()
    {
        collisionEffects = GetComponent<BeybladeCollisionEffects>();
        controller = GetComponent<BeybladeController>();
        
        if (collisionEffects == null)
        {
            Debug.LogError("BurstFinishTest: No BeybladeCollisionEffects found on this GameObject!");
            enabled = false;
        }
        
        burstCount = 0;
        highestBurstImpact = 0f;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (!showDebugInfo)
            return;
            
        // Check if collision is with another Beyblade
        BeybladeController otherBeyblade = collision.collider.GetComponentInParent<BeybladeController>();
        if (otherBeyblade == null || otherBeyblade == controller)
            return;
            
        float impact = collision.relativeVelocity.magnitude;
        float threshold = GetBurstThreshold();
        
        if (impact >= threshold)
        {
            burstCount++;
            if (impact > highestBurstImpact)
            {
                highestBurstImpact = impact;
            }
            
            Debug.Log($"BURST FINISH DETECTED! Impact: {impact:F2}, Count: {burstCount}");
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo)
            return;
            
        GUILayout.BeginArea(new Rect(10, 270, 350, 200));
        GUILayout.Label("Burst Finish Debug", GUI.skin.box);
        GUILayout.Label($"Total Bursts: {burstCount}");
        GUILayout.Label($"Highest Burst Impact: {highestBurstImpact:F2}");
        GUILayout.Label($"Burst Threshold: {GetBurstThreshold():F2}");
        GUILayout.Label($"Match State: {GetMatchState()}");
        GUILayout.EndArea();
    }
    
    float GetBurstThreshold()
    {
        if (collisionEffects == null)
            return 0f;
            
        var field = typeof(BeybladeCollisionEffects).GetField("burstThreshold", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (float)field.GetValue(collisionEffects);
        return 0f;
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
            
        // Visualize burst threshold area around the beyblade
        float threshold = GetBurstThreshold();
        if (threshold > 0f)
        {
            Gizmos.color = burstColor;
            Gizmos.DrawWireSphere(transform.position, threshold * 0.1f);
        }
    }
#endif
}
