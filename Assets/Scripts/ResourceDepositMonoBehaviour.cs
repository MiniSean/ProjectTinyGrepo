using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// A MonoBehaviour that acts as a destination for resources. It implements
/// IResourceCollector and initiates deposit transactions when a provider is in range.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class ResourceDepositMonoBehaviour : MonoBehaviour, IResourceReceiver
{
    [Header("Deposit Settings")]
    [Tooltip("The total amount of resources this deposit can hold.")]
    public int capacity = 500;
    [Tooltip("A list of resource types that this deposit will accept.")]
    public List<ResourceType> acceptedResourceTypes;

    [Tooltip("The radius within which a collector can extract resources.")]
    public float depositRadius = 5f;

    [Header("Extraction Timing")]
    [Tooltip("The time in seconds it takes to extract one unit of resource.")]
    public float extractionCooldown = 2.0f;

    [Tooltip("The time in seconds the progress visual will stay at 100% before resetting.")]
    public float extractionCompleteTime = 0.1f;

    [Tooltip("The Renderer for the radial fill visual effect.")]
    public Renderer progressVisualRenderer;
    
    // Interface implementation for Capacity.
    public int Capacity => capacity;

    private SphereCollider m_TriggerCollider;
    private ITransactionOrder m_ActiveTransaction;
    private Material m_ProgressMaterial;
    private Coroutine m_DepositCoroutine;

    private void Awake()
    {
        // Configure the SphereCollider to act as our detection trigger.
        m_TriggerCollider = GetComponent<SphereCollider>();
        m_TriggerCollider.isTrigger = true;
        m_TriggerCollider.radius = depositRadius;

        // Get a unique instance of the material from the renderer.
        if (progressVisualRenderer != null)
        {
            m_ProgressMaterial = progressVisualRenderer.material;
            progressVisualRenderer.enabled = false; // Start with the visual hidden.
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // When a provider enters, notify it of the deposit opportunity.
        IResourceProvider provider = other.GetComponent<IResourceProvider>();
        if (provider != null)
        {
            // The provider is now responsible for initiating the transaction.
            // This deposit point simply presents the opportunity.
            Debug.Log("Entered deposit zone.");

            if (m_ActiveTransaction == null || m_ActiveTransaction.IsCompletedOrCanceled)
            {
                // Request a transaction from the global manager.
                m_ActiveTransaction = AttemptDeposit(provider);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the object that exited the trigger implements the IResourceCollector interface.
        IResourceProvider provider = other.GetComponent<IResourceProvider>();
        if (provider != null)
        {
            // Notify the collector that it must stop extraction.
            Debug.Log("Left deposit zone.");
            
            if (m_ActiveTransaction != null && provider == m_ActiveTransaction.Source)
            {
                m_ActiveTransaction.Cancel();
            }
            // Notify the collector that it must stop extraction.
            // The collector has left, stop the extraction process.
            if (m_DepositCoroutine != null)
            {
                StopCoroutine(m_DepositCoroutine);
                m_DepositCoroutine = null;
                m_ActiveTransaction = null;
                progressVisualRenderer.enabled = false; // Hide the visual.
            }
        }
    }

    public bool CanReceive(ResourceType type, int amount)
    {
        return ResourceManager.Instance.HasCapacity(this, type, amount);
    }

    private ITransactionOrder AttemptDeposit(IResourceProvider provider)
    {
        foreach (ResourceType resourceType in acceptedResourceTypes)
        {
            if (ResourceManager.Instance.GetResourceAmount(provider, resourceType) > 0)
            {
                m_ActiveTransaction = ResourceManager.Instance.RequestTransaction(provider, this, resourceType, 1);
                bool wasApproved = m_ActiveTransaction != null;
                if (wasApproved)
                {
                    // If approved, tell the visual component on the node to start its cooldown.
                    BeginVisualCooldown(m_ActiveTransaction);
                }
                return m_ActiveTransaction;
            }
        }
        return null;
    }

    /// <summary>
    /// Called by a collector after a transaction has been approved by the ResourceManager.
    /// </summary>
    public void BeginVisualCooldown(ITransactionOrder transaction)
    {
        if (m_DepositCoroutine == null)
        {
            m_ActiveTransaction = transaction;
            m_DepositCoroutine = StartCoroutine(DepositRoutine());
        }
    }

    /// <summary>
    /// A coroutine that handles a single extraction cycle, updates the visual effect,
    /// and then attempts to trigger the next cycle.
    /// </summary>
    private IEnumerator DepositRoutine()
    {
        Debug.Log("Deposit routine started.");
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
        IResourceProvider provider = m_ActiveTransaction?.Source;

        // Reset state for the next cycle.
        m_DepositCoroutine = null;
        m_ActiveTransaction = null;

        bool wasReapproved = false;
        // If the collector is still in range, immediately notify it of the
        // opportunity to start the next extraction cycle.
        if (provider != null)
        {
            Debug.Log("Cycle complete. Re-triggering deposit opportunity.");
            m_ActiveTransaction = AttemptDeposit(provider);
            wasReapproved = m_ActiveTransaction != null;
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
        m_TriggerCollider.radius = depositRadius;
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
        DrawWireDisk(topCenter, depositRadius);
        DrawWireDisk(bottomCenter, depositRadius);

        // Draw vertical lines connecting the circles
        Gizmos.DrawLine(bottomCenter + new Vector3(depositRadius, 0, 0), topCenter + new Vector3(depositRadius, 0, 0));
        Gizmos.DrawLine(bottomCenter + new Vector3(-depositRadius, 0, 0), topCenter + new Vector3(-depositRadius, 0, 0));
        Gizmos.DrawLine(bottomCenter + new Vector3(0, 0, depositRadius), topCenter + new Vector3(0, 0, depositRadius));
        Gizmos.DrawLine(bottomCenter + new Vector3(0, 0, -depositRadius), topCenter + new Vector3(0, 0, -depositRadius));
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
