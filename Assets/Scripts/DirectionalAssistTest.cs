using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Test script to verify directional assist functionality.
/// Attach to a test beyblade in the arena to visualize and debug assist behavior.
/// </summary>
public class DirectionalAssistTest : MonoBehaviour
{
    [Header("Debug Visualization")]
    public bool showDebugInfo = true;
    public Color assistDirectionColor = Color.cyan;
    public Color surfaceNormalColor = Color.green;
    
    BeybladeController controller;
    bool wasUsingAssist = false;
    float assistStartTime;
    
    void Start()
    {
        controller = GetComponent<BeybladeController>();
        if (controller == null)
        {
            Debug.LogError("DirectionalAssistTest: No BeybladeController found on this GameObject!");
            enabled = false;
        }
    }
    
    void Update()
    {
        if (!showDebugInfo || controller == null)
            return;
            
        // Check if assist is being used
        bool isUsingAssist = IsAssistInputActive();
        
        if (isUsingAssist && !wasUsingAssist)
        {
            assistStartTime = Time.time;
            Debug.Log("Directional assist ACTIVATED");
        }
        else if (!isUsingAssist && wasUsingAssist)
        {
            float duration = Time.time - assistStartTime;
            Debug.Log($"Directional assist DEACTIVATED (duration: {duration:F2}s)");
        }
        
        wasUsingAssist = isUsingAssist;
    }
    
    bool IsAssistInputActive()
    {
        Vector2 input = Vector2.zero;
        
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
        }
        
        if (Gamepad.current != null)
        {
            Vector2 stickInput = Gamepad.current.leftStick.ReadValue();
            if (stickInput.magnitude > 0.1f)
                input = stickInput;
        }
        
        return input.magnitude >= 0.1f;
    }
    
    void OnGUI()
    {
        if (!showDebugInfo || controller == null)
            return;
            
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Directional Assist Debug", GUI.skin.box);
        GUILayout.Label($"Using Assist: {IsAssistInputActive()}");
        GUILayout.Label($"Match State: {GetMatchState()}");
        GUILayout.Label($"Spin Speed: {GetSpinSpeed():F1}");
        GUILayout.Label($"Has Landed: {GetHasLanded()}");
        GUILayout.EndArea();
    }
    
    string GetMatchState()
    {
        if (MatchFlowManager.Instance == null)
            return "No Flow Manager";
        return MatchFlowManager.Instance.CurrentState.ToString();
    }
    
    float GetSpinSpeed()
    {
        // Use reflection to access private spinSpeed field
        var field = typeof(BeybladeController).GetField("spinSpeed", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (float)field.GetValue(controller);
        return 0f;
    }
    
    bool GetHasLanded()
    {
        // Use reflection to access private hasLanded field
        var field = typeof(BeybladeController).GetField("hasLanded", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (bool)field.GetValue(controller);
        return false;
    }
    
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showDebugInfo || controller == null)
            return;
            
        // Visualize the assist direction when input is active
        if (IsAssistInputActive())
        {
            Vector2 input = Vector2.zero;
            
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed) input.y -= 1f;
                if (Keyboard.current.aKey.isPressed) input.x -= 1f;
                if (Keyboard.current.dKey.isPressed) input.x += 1f;
            }
            
            if (Gamepad.current != null)
            {
                Vector2 stickInput = Gamepad.current.leftStick.ReadValue();
                if (stickInput.magnitude > 0.1f)
                    input = stickInput;
            }
            
            if (input.magnitude >= 0.1f)
            {
                input = input.normalized;
                Vector3 worldDirection = new Vector3(input.x, 0f, input.y);
                
                // Get tip position
                var tipField = typeof(BeybladeController).GetField("tipCollider", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                Collider tipCollider = tipField != null ? (Collider)tipField.GetValue(controller) : null;
                Vector3 contactPoint = tipCollider != null ? tipCollider.transform.position : transform.position;
                
                // Draw assist direction
                Gizmos.color = assistDirectionColor;
                Gizmos.DrawLine(contactPoint, contactPoint + worldDirection * 2f);
                Gizmos.DrawWireSphere(contactPoint + worldDirection * 2f, 0.2f);
            }
        }
    }
#endif
}
