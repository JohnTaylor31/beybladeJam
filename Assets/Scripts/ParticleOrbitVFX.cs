using UnityEngine;

public sealed class ParticleOrbitVFX : MonoBehaviour
{
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Min(0f)]
    [SerializeField] private float orbitSpeed = 900f;

    private void Update()
    {
        transform.Rotate(
            rotationAxis,
            orbitSpeed * Time.deltaTime,
            Space.Self
        );
    }

    public void SetOrbitSpeed(float degreesPerSecond)
    {
        orbitSpeed = Mathf.Max(0f, degreesPerSecond);
    }
}