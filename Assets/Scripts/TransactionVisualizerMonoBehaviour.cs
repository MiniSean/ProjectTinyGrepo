using UnityEngine;

/// <summary>
/// Listens for completed transactions from the ResourceManager and spawns
/// visual particle effects to represent the resource transfer.
/// </summary>
public class TransactionVisualizer : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The prefab for the resource particle visual effect.")]
    public GameObject resourceParticlePrefab;
    [Tooltip("The data asset that maps resource types to colors.")]
    public ResourceColorMap colorMap;

    [Header("Effect Settings")]
    [Tooltip("The duration of the particle's flight in seconds.")]
    public float particleDuration = 1.0f;
    [Tooltip("The height of the particle's arc in world units.")]
    public float particleArcHeight = 2.0f;

    private void OnEnable()
    {
        ResourceManager.Instance.OnTransactionCompleted += HandleTransactionCompleted;
    }

    private void OnDisable()
    {
        ResourceManager.Instance.OnTransactionCompleted -= HandleTransactionCompleted;
    }

    private void HandleTransactionCompleted(ITransactionOrder transaction)
    {
        if (resourceParticlePrefab == null || colorMap == null) return;

        // We need the world positions of the source and destination.
        // We can get these by checking if they are MonoBehaviours.
        if (transaction.Source is MonoBehaviour sourceMB && transaction.Destination is MonoBehaviour destMB)
        {
            Vector3 startPos = sourceMB.transform.position;
            Transform endTarget = destMB.transform;

            // Spawn the particle prefab.
            GameObject particleGO = Instantiate(resourceParticlePrefab, startPos, Quaternion.identity);

            // Set its color.
            Renderer particleRenderer = particleGO.GetComponent<Renderer>();
            if (particleRenderer != null)
            {
                particleRenderer.material.color = colorMap.GetColor(transaction.ResourceType);
            }

            // Initialize its movement.
            ResourceParticleMonoBehaviour particle = particleGO.GetComponent<ResourceParticleMonoBehaviour>();
            if (particle != null)
            {
                particle.Initialize(startPos, endTarget, particleDuration, particleArcHeight);
            }
        }
    }
}
