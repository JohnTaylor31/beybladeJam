using UnityEngine;

public class BeybladeCollisionDebug : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        // Only log collisions with other Beyblades
        Transform otherRoot = collision.collider.transform.root;
        if (!otherRoot.CompareTag("Beyblade"))
            return;

        float impact = collision.relativeVelocity.magnitude;
        ContactPoint contact = collision.GetContact(0);

        Debug.Log($"{name} hit {otherRoot.name} at velocity {impact} at position {contact.point}");
    }
}
