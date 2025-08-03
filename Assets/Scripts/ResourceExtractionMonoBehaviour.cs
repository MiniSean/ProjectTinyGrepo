using UnityEngine;
using System.Collections;

/// <summary>
/// Manages a single resource extraction point. Implements the IResourceExtraction interface
/// and controls a visual effect to show extraction progress when told to by a collector.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class ResourceExtractionMonoBehaviour : MonoBehaviour, IResourceExtraction
{
    [Header("Extraction Settings")]
    [Tooltip("The type of resource this node provides.")]
    public ResourceType resourceType = ResourceType.Stone;

    [Tooltip("Can resources be extracted from this point?")]
    public bool isExtractionPossible = true;

    [Header("Extraction Timing")]
    [Tooltip("The time in seconds it takes to extract one unit of resource.")]
    public float extractionCooldown = 2.0f;

    [Tooltip("The time in seconds the progress visual will stay at 100% before resetting.")]
    public float extractionCompleteTime = 0.1f;

    [Header("Visuals")]
    [Tooltip("The radius within which a collector can extract resources.")]
    public float extractionRadius = 5f;

    [Tooltip("The Renderer for the radial fill visual effect.")]
    public Renderer progressVisualRenderer;

    // Interface properties
    public ResourceType Type => resourceType;
    public bool IsExtractionAllowed => isExtractionPossible;
    private SphereCollider m_TriggerCollider;
    private Material m_ProgressMaterial;
    private Coroutine m_ExtractionCoroutine;
    private ITransactionOrder m_ActiveTransaction;

    private void Awake()
    {
        // Configure the SphereCollider to act as our detection trigger.
        m_TriggerCollider = GetComponent<SphereCollider>();
        m_TriggerCollider.isTrigger = true;
        m_TriggerCollider.radius = extractionRadius;

        // Get a unique instance of the material from the renderer.
        if (progressVisualRenderer != null)
        {
            m_ProgressMaterial = progressVisualRenderer.material;
            progressVisualRenderer.enabled = false; // Start with the visual hidden.
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered the trigger implements the IResourceCollector interface.
        IResourceCollector collector = other.GetComponent<IResourceCollector>();
        if (collector != null && isExtractionPossible)
        {
            collector.OnTransactionOpportunity(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the object that exited the trigger implements the IResourceCollector interface.
        IResourceCollector collector = other.GetComponent<IResourceCollector>();
        if (collector != null)
        {
            // Notify the collector that it must stop extraction.
            collector.OnTransactionExit();
            // The collector has left, stop the extraction process.
            if (m_ExtractionCoroutine != null)
            {
                StopCoroutine(m_ExtractionCoroutine);
                m_ExtractionCoroutine = null;
                m_ActiveTransaction = null;
                progressVisualRenderer.enabled = false; // Hide the visual.
            }
        }
    }

    public bool CanProvide(ResourceType type, int amount)
    {
        // Assumes extraction point is infinite.
        return true;
    }

    public void FulfillProvide(ResourceType type, int amount)
    {

    }

    /// <summary>
    /// Called by a collector after a transaction has been approved by the ResourceManager.
    /// </summary>
    public void BeginVisualCooldown(ITransactionOrder transaction)
    {
        if (m_ExtractionCoroutine == null)
        {
            m_ActiveTransaction = transaction;
            m_ExtractionCoroutine = StartCoroutine(ExtractionRoutine());
        }
    }

    /// <summary>
    /// A coroutine that handles a single extraction cycle, updates the visual effect,
    /// and then attempts to trigger the next cycle.
    /// </summary>
    private IEnumerator ExtractionRoutine()
    {
        Debug.Log("Extraction routine started.");
        if (progressVisualRenderer != null) progressVisualRenderer.enabled = true;

        float timer = 0f;
        float fillDuration = Mathf.Max(0, extractionCooldown - extractionCompleteTime);

        // This loop now represents a single extraction cycle.
        while (timer < extractionCooldown)
        {
            timer += Time.deltaTime;
            float progress = (timer < fillDuration) ? (timer / fillDuration) : 1.0f;
            if (m_ProgressMaterial != null) m_ProgressMaterial.SetFloat("_Progress", progress);
            yield return null;
        }

        // The cycle is complete. Notify the manager to finalize the transaction.
        m_ActiveTransaction?.Complete();

        // Get the collector from the completed transaction.
        IResourceCollector collector = m_ActiveTransaction?.Destination;

        // Reset state for the next cycle.
        m_ExtractionCoroutine = null;
        m_ActiveTransaction = null;

        bool wasReapproved = false;
        // If the collector is still in range, immediately notify it of the
        // opportunity to start the next extraction cycle.
        if (collector != null)
        {
            Debug.Log("Cycle complete. Re-triggering extraction opportunity.");
            wasReapproved = collector.OnTransactionOpportunity(this);
        }

        // If the next transaction was not approved (e.g., collector is full or left),
        // hide the visual effect.
        if (!wasReapproved)
        {
            if (progressVisualRenderer != null) progressVisualRenderer.enabled = false;
        }
    }

    // This Unity callback ensures the collider radius updates in the editor
    // if you change the extractionRadius value in the inspector.
    private void OnValidate()
    {
        if (m_TriggerCollider == null)
        {
            m_TriggerCollider = GetComponent<SphereCollider>();
        }
        m_TriggerCollider.radius = extractionRadius;
    }

    // Draws the cylinder gizmo to visualize the extraction radius in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        float height = 1f; // An arbitrary height for visualization

        Gizmos.color = Color.yellow;

        // Draw the top and bottom circles of the cylinder
        Vector3 topCenter = position + Vector3.up * height / 2;
        Vector3 bottomCenter = position - Vector3.up * height / 2;
        DrawWireDisk(topCenter, extractionRadius);
        DrawWireDisk(bottomCenter, extractionRadius);

        // Draw vertical lines connecting the circles
        Gizmos.DrawLine(bottomCenter + new Vector3(extractionRadius, 0, 0), topCenter + new Vector3(extractionRadius, 0, 0));
        Gizmos.DrawLine(bottomCenter + new Vector3(-extractionRadius, 0, 0), topCenter + new Vector3(-extractionRadius, 0, 0));
        Gizmos.DrawLine(bottomCenter + new Vector3(0, 0, extractionRadius), topCenter + new Vector3(0, 0, extractionRadius));
        Gizmos.DrawLine(bottomCenter + new Vector3(0, 0, -extractionRadius), topCenter + new Vector3(0, 0, -extractionRadius));
    }

    // Helper function to draw a wireframe circle for the gizmo.
    private void DrawWireDisk(Vector3 center, float radius)
    {
        int segments = 32;
        Vector3 from = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * 360f * Mathf.Deg2Rad;
            Vector3 to = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(from, to);
            from = to;
        }
    }
}