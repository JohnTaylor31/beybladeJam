using UnityEngine;

public sealed class SpinRingVFX : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Min(0f)]
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Optional Pulsing")]
    [SerializeField] private bool pulseScale = true;

    [Min(0f)]
    [SerializeField] private float pulseAmount = 0.04f;

    [Min(0f)]
    [SerializeField] private float pulseSpeed = 8f;

    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    private void Update()
    {
        transform.Rotate(
            rotationAxis,
            rotationSpeed * Time.deltaTime,
            Space.Self
        );

        if (!pulseScale)
            return;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = initialScale * pulse;
    }

    public void SetRotationSpeed(float degreesPerSecond)
    {
        rotationSpeed = Mathf.Max(0f, degreesPerSecond);
    }
}