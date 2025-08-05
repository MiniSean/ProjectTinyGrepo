using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A MonoBehaviour that acts as a destination for resources. It implements
/// IResourceCollector and initiates deposit transactions when a provider is in range.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class ResourceDepositMonoBehaviour : MonoBehaviour, IResourceReceiver, IInteractionHandler
{
    [Header("Deposit Settings")]
    [Tooltip("The total amount of resources this deposit can hold.")]
    public int capacity = 500;
    [Tooltip("A list of resource types that this deposit will accept.")]
    public List<ResourceType> acceptedResourceTypes;

    // Interface implementation for Capacity.
    public int Capacity => capacity;

    private ITransactionOrder m_ActiveTransaction;

    public bool CanReceive(ResourceType type, int amount)
    {
        return ResourceManager.Instance.HasCapacity(this, type, amount);
    }

    #region IInteractionHandler Implemetnation
    /// <summary>
    /// Called when an IResourceProvider enters the trigger volume.
    /// return: true if transaction is accepted, false otherwise.
    /// </summary>
    public bool AttemptTransactionStart(IResourceTrader interactor)
    {
        // This handler expects the interactor to be a provider.
        IResourceProvider provider = interactor as IResourceProvider;
        if (provider == null || m_ActiveTransaction != null) return false;

        foreach (ResourceType resourceType in acceptedResourceTypes)
        {
             if (ResourceManager.Instance.GetResourceAmount(provider, resourceType) > 0)
            {
                m_ActiveTransaction = ResourceManager.Instance.RequestTransaction(provider, this, resourceType, 1);
                bool wasApproved = m_ActiveTransaction != null;
                if (wasApproved)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Called when interaction coroutine finishes.
    /// return: true if transaction completion is accepted, false otherwise.
    /// </summary>
    public bool AttemptTransactionComplete(IResourceTrader interactor)
    {
        IResourceProvider provider = interactor as IResourceProvider;
        if (m_ActiveTransaction == null || provider != m_ActiveTransaction.Source) return false;

        m_ActiveTransaction?.Complete();
        m_ActiveTransaction = null;

        return true;
    }

    /// <summary>
    /// Called when an IResourceProvider exits the trigger volume.
    /// return: true if transaction cancellation is accepted, false otherwise.
    /// </summary>
    public bool AttemptTransactionCancel(IResourceTrader interactor)
    {
        IResourceProvider provider = interactor as IResourceProvider;
        if (m_ActiveTransaction != null && provider == m_ActiveTransaction.Source)
        {
            m_ActiveTransaction.Cancel();
            m_ActiveTransaction = null;
            return true;
        }
        return false;
    }
    #endregion
}
