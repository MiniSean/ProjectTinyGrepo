using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// A generic component that detects IResourceProviders within a trigger volume
/// and notifies a designated handler component. This component is reusable for any
/// proximity-based interaction.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(IInteractionHandler))]
public class ProximityInteractionTrigger : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The radius of the spherical trigger volume.")]
    [SerializeField]
    private float _interactionRadius = 5f;

    [Tooltip("The time in seconds it takes to interact for one unit of resource.")]
    public float interactionCooldown = 1.0f;

    [Tooltip("The time in seconds the progress visual will stay at 100% before resetting.")]
    public float interactionCompleteTime = 0.1f;

    [Tooltip("The Renderer for the radial fill visual effect.")]
    public Renderer progressVisualRenderer;

    // The cached interfaces and components.
    private IInteractionHandler _interactionHandler;
    private SphereCollider _triggerCollider;
    private Material m_ProgressMaterial;
    
    // State management variables
    private Coroutine m_InteractionCoroutine;
    private IResourceProvider _providerInZone; // Reliably tracks which provider is currently inside.

    private void Awake()
    {
        // Validate the Handler ---
        // Ensure the assigned component actually implements the required interface.
        _interactionHandler = GetComponent<IInteractionHandler>();
        if (_interactionHandler == null)
        {
            Debug.LogError($"The component assigned to ProximityInteractionTrigger on {gameObject.name} does not implement the IInteractionHandler interface.", this);
            enabled = false; // Disable the component to prevent further errors.
            return;
        }

        // Configure the Collider ---
        _triggerCollider = GetComponent<SphereCollider>();
        _triggerCollider.isTrigger = true;
        _triggerCollider.radius = _interactionRadius;

        // Get a unique instance of the material from the renderer.
        if (progressVisualRenderer != null)
        {
            m_ProgressMaterial = progressVisualRenderer.material;
            progressVisualRenderer.enabled = false; // Start with the visual hidden.
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // When a collider enters, check if it's a resource provider.
        IResourceProvider provider = other.GetComponent<IResourceProvider>();
        if (provider != null)
        {
            // Set the state to indicate a provider is now inside the zone.
            _providerInZone = provider;

            // If an interaction coroutine is NOT already running, attempt to start one.
            // If one is already running, we do nothing here, as the coroutine itself
            // will handle re-triggering, thus preventing the race condition.
            if (m_InteractionCoroutine == null)
            {
                bool wasApproved = _interactionHandler.AttemptTransactionStart(_providerInZone);
                if (wasApproved)
                {
                    m_InteractionCoroutine = StartCoroutine(InteractionRoutine());
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IResourceProvider provider = other.GetComponent<IResourceProvider>();
        // Check if the exiting provider is the one we are currently tracking.
        if (provider != null && provider == _providerInZone)
        {
            // Clear the state, as the provider has left the zone.
            _providerInZone = null;
            
            // If an interaction was in progress, cancel it.
            if (m_InteractionCoroutine != null)
            {
                _interactionHandler.AttemptTransactionCancel(provider);
                StopCoroutine(m_InteractionCoroutine);
                m_InteractionCoroutine = null; // Mark the coroutine as stopped.
                if (progressVisualRenderer != null) progressVisualRenderer.enabled = false;
            }
        }
    }

    /// <summary>
    /// A coroutine that handles a single interaction cycle, updates the visual effect,
    /// and then attempts to trigger the next cycle if the provider is still present.
    /// </summary>
    private IEnumerator InteractionRoutine()
    {
        if (progressVisualRenderer != null) progressVisualRenderer.enabled = true;

        float timer = 0f;
        float fillDuration = Mathf.Max(0, interactionCooldown - interactionCompleteTime);

        // This loop represents a single interaction cycle.
        while (timer < interactionCooldown)
        {
            timer += Time.deltaTime;
            float progress = (timer < fillDuration) ? (timer / fillDuration) : 1.0f;
            if (m_ProgressMaterial != null) m_ProgressMaterial.SetFloat("_Progress", progress);
            yield return null;
        }

        // The cycle is complete. Notify the handler to finalize the transaction.
        _interactionHandler.AttemptTransactionComplete(_providerInZone);

        // --- Self-Restart Logic ---
        // Check if the provider is still in the zone after the cycle completes.
        if (_providerInZone != null)
        {
            // If so, attempt to start the next transaction immediately.
            bool wasReapproved = _interactionHandler.AttemptTransactionStart(_providerInZone);
            if (wasReapproved)
            {
                // If the next transaction is approved, restart the coroutine for a seamless loop.
                m_InteractionCoroutine = StartCoroutine(InteractionRoutine());
            }
            else
            {
                // If not approved (e.g., provider is full), stop and hide visuals.
                m_InteractionCoroutine = null;
                if (progressVisualRenderer != null) progressVisualRenderer.enabled = false;
            }
        }
        else
        {
            // If the provider left during the transaction, stop and hide visuals.
            m_InteractionCoroutine = null;
            if (progressVisualRenderer != null) progressVisualRenderer.enabled = false;
        }
    }

    // This Unity callback ensures the collider radius updates in the editor
    // if you change the _interactionRadius value in the inspector.
    private void OnValidate()
    {
        if (_triggerCollider == null)
        {
            _triggerCollider = GetComponent<SphereCollider>();
        }
        _triggerCollider.radius = _interactionRadius;
    }

    // Draws a gizmo to visualize the interaction radius in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        float height = 1f; // An arbitrary height for visualization

        Gizmos.color = Color.yellow;

        // Draw the top and bottom circles of the cylinder
        Vector3 topCenter = position + Vector3.up * height / 2;
        Vector3 bottomCenter = position - Vector3.up * height / 2;
        DrawWireDisk(topCenter, _interactionRadius);
        DrawWireDisk(bottomCenter, _interactionRadius);

        // Draw vertical lines connecting the circles
        Gizmos.DrawLine(bottomCenter + new Vector3(_interactionRadius, 0, 0), topCenter + new Vector3(_interactionRadius, 0, 0));
        Gizmos.DrawLine(bottomCenter + new Vector3(-_interactionRadius, 0, 0), topCenter + new Vector3(-_interactionRadius, 0, 0));
        Gizmos.DrawLine(bottomCenter + new Vector3(0, 0, _interactionRadius), topCenter + new Vector3(0, 0, _interactionRadius));
        Gizmos.DrawLine(bottomCenter + new Vector3(0, 0, -_interactionRadius), topCenter + new Vector3(0, 0, -_interactionRadius));
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

/// <summary>
/// Defines the contract for any component that can be notified by the
/// ProximityInteractionTrigger. This decouples the trigger from the specific logic.
/// </summary>
public interface IInteractionHandler
{
    /// <summary>
    /// Called when an IResourceProvider enters the trigger volume.
    /// return: true if transaction is accepted, false otherwise.
    /// </summary>
    public abstract bool AttemptTransactionStart(IResourceTrader interactor);

    /// <summary>
    /// Called when interaction coroutine finishes.
    /// return: true if transaction completion is accepted, false otherwise.
    /// </summary>
    public abstract bool AttemptTransactionComplete(IResourceTrader interactor);

    /// <summary>
    /// Called when an IResourceProvider exits the trigger volume.
    /// return: true if transaction cancellation is accepted, false otherwise.
    /// </summary>
    public abstract bool AttemptTransactionCancel(IResourceTrader interactor);
}
