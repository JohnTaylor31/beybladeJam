using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a Beyblade's rotation speed as a health bar with color gradient.
/// Attach to a UI element with an Image component to display the bar.
/// </summary>
public class BeybladeHealthBar : MonoBehaviour
{
    public MonoBehaviour beybladeController;
    public Image healthBarFill;
    public Text spinSpeedText;
    public float maxSpinSpeed = 3000f;
    public Color highSpinColor = Color.green;
    public Color mediumSpinColor = Color.yellow;
    public Color lowSpinColor = Color.red;

    void Update()
    {
        if (beybladeController == null || healthBarFill == null) return;

        // Get spinSpeed via reflection to avoid direct dependency
        System.Reflection.FieldInfo spinSpeedField = beybladeController.GetType().GetField("spinSpeed");
        if (spinSpeedField == null) return;

        float currentSpin = (float)spinSpeedField.GetValue(beybladeController);

        // Calculate health as percentage of max spin speed
        float spinPercentage = Mathf.Clamp01(currentSpin / maxSpinSpeed);

        // Update health bar fill
        healthBarFill.fillAmount = spinPercentage;

        // Update color based on spin speed
        if (spinPercentage > 0.5f)
        {
            healthBarFill.color = Color.Lerp(mediumSpinColor, highSpinColor, (spinPercentage - 0.5f) * 2f);
        }
        else
        {
            healthBarFill.color = Color.Lerp(lowSpinColor, mediumSpinColor, spinPercentage * 2f);
        }

        // Update spin speed text if available
        if (spinSpeedText != null)
        {
            spinSpeedText.text = Mathf.RoundToInt(currentSpin).ToString();
        }
    }
}

