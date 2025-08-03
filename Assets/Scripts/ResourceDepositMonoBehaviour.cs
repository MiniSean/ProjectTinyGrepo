using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A MonoBehaviour that acts as a destination for resources. It implements
/// IResourceCollector and initiates deposit transactions when a provider is in range.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class ResourceDepositMonoBehaviour : MonoBehaviour, IResourceCollector
{
    [Header("Deposit Settings")]
    [Tooltip("The total amount of resources this deposit can hold.")]
    public int capacity = 500;
    [Tooltip("A list of resource types that this deposit will accept.")]
    public List<ResourceType> acceptedResourceTypes;

    // Interface implementation for Capacity.
    public int Capacity => capacity;

    private void OnTriggerEnter(Collider other)
    {
        // When a provider enters, notify it of the deposit opportunity.
        IResourceProvider provider = other.GetComponent<IResourceProvider>();
        if (provider != null)
        {
            // The provider is now responsible for initiating the transaction.
            // This deposit point simply presents the opportunity.
        }
    }

    // The deposit point itself doesn't need to request transactions,
    // so these methods can have minimal implementation for now.
    public bool OnTransactionOpportunity(IResourceExtraction resourceNode)
    {
        // This method is for collecting from extraction points, not relevant for a deposit.
        return false;
    }

    public void OnTransactionExit()
    {
        // This method is for leaving extraction points.
    }
}
