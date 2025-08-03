using UnityEngine;
using System.Collections.Generic;

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

    // Interface implementation for Capacity.
    public int Capacity => capacity;

    private SphereCollider m_TriggerCollider;

    private void Awake()
    {
        // Configure the SphereCollider to act as our detection trigger.
        m_TriggerCollider = GetComponent<SphereCollider>();
        m_TriggerCollider.isTrigger = true;
        m_TriggerCollider.radius = depositRadius;
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
        }
    }

    public bool CanReceive(ResourceType type, int amount)
    {
        return ResourceManager.Instance.HasCapacity(this, type, amount);
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
