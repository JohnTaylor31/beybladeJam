using UnityEngine;

public class BeybladeSpinVFX : MonoBehaviour
{
    [Header("Spin VFX Prefab")]
    public GameObject spinVFXPrefab;   // assign your teammate's prefab here

    [Header("Spin Threshold")]
    public float minSpinToShow = 50f;  // show VFX only when spinning fast

    private GameObject activeVFX;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (spinVFXPrefab != null)
        {
            // Instantiate VFX as a child of the Beyblade root
            activeVFX = Instantiate(spinVFXPrefab, transform);
            activeVFX.SetActive(false);   // start hidden
        }
    }

    void Update()
    {
        if (activeVFX == null || rb == null)
            return;

        // Calculate spin magnitude along the up axis
        float spinAmount = Mathf.Abs(Vector3.Dot(rb.angularVelocity, transform.up));

        // Show or hide VFX based on spin
        if (spinAmount >= minSpinToShow)
        {
            if (!activeVFX.activeSelf)
                activeVFX.SetActive(true);
        }
        else
        {
            if (activeVFX.activeSelf)
                activeVFX.SetActive(false);
        }
    }
}
