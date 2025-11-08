using UnityEngine;

public class ShatterObject : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float upwardModifier = 0.5f;
    [SerializeField] private ForceMode forceMode = ForceMode.Impulse;

    [ContextMenu("Explode Children")]
    public void ExplodeChildren()
    {
        Vector3 explosionCenter = transform.position;

        // Przejście po wszystkich dzieciach
        foreach (Transform child in transform)
        {
            Rigidbody rb = child.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, explosionCenter, explosionRadius, upwardModifier, forceMode);
            }
        }

        Debug.Log($"💥 Explosion triggered on children of {name}");
    }

    // Opcjonalnie: eksplozja automatycznie przy starcie
    private void OnEnable()
    {
        Invoke("ExplodeChildren", 0.0f); // możesz opóźnić np. o 0.5 sekundy
    }
}